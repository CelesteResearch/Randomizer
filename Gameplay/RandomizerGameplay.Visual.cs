using System;
using System.Numerics;
using System.Linq;
using System.Reflection;
using Celeste.Core.Display;
using Celeste.Core.Geometry;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Presentation.Hud.Poems;
using Celeste.Game.Gameplay.Holdables.TheoCrystal;
using Celeste.Game.Entities.Collectibles.Hearts;
using Celeste.Graphics.Display.Colors;
using Celeste.Mods.Randomizer.Generation.Configuration.Settings;
using Celeste.Simulation.Gameplay.Hazards.Spikes;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;

namespace Celeste.Mods.Randomizer.Gameplay
{
    public sealed partial class RandomizerGameplay
    {
        private void LoadVisualHooks()
        {
            hooks.Add(new Hook(
                typeof(PoemState).GetConstructor(new[]
                    { typeof(ISourceEngineContext), typeof(string), typeof(int), typeof(float) }),
                (Action<Action<PoemState, ISourceEngineContext, string, int, float>, PoemState, ISourceEngineContext,
                    string, int, float>)PoemColor));
            hooks.Add(new Hook(typeof(SpikesState).GetMethod(nameof(SpikesState.Render)),
                (Action<Action<SpikesState, ISourceRenderContext>, SpikesState, ISourceRenderContext>)TentacleOutline));
            hooks.Add(new ILHook(typeof(TheoCrystalState).GetMethod(nameof(TheoCrystalState.Update)), GracefulTheo));
            hooks.Add(new ILHook(
                typeof(OriginalHeartGemState).GetMethod("OriginalCollectRoutine", AllInstanceMethods)
                    .GetStateMachineTarget(), HeartSound));
            hooks.Add(new ILHook(typeof(OriginalHeartGemState).GetMethod(nameof(OriginalHeartGemState.Awake)),
                HeartSprites));
        }

        private void PoemColor(Action<PoemState, ISourceEngineContext, string, int, float> orig, PoemState poem,
            ISourceEngineContext owner, string text, int heartIndex, float alpha)
        {
            orig(poem, owner, text, heartIndex, alpha);
            if (!ReferenceEquals(owner, engine) || heartIndex == 3 || ActiveSettings is not { } settings) return;
            string color;
            string sprite;
            switch (settings.Difficulty)
            {
                case Difficulty.Easy:
                    color = "20c020";
                    sprite = "heartgem0";
                    break;
                case Difficulty.Normal:
                    color = "8cc7fa";
                    sprite = "heartgem0";
                    break;
                case Difficulty.Hard:
                    color = "ff668a";
                    sprite = "heartgem1";
                    break;
                case Difficulty.Expert:
                    color = "fffc24";
                    sprite = "heartgem2";
                    break;
                case Difficulty.Master:
                    color = "ff5024";
                    sprite = "Randomizer_HeartMaster";
                    break;
                case Difficulty.Perfect:
                    color = "9B3FB5";
                    sprite = "Randomizer_HeartPerfect";
                    break;
                default: throw new ArgumentOutOfRangeException();
            }

            typeof(PoemState).GetProperty(nameof(PoemState.Color)).SetValue(poem, SourceHexColor.HexToColor(color));
            poem.Heart = engine.GraphicsResources.GuiSpriteBank.Create(sprite);
            poem.Heart.Play("spin");
            poem.Heart.Position = new Vector2(1920f, 1080f) * .5f;
            poem.Heart.Color = PackedColor.White * alpha;
        }

        private static readonly FieldInfo
            SpikeTypeField = typeof(SpikesState).GetField("spikeType", AllInstanceMethods);

        private static readonly Action<SpikesState, Vector2> ShakeSpikes =
            (Action<SpikesState, Vector2>)typeof(SpikesState).GetMethod("OnShake", AllInstanceMethods)
                .CreateDelegate(typeof(Action<SpikesState, Vector2>));

        private void TentacleOutline(Action<SpikesState, ISourceRenderContext> orig, SpikesState spikes,
            ISourceRenderContext render)
        {
            if (spikes.Scene is OriginalLevelSceneRoot level && Owns(level) && Settings(level.Session) != null &&
                (string)SpikeTypeField.GetValue(spikes) == "tentacles")
            {
                spikes.SetSpikeColor(PackedColor.Black);
                ShakeSpikes(spikes, new Vector2(1, 0));
                orig(spikes, render);
                ShakeSpikes(spikes, new Vector2(-1, 1));
                orig(spikes, render);
                ShakeSpikes(spikes, new Vector2(-1, -1));
                orig(spikes, render);
                ShakeSpikes(spikes, new Vector2(1, -1));
                orig(spikes, render);
                ShakeSpikes(spikes, new Vector2(0, 1));
                spikes.SetSpikeColor(PackedColor.White);
            }

            orig(spikes, render);
        }

        private void GracefulTheo(ILContext il)
        {
            var cursor = new ILCursor(il);
            int matches = 0;
            while (cursor.TryGotoNext(MoveType.After, i => i.MatchCallOrCallvirt<OriginalLevelSceneRoot>("get_Bounds")))
            {
                cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
                cursor.EmitDelegate<Func<IntRect, TheoCrystalState, IntRect>>((bounds, theo) =>
                    theo.Scene is OriginalLevelSceneRoot level && Owns(level) && level.Transitioning &&
                    Settings(level.Session) != null
                        ? ((Celeste.Game.Content.Maps.OriginalChapterMapData)level.Session.MapData).Bounds
                        : bounds);
                matches++;
            }

            if (matches == 0) throw new InvalidOperationException("Theo transition bounds hook no longer matches.");
        }

        private void HeartSound(ILContext il)
        {
            var cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(MoveType.After, i => i.MatchLdstr("event:/game/general/crystalheart_blue_get")))
                throw new InvalidOperationException("Heart sound hook no longer matches.");
            cursor.EmitDelegate<Func<string, string>>(original => ActiveSettings?.Difficulty switch
            {
                Difficulty.Easy or Difficulty.Normal or Difficulty.Hard => "event:/game/general/crystalheart_blue_get",
                Difficulty.Expert or Difficulty.Master => "event:/game/general/crystalheart_red_get",
                Difficulty.Perfect => "event:/game/general/crystalheart_gold_get",
                _ => original
            });
        }

        private void HeartSprites(ILContext il)
        {
            var cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(MoveType.Before,
                    i => i.MatchCallOrCallvirt<Celeste.Graphics.Animation.ISpriteFactory>("Create")))
                throw new InvalidOperationException("Heart sprite hook no longer matches.");
            cursor.EmitDelegate<Func<string, string>>(original =>
            {
                if (original == "heartgem3" || ActiveSettings is not { } settings) return original;
                bool prideMode = AppDomain.CurrentDomain.GetAssemblies()
                    .Any(assembly => assembly.FullName.Contains("PrideHearts"));
                return settings.Difficulty switch
                {
                    Difficulty.Easy => prideMode ? "heartgem0" : "Randomizer_HeartEasy",
                    Difficulty.Normal => "heartgem0", Difficulty.Hard => "heartgem1", Difficulty.Expert => "heartgem2",
                    Difficulty.Master => prideMode ? "heartgem3" : "Randomizer_HeartMaster",
                    Difficulty.Perfect => prideMode ? "heartGemGhost" : "Randomizer_HeartPerfect",
                    _ => original
                };
            });
        }
    }
}
