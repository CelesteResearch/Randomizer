using Celeste.Mods.Randomizer.Generation.Topology;
using System;
using Celeste.Game.Execution.Levels;
using Celeste.Simulation;
using MonoMod.Cil;
using Mono.Cecil.Cil;

namespace Celeste.Mods.Randomizer.Gameplay
{
    public sealed partial class RandomizerGameplay
    {
        private void PatchBounds(ILContext il)
        {
            var cursor = new ILCursor(il);
            int transitionChecks = 0;
            while (cursor.TryGotoNext(MoveType.After,
                       instruction => instruction.MatchCallvirt("Celeste.Game.Content.Maps.OriginalChapterMapData",
                           "CanTransitionTo")))
            {
                transitionChecks++;
                if (transitionChecks != 3) continue;
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldarg_1);
                cursor.EmitDelegate<Func<bool, OriginalLevelSceneRoot, RoomPlayerSession, bool>>((allowed, level,
                        player) =>
                    Owns(level) && Settings(level.Session) != null
                        ? allowed && UsedVerticalHole(level, player, ScreenDirection.Up)
                        : allowed);
            }

            if (transitionChecks != 4)
                throw new InvalidOperationException(
                    "Randomizer bounds hook requires exactly four real transition checks.");
            cursor.Index = 0;
            if (!cursor.TryGotoNext(MoveType.After,
                    instruction => instruction.MatchLdfld("Celeste.Game.Content.Maps.Levels.OriginalLevelRoomData",
                        "DisableDownTransition")))
                throw new InvalidOperationException("Randomizer cannot find the real down-transition field.");
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.EmitDelegate<Func<bool, OriginalLevelSceneRoot, RoomPlayerSession, bool>>((disabled, level,
                    player) =>
                Owns(level) && Settings(level.Session) != null
                    ? !UsedVerticalHole(level, player, ScreenDirection.Down)
                    : disabled);
            cursor.Index = 0;
            if (!cursor.TryGotoNext(MoveType.After,
                    instruction =>
                        instruction.MatchIsinst<Celeste.Game.Gameplay.Holdables.TheoCrystal.TheoCrystalState>()))
                throw new InvalidOperationException("Randomizer cannot find the real Theo transition blocker.");
            cursor.Emit(OpCodes.Ldarg_0);
            cursor
                .EmitDelegate<
                    Func<Celeste.Game.Gameplay.Holdables.TheoCrystal.TheoCrystalState, OriginalLevelSceneRoot,
                        Celeste.Game.Gameplay.Holdables.TheoCrystal.TheoCrystalState>>((theo, level) =>
                    Owns(level) && Settings(level.Session) != null ? null : theo);
        }

        private bool UsedVerticalHole(OriginalLevelSceneRoot level, RoomPlayerSession player, ScreenDirection side)
        {
            var room = (Celeste.Game.Content.Maps.Levels.OriginalLevelRoomData)level.Session.LevelData;
            var holes = context.State(room).Get<System.Collections.Generic.List<Hole>>("UsedVerticalHoles");
            if (holes == null)
                throw new InvalidOperationException("Randomizer generated room has no vertical-hole metadata.");
            foreach (var hole in holes)
                if (hole.Side == side && player.Center.X > room.Bounds.Left + hole.LowBound * 8
                                      && player.Center.X < room.Bounds.Left + hole.HighBound * 8 + 8)
                    return true;
            return false;
        }
    }
}
