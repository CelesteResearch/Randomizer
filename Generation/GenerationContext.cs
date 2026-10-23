using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Celeste.Game.Execution.Areas;

namespace Celeste.Mods.Randomizer.Generation
{
    public abstract class GenerationContext
    {
        public abstract IList<AreaData> Areas { get; }
        public abstract Version Version { get; }
        public abstract RandoMetadataFile Metadata { get; }
        public abstract IDictionary<string, string> Dialog { get; }
        public abstract IDictionary<string, string> CleanedDialog { get; }
        public RandoSettings Settings { get; set; }
        public bool LazyLoading { get; set; } = true;
        public RandoLogic Generator { get; set; }
        public CancellationToken CancellationToken { get; set; }

        public readonly List<StaticRoom> AllRooms = new List<StaticRoom>();
        public readonly Dictionary<string, List<AreaKey>> LevelSets = new Dictionary<string, List<AreaKey>>();

        public readonly Dictionary<RandoSettings.AreaKeyNotStupid, int> LevelCount =
            new Dictionary<RandoSettings.AreaKeyNotStupid, int>();

        public readonly HashSet<RandoSettings.AreaKeyNotStupid> LazyLoaded =
            new HashSet<RandoSettings.AreaKeyNotStupid>();

        public readonly Dictionary<string, string> RandomDialogMappings = new Dictionary<string, string>();

        private readonly ConditionalWeakTable<object, GenerationAttachedState> attachedState =
            new ConditionalWeakTable<object, GenerationAttachedState>();

        private AreaData stagedArea;
        private GenerationAreaView generationAreas;
        public IReadOnlyList<AreaData> GenerationAreas => generationAreas ??= new GenerationAreaView(this);

        public void CheckCancellation() => CancellationToken.ThrowIfCancellationRequested();

        public IDisposable StageArea(AreaData area)
        {
            CheckCancellation();
            if (stagedArea != null)
                throw new InvalidOperationException("A map generation is already active.");
            stagedArea = area;
            return new AreaStage(this);
        }

        public AreaData GetArea(AreaKey key) =>
            key.ID < 0 || key.ID >= GenerationAreas.Count ? null : GenerationAreas[key.ID];

        public AreaData GetArea(string sid)
        {
            foreach (var area in GenerationAreas)
                if (area.SID == sid)
                    return area;
            return null;
        }

        public AreaKey Key(AreaData area, int mode) => new AreaKey
        {
            ID = area.ID,
            Mode = mode,
            SID = area.SID,
            SIDID = area.ID
        };

        public GenerationAttachedState State(object value) => attachedState.GetOrCreateValue(value);
        public abstract RandoConfigFile LoadConfig(AreaData area, bool lazy);
        public abstract RandoConfigFile ReloadConfig(AreaKey key);
        public abstract void PublishArea(AreaData area);
        public abstract MapData CreateGeneratedMap(AreaKey area);
        public abstract LevelData CloneRoom(LevelData source);
        public abstract EntityData CreateEntity(GenerationEntitySpec source);
        public abstract int TextureHeight(string path);
        public abstract bool AudioEventExists(string eventPath);
        public abstract string CleanDialog(string key);
        public abstract string GetDialog(string key);
        public abstract bool HasDialog(string key);
        public abstract Action<Celeste.Scene.Rendering.ISourceRendererScene, bool, Action> CreateRandomWipe();
        public abstract string[] ColorGrades { get; }

        public virtual ModeProperties CreateMode() => new ModeProperties();
        public abstract void ConfigureGeneratedArea(AreaData area);

        private sealed class AreaStage : IDisposable
        {
            private GenerationContext context;
            public AreaStage(GenerationContext context) => this.context = context;

            public void Dispose()
            {
                if (context == null)
                    return;
                context.stagedArea = null;
                context = null;
            }
        }

        private sealed class GenerationAreaView : IReadOnlyList<AreaData>
        {
            private readonly GenerationContext context;
            public GenerationAreaView(GenerationContext context) => this.context = context;

            public int Count => context.stagedArea == null
                ? context.Areas.Count
                : Math.Max(context.Areas.Count, context.stagedArea.ID + 1);

            public AreaData this[int index] =>
                context.stagedArea?.ID == index ? context.stagedArea : context.Areas[index];

            public IEnumerator<AreaData> GetEnumerator()
            {
                for (int index = 0; index < Count; index++)
                    yield return this[index];
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
