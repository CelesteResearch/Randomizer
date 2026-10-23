using System;
using System.Reflection;
using Celeste.Game.Chapters.Flow;
using MonoMod.Cil;
using Mono.Cecil.Cil;

namespace Celeste.Mods.Randomizer.Gameplay
{
    public sealed partial class RandomizerCompletion
    {
        private static readonly FieldInfo CanConfirm =
            typeof(AreaCompleteSequence).GetField("canConfirm", BindingFlags.Instance | BindingFlags.NonPublic);

        private void MarkSessionUnclean(ILContext il)
        {
            var cursor = new ILCursor(il);
            cursor.EmitDelegate<Action>(() =>
            {
                if (engine.Frame.Scenes.CurrentScene is Celeste.Game.Execution.Levels.OriginalLevelSceneRoot level)
                    gameplay.SessionState(level.Session).SeedCleanRandom = false;
            });
        }

        private void PatchEndlessConfirm(ILContext il)
        {
            var cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(MoveType.Before, instruction => instruction.MatchLdarg(0),
                    instruction => instruction.MatchLdfld<AreaCompleteSequence>("snow")))
                throw new InvalidOperationException("Randomizer cannot find completion snow update.");
            var continueUpdate = cursor.MarkLabel();
            cursor.Index = 0;
            if (!cursor.TryGotoNext(MoveType.After,
                    instruction => instruction.MatchStfld<AreaCompleteSequence>("canConfirm")))
                throw new InvalidOperationException("Randomizer cannot find completion confirmation.");
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<AreaCompleteSequence, bool>>(ConfirmEndless);
            cursor.Emit(OpCodes.Brtrue, continueUpdate);
        }
    }
}
