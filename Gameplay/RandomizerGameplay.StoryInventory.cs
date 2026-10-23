using System;
using Celeste.Game.Entities.Npcs.Gravestone;
using Celeste.Game.Entities.Triggers.Events;
using Celeste.Game.Stories.Farewell.Help;
using Celeste.Game.Stories.Farewell.Moon;
using Celeste.Game.Stories.Summit.Ascension;
using Celeste.Simulation.Gameplay.Player.Inventory;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace Celeste.Mods.Randomizer.Gameplay
{
    public sealed partial class RandomizerGameplay
    {
        private static bool IsInventoryDashWrite(Instruction instruction)
            => instruction.MatchStfld<RoomPlayerInventory>(nameof(RoomPlayerInventory.Dashes));

        private void LoadStoryInventory()
        {
            AddStoryProducerHooks(typeof(OriginalEventTriggerState), IsInventoryDashWrite, PatchStoryInventory);
            AddStoryProducerHooks(typeof(MoonIntroSequence), IsInventoryDashWrite, PatchStoryInventory);
            AddStoryProducerHooks(typeof(OriginalBadelineHelpsCutscene), IsInventoryDashWrite, PatchStoryInventory);
            AddStoryProducerHooks(typeof(CS10GravestoneState), IsInventoryDashWrite, PatchStoryInventory);
            AddStoryProducerHooks(typeof(CS10GravestoneState), instruction => instruction.MatchLdcI4(2),
                PatchStoryTwoDashes);
            AddStoryProducerHooks(typeof(OriginalSummitAscendCutscene), instruction => instruction.MatchLdcI4(2),
                PatchStoryTwoDashes);
        }

        private int StoryDashes(int oldDashes, object owner)
        {
            var level = ResolveStoryLevel(owner);
            return level != null && Owns(level) && Settings(level.Session) != null
                ? level.Session.Inventory.Dashes
                : oldDashes;
        }

        private void PatchStoryInventory(ILContext il)
        {
            var cursor = new ILCursor(il);
            while (cursor.TryGotoNext(MoveType.Before, IsInventoryDashWrite))
            {
                cursor.MoveAfterLabels();
                EmitStoryOwner(cursor);
                cursor.EmitDelegate<Func<int, object, int>>(StoryDashes);
                cursor.Index++;
            }
        }

        private void PatchStoryTwoDashes(ILContext il)
        {
            var cursor = new ILCursor(il);
            while (cursor.TryGotoNext(MoveType.After, instruction => instruction.MatchLdcI4(2)))
            {
                cursor.MoveAfterLabels();
                EmitStoryOwner(cursor);
                cursor.EmitDelegate<Func<int, object, int>>(StoryDashes);
            }
        }
    }
}
