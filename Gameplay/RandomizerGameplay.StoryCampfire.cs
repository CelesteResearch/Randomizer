using System;
using Celeste.Game.Entities.StarJump.Cutscenes;
using Celeste.Game.Execution.Levels;
using Celeste.Scene.Entities;
using Celeste.Simulation;
using Celeste.Simulation.Runtime.Session;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.Randomizer.Gameplay
{
    public sealed partial class RandomizerGameplay
    {
        private void LoadStoryCampfire()
        {
            hooks.Add(new ILHook(typeof(CS06CampfireState).GetMethod(nameof(CS06CampfireState.OnBegin)),
                PatchCampfireFlag));
            hooks.Add(new ILHook(typeof(CS06CampfireState).GetMethod(nameof(CS06CampfireState.OnEnd)),
                PatchCampfirePosition));
            hooks.Add(new Hook(typeof(CS06CampfireState).GetMethod(nameof(CS06CampfireState.OnBegin)),
                (Action<Action<CS06CampfireState, OriginalLevelSceneRoot>, CS06CampfireState,
                    OriginalLevelSceneRoot>)BeginCampfire));
        }

        private void BeginCampfire(Action<CS06CampfireState, OriginalLevelSceneRoot> orig,
            CS06CampfireState cutscene, OriginalLevelSceneRoot level)
        {
            var player =
                level.Scheduler.Entities.GetTrackedEntity<RoomPlayerSession>(typeof(RoomPlayerSession).FullName);
            var savedX = player.X;
            orig(cutscene, level);
            if (!Owns(level) || Settings(level.Session) == null) return;
            player.X = savedX;
            player.Machine.Locked = false;
            player.Machine.State = 0;
        }

        private void PatchCampfireFlag(ILContext il)
        {
            var cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(MoveType.After,
                    instruction =>
                        instruction.MatchCallvirt<ChapterCompletionSession>(nameof(ChapterCompletionSession.GetFlag))))
                throw new InvalidOperationException("Randomizer cannot find the real campfire flag check.");
            EmitStoryOwner(cursor);
            cursor.EmitDelegate<Func<bool, object, bool>>((flag, owner) => IsRandomizerStory(owner) || flag);
        }

        private void PatchCampfirePosition(ILContext il)
        {
            var cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(MoveType.Before,
                    instruction => instruction.MatchLdarg(0),
                    instruction => instruction.MatchLdfld<CS06CampfireState>("player"),
                    instruction => instruction.MatchLdfld<RoomPlayerSession>(nameof(RoomPlayerSession.Sprite))))
                throw new InvalidOperationException("Randomizer cannot find the real campfire sleep placement.");
            var end = cursor.DefineLabel();
            cursor.MoveAfterLabels();
            EmitStoryOwner(cursor);
            cursor.EmitDelegate<Func<object, bool>>(IsRandomizerStory);
            cursor.Emit(OpCodes.Brtrue, end);
            if (!cursor.TryGotoNext(MoveType.Before,
                    instruction => instruction.MatchLdarg(0),
                    instruction => instruction.MatchCall<SceneEntityState>(nameof(SceneEntityState.RemoveSelf))))
                throw new InvalidOperationException("Randomizer cannot find the real campfire removal.");
            cursor.MarkLabel(end);
        }
    }
}
