using System;
using System.Numerics;
using Celeste.Game.Entities.StarJump.Cutscenes;
using Celeste.Game.Entities.Triggers.Events;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Gameplay.Collectibles.Strawberries;
using Celeste.Game.Stories.OldSite.Phone;
using Celeste.Game.Stories.Temple;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Intro;
using Celeste.Simulation.Runtime.Session;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.Randomizer.Gameplay
{
    public sealed partial class RandomizerGameplay
    {
        private static bool IsStoryWarp(Instruction instruction)
            => instruction.MatchStfld<ChapterCompletionSession>(nameof(ChapterCompletionSession.Level));

        private void LoadStoryWarps()
        {
            AddStoryProducerHooks(typeof(CS02DreamingPhonecallState), IsStoryWarp, il => PatchStoryWarp(il, false));
            AddStoryProducerHooks(typeof(MirrorPortalCutscene), IsStoryWarp, il => PatchStoryWarp(il, false));
            AddStoryProducerHooks(typeof(MirrorPortalCutscene),
                instruction => instruction.MatchLdfld<ChapterCompletionSession>(nameof(ChapterCompletionSession.Mode)),
                PatchMirrorMode);
            AddStoryProducerHooks(typeof(CS06StarJumpEndState), IsStoryWarp, PatchStarJumpWarp);
            AddStoryProducerHooks(typeof(OriginalEventTriggerState), IsStoryWarp, il => PatchStoryWarp(il, true));
            hooks.Add(new Hook(typeof(CS06StarJumpEndState).GetMethod(nameof(CS06StarJumpEndState.OnBegin)),
                (Action<Action<CS06StarJumpEndState, OriginalLevelSceneRoot>, CS06StarJumpEndState,
                    OriginalLevelSceneRoot>)BeginStarJump));
            AddStoryProducerHooks(typeof(OriginalEventTriggerState), IsStoryWarp, PatchEventFollowers);
        }

        private void PatchStoryWarp(ILContext il, bool specific)
        {
            var cursor = new ILCursor(il);
            var count = 0;
            while (cursor.TryGotoNext(MoveType.Before, IsStoryWarp))
            {
                cursor.MoveAfterLabels();
                cursor.Next.OpCode = OpCodes.Nop;
                cursor.Next.Operand = null;
                cursor.EmitDelegate<Action<ChapterCompletionSession, string>>((session, oldTarget) =>
                {
                    var level = ResolveStorySessionLevel(session);
                    if (level == null || Settings(session) == null) session.Level = oldTarget;
                    else
                        session.Level = specific
                            ? WarpTarget(session, oldTarget)
                            : context.State(session.LevelData).Get<string>("CustomWarp") ??
                              throw new InvalidOperationException("Randomizer has no custom warp for " + session.Level);
                });
                count++;
            }

            if (count == 0) throw new InvalidOperationException("Randomizer story warp producer disappeared.");
            if (specific) return; // Original EventTrigger keeps its relative player/camera positioning.
            cursor.Index = 0;
            while (cursor.TryGotoNext(MoveType.Before,
                       instruction =>
                           instruction.MatchStfld<ChapterCompletionSession>(
                               nameof(ChapterCompletionSession.RespawnPoint))))
            {
                cursor.MoveAfterLabels();
                cursor.Next.OpCode = OpCodes.Nop;
                cursor.Next.Operand = null;
                cursor.EmitDelegate<Action<ChapterCompletionSession, Vector2?>>((session, oldSpawn) =>
                {
                    var level = ResolveStorySessionLevel(session);
                    session.RespawnPoint = level != null && Settings(session) != null
                        ? session.LevelData.Spawns[0]
                        : oldSpawn;
                });
            }
        }

        private void PatchMirrorMode(ILContext il)
        {
            var cursor = new ILCursor(il);
            while (cursor.TryGotoNext(MoveType.Before,
                       instruction =>
                           instruction.MatchLdfld<ChapterCompletionSession>(nameof(ChapterCompletionSession.Mode))))
            {
                cursor.MoveAfterLabels();
                cursor.Next.OpCode = OpCodes.Nop;
                cursor.Next.Operand = null;
                cursor.EmitDelegate<Func<ChapterCompletionSession, int>>(session =>
                    ResolveStorySessionLevel(session) != null && Settings(session) != null ? 1 : session.Mode);
            }
        }

        private void BeginStarJump(Action<CS06StarJumpEndState, OriginalLevelSceneRoot> orig,
            CS06StarJumpEndState cutscene, OriginalLevelSceneRoot level)
        {
            // Upstream installs the strawberry transfer for this cutscene regardless of the current area.
            if (Owns(level))
                OriginalStrawberryTransfer.StoreStrawberries(level.Scheduler.Entities
                    .GetTrackedEntity<RoomPlayerSession>(typeof(RoomPlayerSession).FullName).Leader);
            orig(cutscene, level);
        }

        private void PatchStarJumpWarp(ILContext il)
        {
            PatchStoryWarp(il, false);
            var cursor = new ILCursor(il);
            while (cursor.TryGotoNext(MoveType.After,
                       instruction => instruction.MatchLdcI4((int)PlayerIntroType.Fall)))
            {
                EmitStoryOwner(cursor);
                cursor.EmitDelegate<Func<int, object, int>>((intro, owner) =>
                    IsRandomizerStory(owner) ? (int)PlayerIntroType.None : intro);
            }

            cursor.Index = 0;
            if (!cursor.TryGotoNext(MoveType.After,
                    instruction =>
                        instruction.MatchCallvirt<OriginalLevelSceneRoot>(nameof(OriginalLevelSceneRoot.LoadLevel))))
                throw new InvalidOperationException("Randomizer cannot find the real star-jump reload.");
            EmitStoryOwner(cursor);
            cursor.EmitDelegate<Action<object>>(owner =>
            {
                var level = ResolveStoryLevel(owner);
                if (level != null && Owns(level))
                    OriginalStrawberryTransfer.RestoreStrawberries(level.Scheduler.Entities
                        .GetTrackedEntity<RoomPlayerSession>(typeof(RoomPlayerSession).FullName).Leader);
            });
        }

        private void PatchEventFollowers(ILContext il)
        {
            var cursor = new ILCursor(il);
            var transfer = new Mono.Cecil.Cil.VariableDefinition(il.Import(typeof(StoryFollowerTransfer)));
            il.Body.Variables.Add(transfer);
            EmitStoryOwner(cursor);
            cursor.EmitDelegate<Func<object, StoryFollowerTransfer>>(owner =>
            {
                var level = ResolveStoryLevel(owner);
                return level != null && Owns(level) ? new StoryFollowerTransfer(level) : null;
            });
            cursor.Emit(OpCodes.Stloc, transfer);
            while (cursor.TryGotoNext(MoveType.Before, instruction => instruction.OpCode == OpCodes.Ret))
            {
                cursor.MoveAfterLabels();
                cursor.Emit(OpCodes.Ldloc, transfer);
                cursor.EmitDelegate<Action<StoryFollowerTransfer>>(state => state?.Restore());
                cursor.Index++;
            }
        }
    }
}
