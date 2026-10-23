using Celeste.Mods.Randomizer.Generation;
using Celeste.Mods.Randomizer.Generation.Construction;
using Celeste.Mods.Randomizer.Generation.Configuration.Metadata;
using Celeste.Mods.Randomizer.Generation.Configuration.Maps;
using Celeste.Mods.Randomizer.Generation.Rooms;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using Celeste.Game.Runtime;
using Celeste.Game.Content.Maps;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Execution.Areas;
using Celeste.Everest.Content.Maps.Registry;
using Celeste.Everest.Content.Maps.Metadata;
using Celeste.Everest.Content.Metadata;
using Celeste.Everest.Content.Types.Text;
using Celeste.Formats.Map.Binary.Source;
using Celeste.Scene.Rendering;
using Celeste.Simulation.Presentation.Scene.Transitions;
using Celeste.Game.Gameplay.Enemies.Seekers;

namespace Celeste.Mods.Randomizer.Runtime
{
    public sealed partial class EngineGenerationContext : GenerationContext
    {
        private readonly GameApplication application;
        private readonly IList<EverestArea> areas;
        private RandoMetadataFile metadata = new RandoMetadataFile();

        public EngineGenerationContext(GameApplication application, Version version)
        {
            this.application = application ?? throw new ArgumentNullException(nameof(application));
            Version = version ?? throw new ArgumentNullException(nameof(version));
            areas = new RegisteredAreaView(application);
            Generator = new RandoLogic(this);
        }

        // Read current registry membership without copying; publication remains main-thread only.
        public override IList<EverestArea> Areas => areas;
        public override Version Version { get; }
        public override RandoMetadataFile Metadata => metadata;
        public override IDictionary<string, string> Dialog => application.Dialog.Language.Dialog;
        public override IDictionary<string, string> CleanedDialog => application.Dialog.Language.Cleaned;

        public override string[] ColorGrades => new[]
        {
            "none", "cold", "credits", "feelingdown", "golden", "hot", "oldsite", "panicattack", "reflection",
            "templevoid"
        };

        public override int TextureHeight(string path) => application.GraphicsResources.GameAtlas[path].FrameHeight;
        public override string CleanDialog(string key) => application.Dialog.Clean(key);
        public override string GetDialog(string key) => application.Dialog.Get(key);
        public override bool HasDialog(string key) => application.Dialog.Has(key);

        public void InitializeContent()
        {
            var result = new RandoMetadataFile();
            var sourcePrefix = new Regex("^[^\\\\/]+:(/|\\\\).*$");
            foreach (var item in application.SourceContentRegistry.Map)
            {
                var asset = item.Value;
                if (sourcePrefix.IsMatch(item.Key) || Path.GetFileName(asset.PathVirtual) != "rando" ||
                    asset.Type != typeof(AssetTypeYaml)) continue;
                using var reader = new StreamReader(asset.Stream);
                var parsed = InstalledYamlHelper.Deserializer.Deserialize<RandoMetadataFile>(reader);
                parsed.ConfigPath = asset.PathVirtual;
                parsed.ModName = asset.Source.Name;
                result.Add(parsed, this);
            }

            metadata = result;
            Generator.ProcessAreas();
        }

        public override RandoConfigFile LoadConfig(EverestArea area, bool lazy)
        {
            var full = LoadSingle($"Config/{area.SID}.rando", false);
            if (full != null) return full;
            var a = LoadSingle($"Config/{area.SID}.A.rando", lazy);
            var b = LoadSingle($"Config/{area.SID}.B.rando", lazy);
            var c = LoadSingle($"Config/{area.SID}.C.rando", lazy);
            if (a?.ASide == null && b?.BSide == null && c?.CSide == null) return null;
            return new RandoConfigFile { ASide = a?.ASide, BSide = b?.BSide, CSide = c?.CSide };
        }

        public override RandoConfigFile ReloadConfig(OriginalAreaKey key)
        {
            if (key.Mode < 0 || key.Mode > 2) throw new ArgumentOutOfRangeException(nameof(key));
            return LoadSingle($"Config/{GetArea(key).SID}.{(char)('A' + key.Mode)}.rando", false);
        }

