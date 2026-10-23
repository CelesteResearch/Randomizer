using System;
using Celeste.Core.Audio;
using Celeste.Core.Scheduling;
using Celeste.Everest.Gameplay.Enemies.Badeline.Chasers;
using Celeste.Game.Audio;
using Celeste.Game.Entities.Ascension;
using Celeste.Game.Entities.BadelineDummy;
using Celeste.Game.Entities.Npcs.Oshiro;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Game.Gameplay.Enemies.AngryOshiro;
using Celeste.Game.Gameplay.Enemies.Badeline.Chasers;
using Celeste.Game.Presentation.Hud.Height;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Camera;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.Randomizer.Gameplay
{
    public sealed partial class RandomizerGameplay
    {
        private void LoadStoryQualityOfLife()
        {
            hooks.Add(new Hook(typeof(RoomPlayerSession).GetMethod("UpdateSummitLaunch", AllInstanceMethods),
                (Func<Func<RoomPlayerSession, int>, RoomPlayerSession, int>)SummitLaunchReset));
            hooks.Add(new Hook(typeof(OriginalAngryOshiroState).GetMethod(nameof(OriginalAngryOshiroState.Added)),
                (Action<Action<OriginalAngryOshiroState, object>, OriginalAngryOshiroState, object>)AddedStoryOshiro));
            hooks.Add(new Hook(typeof(BadelineOldsiteState).GetMethod(nameof(BadelineOldsiteState.Added)),
                (Action<Action<BadelineOldsiteState, object>, BadelineOldsiteState, object>)AddedStoryBadeline));
            hooks.Add(new Hook(typeof(EverestBadelineOldsiteState).GetMethod(nameof(EverestBadelineOldsiteState.Added)),
                (Action<Action<EverestBadelineOldsiteState, object>, EverestBadelineOldsiteState, object>)
                ((orig, badeline, scene) =>
                    AddedStoryBadeline((self, target) => orig((EverestBadelineOldsiteState)self, target), badeline,
                        scene))));
            hooks.Add(new ILHook(typeof(OriginalAngryOshiroState).GetMethod("ChaseUpdate", AllInstanceMethods),
                PatchOshiroTarget));
            hooks.Add(new ILHook(typeof(OriginalOshiroLobbyState).GetMethod(nameof(OriginalOshiroLobbyState.Added)),
                PatchOshiroMusic));
        }

        private int SummitLaunchReset(Func<RoomPlayerSession, int> orig, RoomPlayerSession player)
        {
            if (!(player.Level is OriginalLevelSceneRoot level) || !Owns(level) ||
                Settings(level.Session) == null || player.Y >= level.Bounds.Top) return orig(player);
            player.Position = level.Session.RespawnPoint.Value;
            var lockMode = level.CameraLockMode;
            level.CameraLockMode = RoomCameraLockMode.None;
            level.Camera.Position = level.GetFullCameraTargetAt(player, player.Position);
            level.CameraLockMode = lockMode;
            level.CameraUpwardMaxY = level.Camera.Y + 180f;
            OriginalAscendManagerState manager = null;
            Celeste.Scene.Entities.SceneEntityState fader = null;
            OriginalHeightDisplayState height = null;
            BadelineDummyPresentation badeline = null;
            foreach (var entity in level.Scheduler.Entities.Entities)
            {
                if (entity is OriginalAscendManagerState ascend) manager = ascend;
                if (entity.GetType().Name == "Fader") fader = (Celeste.Scene.Entities.SceneEntityState)entity;
                if (entity is OriginalHeightDisplayState display) height = display;
                if (entity is BadelineDummyPresentation dummy) badeline = dummy;
            }

            if (manager != null) level.Scheduler.RemoveEntity(manager);
            if (fader != null) level.Scheduler.RemoveEntity(fader);
            if (height != null) level.Scheduler.RemoveEntity(height);
            if (badeline != null) level.Scheduler.RemoveEntity(badeline);
            level.NextTransitionDuration = .65f;
            return 0;
        }

        private void AddedStoryOshiro(Action<OriginalAngryOshiroState, object> orig,
            OriginalAngryOshiroState oshiro, object scene)
        {
            orig(oshiro, scene);
            if (scene is OriginalLevelSceneRoot level && Owns(level) &&
                !level.Session.GetFlag("oshiro_resort_roof") &&
                level.Session.Level.StartsWith("Celeste/3-CelestialResort/A/roof00", StringComparison.Ordinal))
                oshiro.RemoveSelf();
        }

        private void AddedStoryBadeline(Action<BadelineOldsiteState, object> orig,
            BadelineOldsiteState badeline, object scene)
        {
            orig(badeline, scene);
            if (!(scene is OriginalLevelSceneRoot level) || !Owns(level) ||
                level.Session.GetFlag("evil_maddy_intro") ||
                !level.Session.Level.StartsWith("Celeste/2-OldSite/A/3", StringComparison.Ordinal)) return;
            foreach (var component in badeline.OwnedComponents)
                if (component is SimulationCoroutine)
                {
                    badeline.OwnedComponents.Remove(component);
                    break;
                }

            badeline.Hovering = false;
            badeline.Visible = true;
            badeline.Hair.Visible = false;
            badeline.Sprite.Play("pretendDead");
            if (level.Session.Mode == 0)
            {
                level.Session.Audio.Music.Event = null;
                OriginalAudioSession.Apply(level.Session.Audio,
                    ((IOriginalGameplayProcess)engine).AudioSession.ChannelOutput,
                    ((IOriginalGameplayProcess)engine).AudioApplyPolicy);
            }

            level.Scheduler.QueueSourceEntity(new BadelineIntroCutsceneState(engine, badeline));
        }

        private void PatchOshiroTarget(ILContext il)
        {
            var cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(MoveType.After,
                    instruction => instruction.MatchCall<OriginalAngryOshiroState>("get_TargetY")))
                throw new InvalidOperationException("Randomizer cannot find the real Oshiro chase target.");
            EmitStoryOwner(cursor);
            cursor.EmitDelegate<Func<float, object, float>>((target, owner) =>
            {
                var level = ResolveStoryLevel(owner);
                if (level == null || !Owns(level) || Settings(level.Session) == null) return target;
                var player =
                    level.Scheduler.Entities.GetTrackedEntity<RoomPlayerSession>(typeof(RoomPlayerSession).FullName);
                return player.Facing == -1 && player.X < level.Bounds.Left + 70 ? target - 50 : target;
            });
        }

        private void PatchOshiroMusic(ILContext il)
        {
            var cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(MoveType.After,
                    instruction => instruction.MatchCallvirt<EventTrackState>("set_Event")) ||
                !cursor.TryGotoNext(MoveType.Before,
                    instruction => instruction.MatchCallvirt<EventTrackState>("set_Event")))
                throw new InvalidOperationException("Randomizer cannot find the real Oshiro lobby music clearing.");
            cursor.Remove();
            EmitStoryOwner(cursor);
            cursor.EmitDelegate<Action<EventTrackState, string, object>>((music, track, owner) =>
            {
                if (!IsRandomizerStory(owner)) music.Event = track;
            });
        }
    }
}
