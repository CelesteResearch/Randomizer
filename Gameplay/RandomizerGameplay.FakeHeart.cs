using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Celeste.Core.Components;
using Celeste.Core.Scheduling;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Entities.Collectibles.Hearts;
using Celeste.Game.Entities.Triggers.Wind;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Game.Execution.Startup;
using Celeste.Input.Gameplay.Feedback;
using Celeste.Physics.Collision;
using Celeste.Simulation.Gameplay.Collision;
using Celeste.Scene.Entities;
using Celeste.Simulation;
using Celeste.Simulation.Audio.Sources;
using Celeste.Simulation.Gameplay.Player.Intro;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;

namespace Celeste.Mods.Randomizer.Gameplay
{
    public sealed partial class RandomizerGameplay
    {
        private void LoadFakeHeartHook()
            => hooks.Add(new ILHook(typeof(OriginalHeartGemState)
                .GetMethod("OriginalCollectRoutine", AllInstanceMethods)
                .GetStateMachineTarget(), PatchFakeHeart));

        private void PatchFakeHeart(ILContext il)
        {
            var cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(MoveType.After,
                    instruction => instruction.MatchCallOrCallvirt<RoomPlayerSession>("get_Dead")))
                throw new InvalidOperationException("Fake heart exit hook no longer matches.");
            cursor.EmitDelegate<Func<bool>>(BeginFakeHeartWarp);
            cursor.Emit(OpCodes.Or);
        }

        private bool BeginFakeHeartWarp()
        {
            if (engine.Frame.Scenes.CurrentScene is not OriginalLevelSceneRoot level || !Owns(level) ||
                Settings(level.Session) == null)
                return false;
            string destination = context.State(level.Session.LevelData).Get<string>("CustomWarp");
            if (destination == null) return false;
            var target = (OriginalLevelRoomData)level.Session.MapData.GetLevel(destination);
            var player = (RoomPlayerSession)level.Scheduler.Entities
                .GetTrackedEntities(typeof(RoomPlayerSession).FullName)[0];
            var entity =
                new SceneEntityState(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration);
            entity.Components.Add(new SimulationCoroutine(engine, FakeHeartWarp(level, player, target)));
            level.Scheduler.QueueSourceEntity(entity);
            return true;
        }

        private IEnumerator FakeHeartWarp(OriginalLevelSceneRoot level, RoomPlayerSession player,
            OriginalLevelRoomData target)
        {
            ((ISourceRumbleEnvironment)engine).RumbleSession.Rumble(2, 1);
            ((IOriginalGameplayProcess)engine).AudioSession.Play("event:/new_content/game/10_farewell/glitch_short");
            for (float elapsed = 0f; elapsed < .5f; elapsed += engine.RawDeltaTime)
            {
                engine.Effects.Glitch = elapsed * 2f;
                yield return null;
            }

            engine.Effects.TimeRate = 1f;
            player.Depth = 0;
            engine.Effects.Glitch = 0f;
            level.Session.Audio.Music.Event = "event:/new_content/music/lvl10/intermission_heartgroove";
            level.Scheduler.OnEndOfFrame += () => FinishFakeHeartWarp(level, player, target);
        }

        private void FinishFakeHeartWarp(OriginalLevelSceneRoot level, RoomPlayerSession player,
            OriginalLevelRoomData target)
        {
            var entities = level.Scheduler.Entities;
            var tags = ((IOriginalGameTagsEnvironment)engine).Tags;
            var toRemove = entities.GetEntitiesExcludingTagMask(tags.Persistent.Value | tags.Global.Value);
            var outgoing = CopyFakeHeartListeners(level);
            player.CleanUpPlayerTriggers();
            foreach (var component in level.Scheduler.Components.GetTrackedComponents("Celeste.SoundSource"))
                if (component is SoundSourceComponentState { DisposeOnTransition: true } sound)
                    sound.Stop();
            level.TeleportTo(player, level.Session.Level, PlayerIntroType.Transition, target.Spawns[0]);
            ((IOriginalGameplayProcess)engine).AudioSession.ChannelOutput.SetAmbienceParam("has_conveyors",
                entities.GetTrackedEntities("Celeste.WallBooster").Count > 0 ? 1f : 0f);
            var incoming = CopyFakeHeartListeners(level);
            incoming.RemoveAll(outgoing.Contains);
            level.CameraUpwardMaxY = ((OriginalLevelRoomData)level.Session.LevelData).Bounds.Bottom;
            var camera = level.GetFullCameraTargetAt(player, player.Position);
            var position = player.Position;
            foreach (var entity in SourceSceneCollisionQueries.All((ISourceEntityCollider)player,
                         entities.GetTrackedEntities("Celeste.WindTrigger")))
            {
                if (toRemove.Contains(entity)) continue;
                level.WindController.SetPattern(((OriginalWindTrigger)entity).Pattern);
                break;
            }

            level.WindController.SetStartPattern();
            player.Position = position;
            foreach (var listener in outgoing) listener.OnOutBegin?.Invoke();
            foreach (var listener in incoming) listener.OnInBegin?.Invoke();
            level.Camera.Position = camera;
            foreach (var listener in incoming) listener.OnInEnd?.Invoke();
        }

        private static List<RoomTransitionListener> CopyFakeHeartListeners(OriginalLevelSceneRoot level)
            => level.Scheduler.Components.GetTrackedComponents("Celeste.TransitionListener")
                .Cast<RoomTransitionListener>().ToList();
    }
}
