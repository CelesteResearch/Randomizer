using Celeste.Mods.Randomizer.Generation;
using Celeste.Mods.Randomizer.Generation.Configuration.Settings;
using Celeste.Mods.Randomizer.Runtime;
using Celeste.Mods.Randomizer.Gameplay;
using Celeste.Mods.Randomizer.UI;
using Celeste.Mods.Randomizer.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Runtime;
using Celeste.Everest.Runtime.Events;
using Celeste.Game.Content.Dialogs;
using Celeste.Game.Controls.Source;
using Celeste.Game.Execution.Processes;
using Celeste.Game.Menus.Title;
using Celeste.Game.Runtime;
using Celeste.Game.UI.Menus.Buttons;
using Celeste.Game.UI.Menus.Buttons.Title;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Celeste.Input.Runtime;
using Celeste.Input.Devices.Keyboard;
using Celeste.Everest.Diagnostics.Logging;
using Celeste.Audio.Fmod.Events;
using Celeste.Simulation.UI.Menus.Text;

namespace Celeste.Mods.Randomizer
{
    public partial class RandoModule : SourceEverestModule
    {
        public const int MAX_SEED_CHARS = 20;

        public RandoModule(ISourceEngineContext engine) : base(engine)
        {
        }

        public override Type SettingsType => typeof(RandoModuleSettings);
        public RandoModuleSettings SavedData => (RandoModuleSettings)_Settings;
        public GenerationContext Context { get; private set; }
        public RandoSettings Settings { get; private set; }
        public string VersionString => Metadata.VersionString;
        public bool Entering { get; set; }
        public RandomizerGameplay Gameplay { get; private set; }
        public RandomizerCompletion Completion { get; private set; }
        internal CancellationTokenSource GenerationCancellation { get; set; }
        internal Task<Celeste.Game.Execution.Areas.OriginalAreaKey> GenerationTask { get; set; }
        private bool generatedContentInitialized;

        public static RandoModule For(ISourceEngineContext engine) =>
            ((ISourceEverestModulesEnvironment)engine).Modules.OfType<RandoModule>().Single();

        private EverestGameEventsProcess Events => ((IEverestGameEventsEnvironment)SourceEngine).GameEvents;
        public string Clean(string key) => ((IOriginalGameTextEnvironment)SourceEngine).Text.Dialog.Clean(key);
        public void Play(string path) => ((IOriginalGameplayProcess)SourceEngine).AudioSession.Play(path);

        public void LogError(Exception error) =>
            ((IEverestLoggerProcessEnvironment)SourceEngine).Logger.LogDetailed(error, "randomizer");

        public override void CreateModMenuSection(TextMenuPresentation menu, bool inGame, FmodEventReference snapshot)
        {
            if (!inGame) base.CreateModMenuSection(menu, false, snapshot);
        }

        public override void Load()
        {
            Settings = SavedData.SavedSettings?.Copy() ?? new RandoSettings();
            if (SavedData.CurrentVersion != VersionString)
            {
                SavedData.CurrentVersion = VersionString;
                SavedData.BestTimes = new Dictionary<uint, long>();
            }

            SavedData.BestSetSeedTimes ??= new Dictionary<string, RecordTuple>();
            SavedData.BestRandomSeedTimes ??= new Dictionary<string, RecordTuple>();
            LoadInterop();
            LoadMenuLifecycle();
            Events.OnCreateButtons += CreateMainMenuButton;
        }

        public override void LoadContent(bool firstLoad)
        {
            if (!firstLoad) return;
            var context = new EngineGenerationContext((GameApplication)SourceEngine, Metadata.Version)
                { Settings = Settings, LazyLoading = SavedData.LazyLoading };
            var gameplay = new RandomizerGameplay((GameApplication)SourceEngine, context, variantResetImports);
            RandomizerCompletion completion = null;
            try
            {
                gameplay.Load();
                completion = new RandomizerCompletion(this, (GameApplication)SourceEngine, context, gameplay);
                completion.Load();
            }
            catch
            {
                completion?.Dispose();
                gameplay.Dispose();
                throw;
            }

            Context = context;
            Settings.Context = context;
            Gameplay = gameplay;
            Completion = completion;
        }

        public void EnsureGeneratedContent()
        {
            if (generatedContentInitialized) return;
            ((EngineGenerationContext)Context).InitializeContent();
            Settings.PruneMaps();
            if (SavedData.SavedSettings == null) Settings.SetNormalMaps();
            generatedContentInitialized = true;
        }

        public override void Unload()
        {
            RetireMenuGeneration(true);
            Exception interopFailure = null;
            try
            {
                UnloadInterop();
            }
            catch (Exception failure)
            {
                interopFailure = failure;
            }

            Events.OnCreateButtons -= CreateMainMenuButton;
            UnloadMenuLifecycle();
            Completion?.Dispose();
            Completion = null;
            Gameplay?.Dispose();
            Gameplay = null;
            if (interopFailure != null)
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(interopFailure).Throw();
        }

        internal void RetireMenuGeneration(bool cancel)
        {
            var cancellation = GenerationCancellation;
            var task = GenerationTask;
            GenerationCancellation = null;
            GenerationTask = null;
            if (cancellation == null) return;
            if (cancel) cancellation.Cancel();
            if (task == null || task.IsCompleted)
            {
                _ = task?.Exception;
                cancellation.Dispose();
            }
            else
                _ = task.ContinueWith(completed =>
                {
                    _ = completed.Exception;
                    cancellation.Dispose();
                }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }

        public void PersistSettings()
        {
            SavedData.SavedSettings = Settings.Copy();
            SaveSettings();
        }

        private void CreateMainMenuButton(MainMenuSequence menu, List<MenuButtonPresentation> buttons)
        {
            EnsureGeneratedContent();
            var app = (GameApplication)SourceEngine;
            buttons.Insert(1, new MainMenuSmallButtonPresentation("MODOPTIONS_RANDOMIZER_TOPMENU", "menu/randomizer",
                menu, Vector2.Zero, Vector2.Zero, () =>
                {
                    Play("event:/ui/main/button_select");
                    Play("event:/ui/main/whoosh_large_in");
                    var input = ((IOriginalGameInputProcessEnvironment)SourceEngine).InputSource;
                    bool quickMenu = input.MenuJournal.Check ||
                                     ((ISourceMInputEnvironment)SourceEngine).MInput.Keyboard.Check(
                                         SourceKeys.LeftShift);
                    if (SavedData.FastMenu ^ quickMenu)
                        menu.Overworld.Goto<OuiRandoSettings>();
                    else menu.Overworld.Goto<OuiRandoMode>();
                }, app.GuiAtlas, SourceEngine));
        }
    }
}
