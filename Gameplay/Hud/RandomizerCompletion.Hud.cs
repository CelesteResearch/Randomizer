using System;
using Celeste.Core.Scheduling;
using Celeste.Mods.Randomizer.Gameplay.Reflection;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using Celeste.Core.Display;
using Celeste.Core.Rendering;
using Celeste.Core.Mathematics.Easing;
using Celeste.Game.Chapters.Flow;
using Celeste.Game.Content.Dialogs;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Presentation.Hud.Speedrun;
using Celeste.Game.Settings;
using Celeste.Graphics.Display;
using Celeste.Graphics.Rendering;
using Celeste.Graphics.Content.Atlas;
using Celeste.Scene.Components.Oscillation;
using SpeedrunClockMode = Celeste.Simulation.Presentation.Hud.Speedrun.SpeedrunClockMode;
using Celeste.Mods.Randomizer.Generation.Configuration.Settings;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.Randomizer.Gameplay
{
    public sealed partial class RandomizerCompletion
    {
        private static readonly Func<SpeedrunTimerState, ISourceEngineContext> TimerEngine =
            RandomizerOwnerAccess.Create<SpeedrunTimerState>();

        private static readonly Func<OriginalAreaCompleteInfoState, ISourceEngineContext> InfoEngine =
            RandomizerOwnerAccess.Create<OriginalAreaCompleteInfoState>();

        private static readonly FieldInfo TimerBackground =
            typeof(SpeedrunTimerState).GetField("bg", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo TimerWiggler =
            typeof(SpeedrunTimerState).GetField("wiggler", BindingFlags.Instance | BindingFlags.NonPublic);

        private OriginalGameTextProcess HudText => ((IOriginalGameTextEnvironment)engine).Text;

        private RandoSettings PlayingSettings => engine.Frame.Scenes.CurrentScene is OriginalLevelSceneRoot level
            ? gameplay.Settings(level.Session)
            : null;

        private void LoadCompletionHud(List<IDisposable> hooks)
        {
            hooks.Add(new Hook(
                typeof(OriginalAreaCompleteInfoState).GetMethod("DrawOriginal",
                    BindingFlags.Instance | BindingFlags.NonPublic),
                (Action<Action<OriginalAreaCompleteInfoState, ISourceRenderContext, object, string, float, float, float>
                    , OriginalAreaCompleteInfoState, ISourceRenderContext, object, string, float, float, float>)
                AreaCompleteDrawHash));
            hooks.Add(new Hook(
                typeof(OriginalAreaCompleteInfoState).GetMethod(nameof(OriginalAreaCompleteInfoState.Info)),
                (Action<
                    Action<OriginalAreaCompleteInfoState, ISourceRenderContext, object, float, string, string, string,
                        string>, OriginalAreaCompleteInfoState, ISourceRenderContext, object, float, string, string,
                    string, string>)EndlessShowScore2));
            hooks.Add(new Hook(typeof(SpeedrunTimerState).GetMethod(nameof(SpeedrunTimerState.Render)),
                (Action<Action<SpeedrunTimerState, ISourceRenderContext>, SpeedrunTimerState, ISourceRenderContext>)
                EndlessShowScore));
            LoadCompletionHudIl(hooks);
        }

        private void AreaCompleteDrawHash(
            Action<OriginalAreaCompleteInfoState, ISourceRenderContext, object, string, float, float, float> orig,
            OriginalAreaCompleteInfoState self, ISourceRenderContext render, object owner, string version, float ease,
            float alpha, float offset)
        {
            orig(self, render, owner, version, ease, alpha, offset);
            if (!ReferenceEquals(InfoEngine(self), engine) || endingSettings == null) return;
            var settings = endingSettings;
            string text = settings.Seed;
            var save = engine.SaveContext.CurrentSave;
            if (!string.IsNullOrEmpty(settings.Rules))
            {
                text += " " + settings.Rules;
                if (save?.CurrentSession != null && gameplay.SessionState(save.CurrentSession).SeedCleanRandom)
                    text += "!";
            }

            text += "\n#" + settings.Hash + "\nrando " + context.Version;
            HudText.ActiveFont.DrawOutline(render, owner, text,
                new Vector2(1820f + 300f * (1f - ((ISourceEasingEnvironment)engine).EasingArithmetic.CubeOut(ease)),
                    save?.VariantMode == true ? 810f : 894f),
                new Vector2(.5f, 0f), Vector2.One * .5f,
                settings.SpawnGolden
                    ? Celeste.Graphics.Display.Colors.SourceHexColor.HexToColor("fad768")
                    : PackedColor.White, 2f, PackedColor.Black);
        }

        private void EndlessShowScore(Action<SpeedrunTimerState, ISourceRenderContext> orig, SpeedrunTimerState self,
            ISourceRenderContext render)
        {
            var settings = PlayingSettings;
            if (!ReferenceEquals(TimerEngine(self), engine) || settings == null ||
                settings.Algorithm != LogicType.Endless ||
                ((ISettingsContext)engine).Settings.SpeedrunClock == (int)SpeedrunClockMode.Off)
            {
                orig(self, render);
                return;
            }

            if (!(engine.Frame.Scenes.CurrentScene is OriginalLevelSceneRoot level)) return;
            float x = -300f * ((ISourceEasingEnvironment)engine).EasingArithmetic.CubeIn(1f - self.DrawLerp);
            var session = level.Session;
            var bg = (AtlasEntry)TimerBackground.GetValue(self);
            var wiggler = (WigglerComponent)TimerWiggler.GetValue(self);
            SourceAtlasDrawing.Draw(render, self, bg, new Vector2(x, self.Position.Y), Vector2.Zero, PackedColor.White,
                Vector2.One);
            SpeedrunTimerState.DrawTimeSource(engine, render, self, new Vector2(x + 32f, self.Position.Y + 44f),
                SpeedrunTimerState.ShortGameplayFormat(session.Time));
            SourcePrimitiveDrawing.Rect(render, self, x, self.Position.Y + 38f, 98f, 22.8f, PackedColor.Black);
            SourceAtlasDrawing.Draw(render, self, bg, new Vector2(x + 96f, self.Position.Y + 38f), Vector2.Zero,
                PackedColor.White, Vector2.One * .6f);
            SpeedrunTimerState.DrawTimeSource(engine, render, self,
                new Vector2(x + 32f, (float)((double)self.Position.Y + 40.0 + 26.400001525878906)),
                HudText.CleanDialog("RANDOENDLESS_SCORE") + " " + CurrentScore,
                (float)((1.0 + (double)wiggler.Value * .15000000596046448) * .6000000238418579),
                session.StartedFromBeginning, level.Completed, session.BeatBestTime, .6f);
        }

        private void EndlessShowScore2(
            Action<OriginalAreaCompleteInfoState, ISourceRenderContext, object, float, string, string, string, string>
                orig,
            OriginalAreaCompleteInfoState self, ISourceRenderContext render, object owner, float ease,
            string chapterTime, string fileTime, string chapterText, string version)
        {
            if (!ReferenceEquals(InfoEngine(self), engine))
            {
                orig(self, render, owner, ease, chapterTime, fileTime, chapterText, version);
                return;
            }

            var settings = engine.Frame.Scenes.CurrentScene is AreaCompleteSequence ? endingSettings : PlayingSettings;
            var options = ((ISettingsContext)engine).Settings;
            int saved = options.SpeedrunClock;
            bool endless = saved != (int)SpeedrunClockMode.Off && settings?.Algorithm == LogicType.Endless;
            try
            {
                if (endless) options.SpeedrunClock = (int)SpeedrunClockMode.Chapter;
                orig(self, render, owner, ease, chapterTime, fileTime, chapterText, version);
            }
            finally
            {
                options.SpeedrunClock = saved;
            }

            if (!endless) return;
            var at = new Vector2(
                (float)(80.0 -
                        300.0 * (1.0 - (double)((ISourceEasingEnvironment)engine).EasingArithmetic.CubeOut(ease))),
                1000f);
            string text = HudText.CleanDialog("RANDOENDLESS_SCORE");
            HudText.ActiveFont.DrawOutline(render, owner, text, at + new Vector2(0f, 40f), new Vector2(0f, 1f),
                Vector2.One * .6f, PackedColor.White, 2f, PackedColor.Black);
            SpeedrunTimerState.DrawTimeSource(engine, render, owner,
                at + new Vector2((float)((double)HudText.ActiveFont.Measure(text).X * .6000000238418579 + 8.0), 40f),
                CurrentScore.ToString(), .6f);
        }
    }
}
