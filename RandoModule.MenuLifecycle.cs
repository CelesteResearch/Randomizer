using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using Celeste.Core.Scheduling;
using Celeste.Game.Audio.Mixing;
using Celeste.Game.Chapters.Flow;
using Celeste.Game.Execution.Areas;
using Celeste.Game.Execution.Engine;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Game.Menus.Overworld;
using Celeste.Game.Presentation.Environment;
using Celeste.Game.Save.Runtime;
using Celeste.Mods.Randomizer.Generation.Configuration.Settings;
using Celeste.Mods.Randomizer.UI;
using Celeste.Scene.Lifecycle;
using Celeste.Simulation.Runtime.Session;
using Celeste.Simulation.UI.Menus.Text;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.Randomizer
{
    public partial class RandoModule
    {
        public const OverworldStartMode StartModeRandomizer = (OverworldStartMode)55;
        private readonly List<IDisposable> menuLifecycleHooks = new();
        private bool menuLifecycleLoaded;
        private RandoSettings cachedSettings;
        private OriginalAreaKey? cachedSettingsKey;

        internal void ResetCachedSettings()
        {
            cachedSettingsKey = null;
            cachedSettings = null;
            Gameplay?.InvalidateSettingsCache();
        }

        public RandoSettings InRandomizerSettings
        {
            get
            {
                var save = ((IOriginalGameplayProcess)SourceEngine).SaveContext.CurrentSave;
                if (save == null || Gameplay == null) return null;
                var session = ((IOriginalSaveSessionSelectionEnvironment)SourceEngine).SaveSessionSelection
                    .GetCurrentSessionSafe(save);
                if (session == null) return null;
                var key = new OriginalAreaKey { ID = session.AreaID, Mode = session.Mode };
                if (cachedSettingsKey != null && key == cachedSettingsKey.Value) return cachedSettings;
                cachedSettingsKey = key;
                return cachedSettings = Gameplay.Settings(session);
            }
        }

        public bool InRandomizer => InRandomizerSettings != null;

        private void LoadMenuLifecycle()
        {
            try
            {
                menuLifecycleHooks.Add(new Hook(
                    typeof(OverworldLoaderSequence).GetConstructor(new[]
                        { typeof(ISourceEngineContext), typeof(OverworldStartMode), typeof(HiresSnowState) }),
                    (Action<Action<OverworldLoaderSequence, ISourceEngineContext, OverworldStartMode, HiresSnowState>,
                        OverworldLoaderSequence, ISourceEngineContext, OverworldStartMode, HiresSnowState>)
                    EnterToRandoMenu));
                menuLifecycleHooks.Add(new Hook(
                    typeof(LevelExitSequence).GetConstructor(new[]
                    {
                        typeof(LevelExitMode), typeof(ChapterCompletionSession), typeof(ISourceEngineContext),
                        typeof(HiresSnowState), typeof(IInstalledLevelExitConstructorPolicy)
                    }),
                    (Action<Action<LevelExitSequence, LevelExitMode, ChapterCompletionSession, ISourceEngineContext,
                            HiresSnowState, IInstalledLevelExitConstructorPolicy>, LevelExitSequence, LevelExitMode,
                        ChapterCompletionSession, ISourceEngineContext, HiresSnowState,
                        IInstalledLevelExitConstructorPolicy>)EndlessCantRestart));
                menuLifecycleHooks.Add(new Hook(
                    typeof(OriginalLevelSceneRoot).GetMethod("GiveUp", BindingFlags.Instance | BindingFlags.NonPublic,
                        null, new[] { typeof(int), typeof(bool), typeof(bool), typeof(bool) }, null),
                    (Action<Action<OriginalLevelSceneRoot, int, bool, bool, bool>, OriginalLevelSceneRoot, int,
                        bool, bool, bool>)EndlessCantRestartSnarky));
                menuLifecycleHooks.Add(new Hook(
                    typeof(OverworldSequence).GetConstructor(new[]
                        { typeof(ISourceEngineContext), typeof(OverworldLoaderSequence) }),
                    (Action<Action<OverworldSequence, ISourceEngineContext, OverworldLoaderSequence>,
                        OverworldSequence, ISourceEngineContext, OverworldLoaderSequence>)HideMaddy));
                menuLifecycleHooks.Add(new Hook(
                    typeof(TextMenuPresentation).GetMethod(nameof(TextMenuPresentation.MoveSelection),
                        new[] { typeof(int), typeof(bool) }),
                    (Action<Action<TextMenuPresentation, int, bool>, TextMenuPresentation, int, bool>)
                    DisableMenuMovement));
                Events.OnCreatePauseMenuButtons += ModifyLevelMenu;
                menuLifecycleLoaded = true;
            }
            catch
            {
                UnloadMenuLifecycle();
                throw;
            }
        }

        private void UnloadMenuLifecycle()
        {
            if (menuLifecycleLoaded) Events.OnCreatePauseMenuButtons -= ModifyLevelMenu;
            menuLifecycleLoaded = false;
            for (int i = menuLifecycleHooks.Count - 1; i >= 0; i--) menuLifecycleHooks[i].Dispose();
            menuLifecycleHooks.Clear();
        }

        internal static OverworldStartMode ReturnStartMode(OverworldStartMode startMode, bool inRandomizer)
            => inRandomizer &&
               (startMode == OverworldStartMode.MainMenu || startMode == OverworldStartMode.AreaComplete)
                ? StartModeRandomizer
                : startMode;

        internal static LevelExitMode EndlessExitMode(LevelExitMode mode, bool endless)
            => endless && (mode == LevelExitMode.Restart || mode == LevelExitMode.GoldenBerryRestart)
                ? LevelExitMode.SaveAndQuit
                : mode;

        private void EnterToRandoMenu(
            Action<OverworldLoaderSequence, ISourceEngineContext, OverworldStartMode, HiresSnowState> original,
            OverworldLoaderSequence loader, ISourceEngineContext engine, OverworldStartMode startMode,
            HiresSnowState snow)
        {
            if (ReferenceEquals(engine, SourceEngine)) startMode = ReturnStartMode(startMode, InRandomizer);
            original(loader, engine, startMode, snow);
        }

        private void EndlessCantRestart(
            Action<LevelExitSequence, LevelExitMode, ChapterCompletionSession, ISourceEngineContext, HiresSnowState,
                IInstalledLevelExitConstructorPolicy> original,
            LevelExitSequence exit, LevelExitMode mode, ChapterCompletionSession session, ISourceEngineContext engine,
            HiresSnowState snow, IInstalledLevelExitConstructorPolicy policy)
        {
            if (ReferenceEquals(engine, SourceEngine))
                mode = EndlessExitMode(mode, InRandomizerSettings?.Algorithm == LogicType.Endless);
            original(exit, mode, session, engine, snow, policy);
        }

        private void HideMaddy(Action<OverworldSequence, ISourceEngineContext, OverworldLoaderSequence> original,
            OverworldSequence overworld, ISourceEngineContext engine, OverworldLoaderSequence loader)
        {
            original(overworld, engine, loader);
            if (ReferenceEquals(engine, SourceEngine) && InRandomizer) overworld.Maddy.Hide();
        }

        private void DisableMenuMovement(Action<TextMenuPresentation, int, bool> original,
            TextMenuPresentation menu, int direction, bool wiggle)
        {
            if (ReferenceEquals(menu.EngineContext, SourceEngine) &&
                menu is DisablableTextMenu { DisableMovement: true }) return;
            original(menu, direction, wiggle);
        }

        private void ModifyLevelMenu(OriginalLevelSceneRoot level, TextMenuPresentation pauseMenu, bool minimal)
        {
            if (!ReferenceEquals(level.EngineContext, SourceEngine)) return;
            RandoSettings settings = InRandomizerSettings;
            if (settings == null) return;
            foreach (TextMenuPresentation.Item item in new List<TextMenuPresentation.Item>(pauseMenu.Items))
            {
                if (item.GetType() != typeof(TextMenuPresentation.Button)) continue;
                var button = (TextMenuPresentation.Button)item;
                if (button.Label == Clean("MENU_PAUSE_SAVEQUIT") || button.Label == Clean("MENU_PAUSE_RETURN"))
                    pauseMenu.Remove(item);
                if (button.Label != Clean("MENU_PAUSE_RESTARTAREA")) continue;
                if (settings.Algorithm == LogicType.Endless) pauseMenu.Remove(item);
                else button.Label = Clean("MENU_PAUSE_RESTARTRANDO");
            }

            int returnIndex = pauseMenu.Items.Count;
            pauseMenu.Add(new TextMenuPresentation.Button(Clean("MENU_PAUSE_QUITRANDO")).Pressed(() =>
            {
                level.PauseMainMenuOpen = false;
                pauseMenu.RemoveSelf();
                ShowQuitRandomizer(level, returnIndex, minimal);
            }));
        }

        private void ShowQuitRandomizer(OriginalLevelSceneRoot level, int returnIndex, bool minimal)
        {
            var graphics = ((IOriginalEngineGraphicsEnvironment)SourceEngine).EngineGraphics;
            var menu = new TextMenuPresentation(SourceEngine)
            {
                AutoScroll = false,
                Position = new Vector2(graphics.Width / 2f, graphics.Height / 2f - 100f)
            };
            menu.Add(new TextMenuPresentation.Header(Clean("MENU_QUITRANDO_TITLE")));
            menu.Add(new TextMenuPresentation.Button(Clean("MENU_QUITRANDO_CONFIRM")).Pressed(() =>
            {
                SourceEngine.Effects.TimeRate = 1f;
                menu.Focused = false;
                level.Session.InArea = false;
                var audio = ((IOriginalGameplayProcess)SourceEngine).AudioSession;
                audio.SetMusic(null);
                audio.Runtime.StopGameplaySfx(true);
                level.DoScreenWipe(false, () =>
                {
                    SourceEngine.Frame.Scenes.NextScene = ((IOriginalLevelExitEnvironment)SourceEngine).LevelExits
                        .CreateLevelExit(LevelExitMode.SaveAndQuit, level.Session, level.HiresSnow);
                }, true);
                foreach (ILevelEndingCallback component in level.Scheduler.Components
                             .GetTrackedComponents("Celeste.LevelEndingHook"))
                    component.OnEnd?.Invoke();
            }));
            menu.Add(new TextMenuPresentation.Button(Clean("MENU_QUITRANDO_CANCEL")).Pressed(() => menu.OnCancel()));
            BindConfirmationExit(menu, level, returnIndex, minimal);
            level.Scheduler.QueueSourceEntity(menu);
        }

        private void EndlessCantRestartSnarky(Action<OriginalLevelSceneRoot, int, bool, bool, bool> original,
            OriginalLevelSceneRoot level, int returnIndex, bool restartArea, bool minimal, bool showHint)
        {
            if (!ReferenceEquals(level.EngineContext, SourceEngine) ||
                InRandomizerSettings?.Algorithm != LogicType.Endless || !restartArea)
            {
                original(level, returnIndex, restartArea, minimal, showHint);
                return;
            }

            level.Paused = true;
            var menu = new TextMenuPresentation(SourceEngine) { AutoScroll = false };
            menu.Add(new TextMenuPresentation.Header(Clean("MENU_CANTRESTART_HEADER")));
            BindConfirmationExit(menu, level, returnIndex, minimal);
            level.Scheduler.QueueSourceEntity(menu);
        }

        private void BindConfirmationExit(TextMenuPresentation menu, OriginalLevelSceneRoot level,
            int returnIndex, bool minimal)
        {
            menu.OnPause = menu.OnESC = () =>
            {
                menu.RemoveSelf();
                level.Paused = false;
                SourceEngine.Effects.FreezeTimer = .15f;
                Play("event:/ui/game/unpause");
            };
            menu.OnCancel = () =>
            {
                Play("event:/ui/main/button_back");
                menu.RemoveSelf();
                level.Pause(returnIndex, minimal, false);
            };
        }
    }
}
