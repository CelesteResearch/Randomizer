using System;
using System.Collections.Generic;
using System.Reflection;
using System.Collections;
using System.Linq;
using System.Numerics;
using System.Xml;
using Celeste.Core.Scheduling;
using Celeste.Game.Execution.Areas;
using Celeste.Game.Execution.Graphics;
using Celeste.Game.Chapters.Flow;
using Celeste.Everest.Content.Graphics;
using Celeste.Graphics.Content.Tiles;
using Celeste.Graphics.Content.Tiles.Autotiling;
using Celeste.Formats.Graphics.Sprites;
using Celeste.Graphics.Animation;
using Celeste.Localization.Lookup;
using Celeste.Simulation.Runtime.Session;
using Celeste.Mods.Randomizer.Generation.Configuration.Settings;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.Randomizer.Gameplay
{
    public sealed partial class RandomizerGameplay
    {
        private delegate void ApplyGraphics(ChapterCompletionSession session, IOriginalAreaSourceTable areas,
            OriginalGameGraphicsResources graphics, ISourceEngineContext owner, EverestGraphicsXmlReader content,
            LevelEnterStaticState errors, DialogCatalog dialogs, ref string path,
            ref EverestGraphicsFailureDomain domain);

        private void LoadContentHooks()
        {
            hooks.Add(new Hook(
                typeof(EverestLevelLoaderGraphicsPolicy).GetMethod(nameof(EverestLevelLoaderGraphicsPolicy.Apply)),
                (ApplyGraphicsHook)MergeGraphics));
            LoadDialogHooks();
            LoadVisualHooks();
            LoadFakeHeartHook();
        }

        private delegate void ApplyGraphicsHook(ApplyGraphics orig, ChapterCompletionSession session,
            IOriginalAreaSourceTable areas, OriginalGameGraphicsResources graphics, ISourceEngineContext owner,
            EverestGraphicsXmlReader content, LevelEnterStaticState errors, DialogCatalog dialogs,
            ref string path, ref EverestGraphicsFailureDomain domain);

        private void MergeGraphics(ApplyGraphics orig, ChapterCompletionSession session, IOriginalAreaSourceTable areas,
            OriginalGameGraphicsResources graphics, ISourceEngineContext owner, EverestGraphicsXmlReader content,
            LevelEnterStaticState errors, DialogCatalog dialogs, ref string path,
            ref EverestGraphicsFailureDomain domain)
        {
            orig(session, areas, graphics, owner, content, errors, dialogs, ref path, ref domain);
            if (!ReferenceEquals(owner, engine) || Settings(session) is not { } settings) return;
            var foreground = new List<string>();
            var background = new List<string>();
            var animated = new List<string>();
            var sprites = new List<string>();
            foreach (var map in settings.EnabledMaps)
            {
                var metadata = context.GetArea(map).Metadata;
                AddPath(foreground, metadata?.ForegroundTiles);
                AddPath(background, metadata?.BackgroundTiles);
                AddPath(animated, metadata?.AnimatedTiles);
                AddPath(sprites, metadata?.Sprites);
            }

            CombineAutotilers(graphics.ForegroundAutotiler, foreground, settings, content);
            foreach (var pair in context.Metadata.FGTiles)
            {
                var parts = pair.Value.Split(':');
                Tiles(graphics.ForegroundAutotiler)[pair.Key] = Tiles(CreateAutotiler(parts[0], content))[parts[1][0]];
            }

            CombineAutotilers(graphics.BackgroundAutotiler, background, settings, content);
            CombineAnimatedTiles(graphics.AnimatedTilesBank, animated, settings, content);
            CombineSprites(graphics.SpriteBank, sprites, content);
        }

        private static void AddPath(List<string> paths, string path)
        {
            if (!string.IsNullOrEmpty(path) && !paths.Contains(path)) paths.Add(path);
        }

        private Autotiler CreateAutotiler(string path, EverestGraphicsXmlReader content)
            => ((ISourceAutotilerConstructionEnvironment)engine).AutotilerConstruction.Create(engine,
                path, content.LoadContent(path), engine.GraphicsResources);

        private static IDictionary Tiles(Autotiler autotiler)
            => (IDictionary)autotiler.GetType().GetField("lookup", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(autotiler);

        private void CombineAutotilers(Autotiler basic, List<string> additions, RandoSettings settings,
            EverestGraphicsXmlReader content)
        {
            var counts = new Dictionary<char, int>();
            var random = new Random((int)settings.IntSeed);
            var lookup = Tiles(basic);
            foreach (char key in lookup.Keys) counts[key] = 1;
            foreach (var path in additions)
            {
                var incoming = Tiles(CreateAutotiler(path, content));
                foreach (char key in incoming.Keys)
                {
                    if (counts.ContainsKey(key))
                    {
                        counts[key]++;
                        if (random.Next(counts[key]) == 0) lookup[key] = incoming[key];
                    }
                    else
                    {
                        counts[key] = 1;
                        lookup[key] = incoming[key];
                    }
                }
            }
        }

        private void CombineAnimatedTiles(AnimatedTileBank basic, List<string> additions, RandoSettings settings,
            EverestGraphicsXmlReader content)
        {
            var counts = basic.AnimationsByName.Keys.ToDictionary(key => key, _ => 1);
            var random = new Random((int)settings.IntSeed);
            foreach (var path in additions)
            foreach (XmlNode node in content.LoadContent(path)["Data"])
            {
                if (node == null || node.NodeType == XmlNodeType.Comment) continue;
                var element = (XmlElement)node;
                string name = element.GetAttribute("name");
                bool insert;
                if (counts.TryGetValue(name, out int count))
                {
                    counts[name] = ++count;
                    insert = random.Next(count) == 0;
                }
                else
                {
                    counts[name] = 1;
                    insert = true;
                }

                if (!insert) continue;
                int previousId = -1;
                if (basic.AnimationsByName.TryGetValue(name, out var previous))
                {
                    previousId = previous.ID;
                    basic.AnimationsByName.Remove(name);
                }

                basic.Add(name, AttributeFloat(element, "delay"),
                    new Vector2(AttributeFloat(element, "posX"), AttributeFloat(element, "posY")),
                    new Vector2(AttributeFloat(element, "origX"), AttributeFloat(element, "origY")),
                    engine.GraphicsResources.GameAtlas.GetAtlasSubtextures(element.GetAttribute("path")));
                if (previousId < 0) continue;
                var animation = basic.AnimationsByName[name];
                animation.ID = previousId;
                basic.Animations[previousId] = animation;
                basic.Animations.RemoveAt(basic.Animations.Count - 1);
            }
        }

        private static float AttributeFloat(XmlElement element, string name)
            => element.HasAttribute(name)
                ? float.Parse(element.GetAttribute(name), System.Globalization.CultureInfo.InvariantCulture)
                : 0f;

        private void CombineSprites(SpriteBank original, List<string> additions, EverestGraphicsXmlReader content)
        {
            var sources = (Dictionary<string, SpriteData>)typeof(SpriteBank)
                .GetField("sources", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(original);
            var templates = (Dictionary<string, Sprite>)typeof(SpriteTemplateCollection)
                .GetField("templates", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(original);
            foreach (var path in additions)
            {
                var incoming = new SpriteBank(engine, engine.GraphicsResources.GameAtlas, content.LoadContent(path));
                foreach (var pair in incoming.SpriteData)
                {
                    if (original.SpriteData.TryGetValue(pair.Key, out var existing))
                    {
                        existing.Sprite.MergeAnimationsFrom(pair.Value.Sprite);
                        existing.Sources.AddRange(pair.Value.Sources);
                        existing.Sprite.Stop();
                        if (pair.Value.Sprite.CurrentAnimationID != "")
                            existing.Sprite.Play(pair.Value.Sprite.CurrentAnimationID);
                    }
                    else
                    {
                        sources[pair.Key] = pair.Value;
                        templates[pair.Key] = pair.Value.Sprite;
                    }
                }
            }
        }
    }
}