        private RandoConfigFile LoadSingle(string path, bool lazy)
        {
            if (!application.SourceContentRegistry.TryGet(path, out var asset)) return null;
            if (lazy)
                return new RandoConfigFile
                {
                    ConfigPath = asset.PathVirtual, ModName = asset.Source.Name,
                    ASide = new List<RandoConfigRoom>(), BSide = new List<RandoConfigRoom>(),
                    CSide = new List<RandoConfigRoom>()
                };
            using var reader = new StreamReader(asset.Stream);
            var result = InstalledYamlHelper.Deserializer.Deserialize<RandoConfigFile>(reader);
            result.ConfigPath = asset.PathVirtual;
            result.ModName = asset.Source.Name;
            return result;
        }

        public override void PublishArea(EverestArea area)
        {
            using var publication = new GeneratedAreaPublication(() =>
            {
                var areas = application.AreaRegistry.Areas;
                if (area.ID == areas.Count) areas.Add(area);
                else if (area.ID == areas.Count - 1 && areas[area.ID] is EverestArea previous &&
                         previous.SID.StartsWith("randomizer/", StringComparison.Ordinal)) areas[area.ID] = area;
                else
                    throw new InvalidOperationException(
                        "Randomizer area publication would overwrite an installed area.");
            }, CancellationToken);
            application.MainThread.Schedule(publication.Commit);
            publication.Wait();
        }

        public override OriginalChapterMapData CreateGeneratedMap(OriginalAreaKey area)
            => OriginalChapterMapData.CreateGenerated(area, GetArea(area), application.MapStartPolicy,
                new GeneratedMapLoadPolicy());

        public override bool AudioEventExists(string eventPath)
        {
            if (string.IsNullOrEmpty(eventPath) || eventPath == "null" || eventPath == "event:/none") return false;
            if (application.EverestAudio.cachedModEvents.TryGetValue(eventPath, out var modEvent))
                return modEvent != null;
            var runtime = application.AudioSession.Runtime;
            var result = eventPath.StartsWith("guid://", StringComparison.Ordinal)
                ? runtime.GetEventByIDNative(Guid.Parse(eventPath.Substring(7)), out _)
                : runtime.GetEventNative(eventPath, out _);
            return result == Celeste.Audio.Fmod.Interop.FmodResult.OK;
        }

        public override OriginalAreaModeData CreateMode() => new EverestAreaModeData();

        public override void ConfigureGeneratedArea(EverestArea area)
        {
            area.Metadata = new EverestMapMetadata
            {
                Modes = new[] { new EverestModeMetadata { HeartIsEnd = true }, null, null }
            };
            area.OnLevelBegin = level => level.SourceScene.AddEntity(new SeekerEffectsControllerState(application));
        }

        public override Action<ISourceRendererScene, bool, Action> CreateRandomWipe()
            => (scene, wipeIn, completed) =>
            {
                // Upstream PickWipe chooses at playback time, independently of the generation seed.
                _ = new Random().Next(10) switch
                {
                    0 => (ScreenWipePresentation)new CurtainWipePresentation(scene, wipeIn, completed),
                    1 => new AngledWipePresentation(scene, wipeIn, completed),
                    2 => new DropWipePresentation(scene, wipeIn, completed),
                    3 => new DreamWipePresentation(scene, wipeIn, completed),
                    4 => new WindWipePresentation(scene, wipeIn, completed),
                    5 => new FallWipePresentation(scene, wipeIn, completed),
                    6 => new HeartWipePresentation(scene, wipeIn, completed),
                    7 => new KeyDoorWipePresentation(scene, wipeIn, completed),
                    8 => new MountainWipePresentation(scene, wipeIn, completed),
                    _ => new StarfieldWipePresentation(scene, wipeIn, completed)
                };
            };

        public override OriginalLevelEntityData CreateEntity(GenerationEntitySpec source)
        {
            var attributes = source.Values == null
                ? new Dictionary<string, object>()
                : new Dictionary<string, object>(source.Values);
            attributes["id"] = source.ID;
            attributes["x"] = source.Position.X;
            attributes["y"] = source.Position.Y;
            attributes["width"] = source.Width;
            attributes["height"] = source.Height;
            var element = new BinaryPackerElement { Name = source.Name, Attributes = attributes };
            if (source.Nodes != null)
                element.Children = source.Nodes.Select(node => new BinaryPackerElement
                {
                    Name = "node", Attributes = new Dictionary<string, object> { ["x"] = node.X, ["y"] = node.Y }
                }).ToList();
            var entity = OriginalLevelRoomDataReader.CreateEntityData(source.Level, element);
            // Values belongs to the authored runtime entity, including reserved-looking keys; retain it exactly.
            entity.Values = source.Values;
            entity.Nodes = source.Nodes;
            return entity;
        }
    }
}
