using Celeste.Mods.Randomizer.Generation;
using Celeste.Mods.Randomizer.Generation.Configuration.Settings;
using Celeste.Mods.Randomizer.Runtime;
using Celeste.Mods.Randomizer.Gameplay;
using Celeste.Mods.Randomizer.UI;
using Celeste.Mods.Randomizer.Settings;
using Celeste.Game.Chapters.Flow;
using Celeste.Game.Execution.Areas;
using Celeste.Game.Execution.Processes;
using Celeste.Game.Execution.Session;
using Celeste.Game.Save.IO;
using Celeste.Game.Save.Runtime;
using Celeste.Scene.Scenes;
using Celeste.Simulation.Presentation.Scene.Transitions;
using Celeste.Simulation.Runtime.Session;

namespace Celeste.Mods.Randomizer
{
    public partial class RandoModule
    {
        public OriginalAreaKey? StartMe;
        public ChapterCompletionSession UseSession;

        public void LaunchIntoRandoArea(OriginalAreaKey newArea)
        {
            ResetCachedSettings();
            var area = Context.GetArea(newArea);
            var areaSettings = Context.State(area).Get<RandoSettings>("RandoSettings");
            var process = (IOriginalGameplayProcess)SourceEngine;
            process.AudioSession.SetMusic(null);
            process.AudioSession.SetAmbience(null);
            process.AudioSession.Play("event:/ui/main/savefile_begin");

            ((IOriginalSaveDebugEnvironment)SourceEngine).SaveDebug.InitializeDebugMode(
                ((IOriginalUserIOStorageEnvironment)SourceEngine).UserIOStorage,
                (IOriginalSaveInitializationServices)SourceEngine);
            var save = process.SaveContext.CurrentSave;
            save.VariantMode = areaSettings.Variants;
            save.AssistMode = false;
            save.AssistModeChecks((IOriginalSaveInitializationServices)SourceEngine);
            var areaStats = ((IOriginalSaveCounterEnvironment)SourceEngine).SaveCounters.GetAreasSafe(save)[newArea.ID];
            areaStats.Modes[0].Completed = areaSettings.SpawnGolden;
            areaStats.Modes[0].HeartGem = false;
            Entering = true;

            _ = new FadeWipePresentation((SourceSceneState)SourceEngine.Frame.Scenes.CurrentScene, false, () =>
            {
                ChapterCompletionSession session;
                if (UseSession != null)
                {
                    session = UseSession;
                    UseSession = null;
                }
                else
                {
                    session = ((IOriginalSessionEnvironment)SourceEngine).Sessions.CreateSession(newArea);
                    session.FirstLevel = true;
                    session.StartedFromBeginning = true;
                    Gameplay.SessionState(session).SeedCleanRandom = Settings.SeedType == SeedType.Random;
                }

                session.OldStats.Modes[0].HeartGem = false;
                string postcard = null;
                if (areaSettings.EndlessLevel == 0 && !areaSettings.SpawnGolden)
                {
                    int count = SavedData.StartCounter;
                    if (count % 3 == 0)
                    {
                        int postnum = count / 3;
                        if (Context.HasDialog("RANDOCARD_" + postnum))
                        {
                            postcard = Context.GetDialog("RANDOCARD_" + postnum);
                            count++;
                        }
                    }
                    else
                    {
                        count++;
                    }

                    SavedData.StartCounter = count;
                    SaveSettings();
                }

                if (postcard != null)
                    Context.Dialog[area.Name + "_postcard"] = postcard;
                else
                    Context.Dialog.Remove(area.Name + "_postcard");

                ((IOriginalSaveSessionStartEnvironment)SourceEngine).SaveSessionStart.StartSession(save, session);
                StartMe = null;
                Entering = false;
                var enter = (IOriginalLevelEnterProcess)SourceEngine;
                enter.LevelEnterPolicy.Go(session, false, enter);
            });
        }
    }
}
