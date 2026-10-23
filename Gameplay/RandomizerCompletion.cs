using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Celeste.Everest.Chapters.Flow;
using Celeste.Game.Chapters.Flow;
using Celeste.Game.Execution.Areas;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Session;
using Celeste.Game.Diagnostics.Speedrun;
using Celeste.Game.Runtime;
using Celeste.Mods.Randomizer.Generation;
using Celeste.Mods.Randomizer.Generation.Configuration.Settings;
using Celeste.Mods.Randomizer.Generation.Errors;
using Celeste.Simulation.Gameplay.Player.Intro;
using Celeste.Simulation.Runtime.Session;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.Randomizer.Gameplay
{
    public sealed partial class RandomizerCompletion : IDisposable
    {
        private readonly RandoModule module;
        private readonly GameApplication engine;
        private readonly GenerationContext context;
        private readonly RandomizerGameplay gameplay;
        private readonly List<IDisposable> hooks = new List<IDisposable>();
        private RandoSettings endingSettings;
        private Task<OriginalAreaKey> nextMap;
        private CancellationTokenSource generationCancellation;
        private bool loaded;
        private bool disposed;

        public int CurrentScore { get; private set; } = -1;

        public RandomizerCompletion(RandoModule module, GameApplication engine, GenerationContext context,
            RandomizerGameplay gameplay)
        {
            this.module = module ?? throw new ArgumentNullException(nameof(module));
            this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
            this.context = context ?? throw new ArgumentNullException(nameof(context));
            this.gameplay = gameplay ?? throw new ArgumentNullException(nameof(gameplay));
        }

        public void Load()
        {
            if (loaded || disposed)
                throw new InvalidOperationException("Randomizer completion is not available for loading.");
            try
            {
                hooks.Add(new Hook(typeof(LevelExitSequence).GetMethod(nameof(LevelExitSequence.Begin)),
                    (Action<Action<LevelExitSequence>, LevelExitSequence>)BeginExit));
                hooks.Add(new ILHook(typeof(AreaCompleteSequence).GetMethod(nameof(AreaCompleteSequence.Update)),
                    PatchEndlessConfirm));
                hooks.Add(new Hook(
                    typeof(EverestAreaCompleteInfoState).GetMethod(nameof(EverestAreaCompleteInfoState
                        .InitializeInstalled)),
                    (Action<Action<EverestAreaCompleteInfoState, bool, ChapterCompletionSession>,
                        EverestAreaCompleteInfoState, bool, ChapterCompletionSession>)InitializeInstalledInfo));
                hooks.Add(new Hook(typeof(OriginalAutoSplitterInfo).GetMethod(nameof(OriginalAutoSplitterInfo.Update)),
                    (Action<Action<OriginalAutoSplitterInfo>, OriginalAutoSplitterInfo>)UpdateFrame));
                hooks.Add(new ILHook(typeof(Celeste.Game.Editor.MapEditorState).GetConstructors()[0],
                    MarkSessionUnclean));
                engine.GameEvents.OnComplete += Complete;
                engine.GameEvents.OnLoadLevel += LoadedLevel;
                loaded = true;
                LoadCompletionHud(hooks);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            generationCancellation?.Cancel();
            if (loaded)
            {
                engine.GameEvents.OnComplete -= Complete;
                engine.GameEvents.OnLoadLevel -= LoadedLevel;
            }

            for (int i = hooks.Count - 1; i >= 0; i--) hooks[i].Dispose();
            hooks.Clear();
            if (generationCancellation != null)
            {
                var cancellation = generationCancellation;
                if (nextMap == null || nextMap.IsCompleted) cancellation.Dispose();
                else
                    _ = nextMap.ContinueWith(_ => cancellation.Dispose(), CancellationToken.None,
                        TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            }
        }

        private void LoadedLevel(OriginalLevelSceneRoot level, PlayerIntroType intro, bool fromLoader)
        {
            if (ReferenceEquals(level.EngineContext, engine)) endingSettings = null;
        }

        private void UpdateScore(OriginalLevelSceneRoot level)
        {
            if (!ReferenceEquals(level.EngineContext, engine)) return;
            var settings = gameplay.Settings(level.Session);
            CurrentScore = settings?.Algorithm == LogicType.Endless
                ? RandomizerGameplay.ComputeScore(settings, level.Session.Time,
                    gameplay.SessionState(level.Session).CarryingLifeBerries)
                : -1;
        }

        private void UpdateFrame(Action<OriginalAutoSplitterInfo> orig, OriginalAutoSplitterInfo info)
        {
            orig(info);
            if (!ReferenceEquals(info, engine.AutoSplitter)) return;
            module.UpdateInterop();
            var session = engine.SaveContext.CurrentSave?.CurrentSession;
            var settings = engine.Frame.Scenes.CurrentScene is AreaCompleteSequence
                ? endingSettings
                : session == null
                    ? null
                    : gameplay.Settings(session);
            CurrentScore = settings?.Algorithm == LogicType.Endless && session != null
                ? RandomizerGameplay.ComputeScore(settings, session.Time,
                    gameplay.SessionState(session).CarryingLifeBerries)
                : -1;
        }

        private void Complete(OriginalLevelSceneRoot level)
        {
            if (!ReferenceEquals(level.EngineContext, engine)) return;
            var state = gameplay.SessionState(level.Session);
            state.BeatBestTimePlatinum = false;
            var settings = gameplay.Settings(level.Session);
            if (settings == null || !level.Session.StartedFromBeginning) return;
            UpdateScore(level);
            var records = RandomizerRecords.Complete(module.SavedData, settings, level.Session.Time,
                state.SeedCleanRandom, CurrentScore);
            level.Session.BeatBestTime = records.BeatBestTime;
            state.BeatBestTimePlatinum = records.BeatBestTimePlatinum;
            module.SaveSettings();
        }

        private void BeginExit(Action<LevelExitSequence> orig, LevelExitSequence exit)
        {
            orig(exit);
            if (!ReferenceEquals(exit.EngineContext, engine)) return;
            endingSettings = gameplay.Settings(exit.Session);
            if (endingSettings?.Algorithm != LogicType.Endless || exit.Mode != LevelExitMode.Completed) return;
            if (nextMap != null)
                throw new InvalidOperationException("Randomizer already has an Endless map generation pending.");
            var nextSettings = endingSettings.Copy();
            nextSettings.EndlessLevel++;
            engine.AudioSession.SetMusic("event:/music/complete/bside");
            engine.AudioSession.SetAmbience(null);
            generationCancellation = new CancellationTokenSource();
            context.CancellationToken = generationCancellation.Token;
            nextMap = Task.Run(() => new RandoLogic(context).GenerateMap(nextSettings), generationCancellation.Token);
        }

        private void InitializeInstalledInfo(Action<EverestAreaCompleteInfoState, bool, ChapterCompletionSession> orig,
            EverestAreaCompleteInfoState info, bool pie, ChapterCompletionSession session)
        {
            var actual = ((IOriginalAreaCompleteInfoEnvironment)engine).AreaCompleteInfo;
            orig(info, pie, ReferenceEquals(info, actual) && endingSettings != null ? null : session);
        }

        private bool ConfirmEndless(AreaCompleteSequence complete)
        {
            if (!ReferenceEquals(complete.EngineContext, engine) ||
                endingSettings?.Algorithm != LogicType.Endless) return false;
            if (nextMap == null)
                throw new InvalidOperationException("Randomizer completion has no pending Endless map.");
            if (!nextMap.IsCompleted)
            {
                CanConfirm.SetValue(complete, true);
                return true;
            }

            OriginalAreaKey area;
            try
            {
                area = nextMap.GetAwaiter().GetResult();
            }
            catch (GenerationError error)
            {
                var enter = (IOriginalLevelEnterProcess)engine;
                enter.LevelEnterStatics.ErrorMessage = error.Message ?? "Failed to generate area";
                enter.LevelEnterPolicy.Go(
                    ((IOriginalSessionEnvironment)engine).Sessions.CreateSession(new OriginalAreaKey
                        { ID = 1, SID = string.Empty }), false, enter);
                RetireGeneration();
                return true;
            }

            var old = engine.SaveContext.CurrentSave.CurrentSession;
            var next = ((IOriginalSessionEnvironment)engine).Sessions.CreateSession(area);
            next.Time = old.Time;
            next.Deaths = old.Deaths;
            var oldState = gameplay.SessionState(old);
            var state = gameplay.SessionState(next);
            state.SeedCleanRandom = oldState.SeedCleanRandom;
            state.CarryingLifeBerries = oldState.CarryingLifeBerries;
            foreach (var taken in oldState.TakenLifeBerries) state.TakenLifeBerries.Add(taken);
            module.UseSession = next;
            module.StartMe = area;
            RetireGeneration();
            module.LaunchIntoRandoArea(area);
            return true;
        }

        private void RetireGeneration()
        {
            nextMap = null;
            generationCancellation?.Dispose();
            generationCancellation = null;
            context.CancellationToken = CancellationToken.None;
        }
    }
}
