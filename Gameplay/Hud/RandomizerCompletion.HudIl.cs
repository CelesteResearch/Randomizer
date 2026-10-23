using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Celeste.Core.Display;
using Celeste.Core.Components;
using Celeste.Game.Chapters.Flow;
using Celeste.Game.Content.Dialogs;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Presentation.Hud.Counters;
using Celeste.Game.Presentation.Hud.Speedrun;
using Celeste.Graphics.Display;
using Celeste.Graphics.Display.Colors;
using Celeste.Scene.Entities;
using Celeste.Mods.Randomizer.Generation.Configuration.Settings;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.Randomizer.Gameplay
{
    public sealed partial class RandomizerCompletion
    {
        private void LoadCompletionHudIl(List<IDisposable> hooks)
        {
            var glyphSink = typeof(SpeedrunTimerState).GetNestedType("SourceGlyphSink", BindingFlags.NonPublic);
            var drawTime = typeof(SpeedrunTimerState).GetMethods()
                .Single(method => method.Name == "DrawTime" && method.IsGenericMethodDefinition);
            hooks.Add(new ILHook(drawTime.MakeGenericMethod(glyphSink), SetPlatinumColor));
            hooks.Add(new ILHook(typeof(AreaCompleteSequence).GetConstructors().Single(), SetEndlessTitle));
        }

        private void SetPlatinumColor(ILContext il)
        {
            var colors = il.Body.Variables
                .Where(variable => variable.VariableType.FullName == typeof(PackedColor).FullName).Take(2).ToArray();
            if (colors.Length != 2) throw new InvalidOperationException("Randomizer: timer color locals changed.");
            var cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(MoveType.Before, instruction => instruction.MatchLdarg(5)))
                throw new InvalidOperationException("Randomizer: timer validity branch missing.");
            int start = cursor.Index;
            if (!cursor.TryGotoNext(MoveType.Before, instruction => instruction.MatchLdcI4(0)))
                throw new InvalidOperationException("Randomizer: timer glyph loop missing.");
            var afterColors = cursor.MarkLabel();
            cursor.Index = start;
            cursor.EmitDelegate<Func<bool>>(() => engine.Frame.Scenes.CurrentScene is OriginalLevelSceneRoot level &&
                                                  gameplay.Settings(level.Session) != null &&
                                                  gameplay.SessionState(level.Session).BeatBestTimePlatinum);
            var normal = cursor.DefineLabel();
            cursor.Emit(OpCodes.Brfalse, normal);
            EmitColor("cb19d2", colors[0]);
            EmitColor("994f9c", colors[1]);
            cursor.Emit(OpCodes.Br, afterColors);
            cursor.MarkLabel(normal);

            void EmitColor(string hex, VariableDefinition destination)
            {
                cursor.Emit(OpCodes.Ldstr, hex);
                cursor.Emit(OpCodes.Call,
                    typeof(SourceHexColor).GetMethod(nameof(SourceHexColor.HexToColor), new[] { typeof(string) }));
                cursor.Emit(OpCodes.Ldarg, 8);
                cursor.Emit(OpCodes.Call,
                    typeof(PackedColor).GetMethod("op_Multiply", new[] { typeof(PackedColor), typeof(float) }));
                cursor.Emit(OpCodes.Stloc, destination);
            }
        }

        private void SetEndlessTitle(ILContext il)
        {
            var cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(MoveType.After,
                    instruction =>
                        instruction.MatchCallOrCallvirt<OriginalGameTextProcess>(
                            nameof(OriginalGameTextProcess.CleanDialog))))
                throw new InvalidOperationException("Randomizer: completion title dialog call missing.");
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<string, AreaCompleteSequence, string>>((text, self) =>
                ReferenceEquals(self.EngineContext, engine) && endingSettings?.Algorithm == LogicType.Endless
                    ? string.Format(HudText.Dialog.Get("RANDOENDLESS_HEADER"), endingSettings.EndlessLevel + 1)
                    : text);
            // The shared scene queues entities instead of Monocle.Scene.Add; attach at constructor return,
            // after the complete renderer and snow have been installed.
            while (cursor.TryGotoNext(MoveType.Before, instruction => instruction.OpCode == OpCodes.Ret))
            {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Action<AreaCompleteSequence>>(self =>
                {
                    if (!ReferenceEquals(self.EngineContext, engine) || endingSettings?.HasLives != true) return;
                    var counter = new StrawberriesCounterState(engine, false,
                            gameplay.SessionState(self.Session).CarryingLifeBerries)
                        { Position = new System.Numerics.Vector2(70f, 100f) };
                    var entity =
                        new SceneEntityState(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration);
                    entity.OwnedComponents.Add(counter);
                    self.SourceScene.QueueSourceEntity(entity);
                });
                cursor.Index++;
            }
        }
    }
}
