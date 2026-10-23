using Celeste.Mods.Randomizer.Generation.Configuration.Settings;
using System;
using System.Collections;
using System.Numerics;
using System.Reflection;
using Celeste.Formats.Map.Entities.Attributes;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Gameplay.Collectibles.Cassettes;
using Celeste.Game.Gameplay.Collectibles.Keys;
using Celeste.Game.Gameplay.Collectibles.Strawberries;
using Celeste.Game.Gameplay.Environment.Lava.Rising;
using Celeste.Game.Gameplay.Clutter.Switches;
using Celeste.Game.Entities.Doors.LockBlocks;
using Celeste.Game.Entities.Collectibles.Hearts;
using Celeste.Game.Entities.Collectibles.SummitGems;
using Celeste.Scene.Entities;
using Celeste.Simulation;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Mono.Cecil.Cil;

namespace Celeste.Mods.Randomizer.Gameplay
{
    public sealed partial class RandomizerGameplay
    {
        private const BindingFlags AllInstanceMethods =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private void LoadQualityOfLife()
        {
            hooks.Add(new Hook(typeof(OriginalCassetteState).GetMethod("CollectRoutine", AllInstanceMethods),
                (Func<Func<OriginalCassetteState, RoomPlayerSession, IEnumerator>, OriginalCassetteState,
                    RoomPlayerSession, IEnumerator>)CollectCassette));
            hooks.Add(new Hook(typeof(OriginalLockBlockState).GetMethod("OnPlayer", AllInstanceMethods),
                (Action<Action<OriginalLockBlockState, RoomPlayerSession>, OriginalLockBlockState, RoomPlayerSession>)
                OpenLock));
            hooks.Add(new Hook(typeof(OriginalRisingLavaState).GetMethod(nameof(OriginalRisingLavaState.Update)),
                (Action<Action<OriginalRisingLavaState>, OriginalRisingLavaState>)UpdateLava));
            hooks.Add(new Hook(typeof(OriginalClutterSwitchState).GetMethod(nameof(OriginalClutterSwitchState.Update)),
                (Action<Action<OriginalClutterSwitchState>, OriginalClutterSwitchState>)UpdateClutter));
            hooks.Add(new Hook(typeof(RoomPlayerSession).GetMethod(nameof(RoomPlayerSession.Added)),
                (Action<Action<RoomPlayerSession, object>, RoomPlayerSession, object>)AddedPlayer));
            hooks.Add(new Hook(typeof(RoomPlayerSession).GetMethod(nameof(RoomPlayerSession.Update)),
                (Action<Action<RoomPlayerSession>, RoomPlayerSession>)UpdatePlayer));
            hooks.Add(new Hook(typeof(OriginalKeyState).GetMethod("OnPlayer", AllInstanceMethods),
                (Action<Action<OriginalKeyState, RoomPlayerSession>, OriginalKeyState, RoomPlayerSession>)CollectKey));
            hooks.Add(new Hook(typeof(OriginalStrawberryState).GetMethod(nameof(OriginalStrawberryState.OnPlayer)),
                (Action<Action<OriginalStrawberryState, RoomPlayerSession>, OriginalStrawberryState, RoomPlayerSession>)
                CollectBerry));
            hooks.Add(new Hook(typeof(OriginalSummitGemState).GetMethod("SmashRoutine", AllInstanceMethods),
                (Func<Func<OriginalSummitGemState, RoomPlayerSession, OriginalLevelSceneRoot, IEnumerator>,
                    OriginalSummitGemState, RoomPlayerSession, OriginalLevelSceneRoot, IEnumerator>)SmashGem));
            hooks.Add(new ILHook(typeof(OriginalHeartGemState).GetMethod(nameof(OriginalHeartGemState.OnPlayer)),
                PatchDashlessAccess));
            hooks.Add(new ILHook(typeof(OriginalSummitGemState).GetMethod("OnPlayer", AllInstanceMethods),
                PatchDashlessAccess));
            hooks.Add(new ILHook(typeof(OriginalSummitGemState).GetMethod("OnPlayer", AllInstanceMethods),
                PatchGemRefill));
        }

        private void AddedPlayer(Action<RoomPlayerSession, object> orig, RoomPlayerSession player, object scene)
        {
            orig(player, scene);
            if (scene is OriginalLevelSceneRoot level && Owns(level) && Settings(level.Session) != null)
                player.JustRespawned = true;
        }

        private void UpdatePlayer(Action<RoomPlayerSession> orig, RoomPlayerSession player)
        {
            if (player.Level is OriginalLevelSceneRoot level && Owns(level) && level.InCutscene)
            {
                Entities.FindTheoPhoneCutscene phone = null;
                int cutscenes = 0;
                foreach (var entity in level.Scheduler.Entities.Entities)
                {
                    if (entity is Entities.FindTheoPhoneCutscene found) phone ??= found;
                    if (entity is Celeste.Game.Execution.Levels.Cutscenes.OriginalCutsceneEntityState) cutscenes++;
                }

                if (phone != null && (phone.State == 1 && player.Machine.State == 0 || cutscenes > 1))
                    phone.OnEnd(level);
            }

            orig(player);
        }

        private IEnumerator CollectCassette(Func<OriginalCassetteState, RoomPlayerSession, IEnumerator> orig,
            OriginalCassetteState cassette, RoomPlayerSession player)
        {
            var routine = orig(cassette, player);
            try
            {
                while (routine.MoveNext()) yield return routine.Current;
            }
            finally
            {
                (routine as IDisposable)?.Dispose();
            }

            if (cassette.Scene is OriginalLevelSceneRoot level && Owns(level) && Settings(level.Session) != null)
                level.Session.Cassette = false;
        }

        private void OpenLock(Action<OriginalLockBlockState, RoomPlayerSession> orig, OriginalLockBlockState block,
            RoomPlayerSession player)
        {
            if (block.Scene is OriginalLevelSceneRoot level && Owns(level) && Settings(level.Session) != null)
            {
                int blocks = 0, keys = 0;
                foreach (var entity in level.Scheduler.Entities.Entities)
                {
                    if (entity is OriginalLockBlockState) blocks++;
                    if (entity is OriginalKeyState) keys++;
                }

                if (blocks > keys) return;
            }

            orig(block, player);
        }

        private static readonly FieldInfo LavaWaiting =
            typeof(OriginalRisingLavaState).GetField("waiting", AllInstanceMethods);

        private void UpdateLava(Action<OriginalRisingLavaState> orig, OriginalRisingLavaState lava)
        {
            if (lava.Scene is OriginalLevelSceneRoot level && Owns(level) && Settings(level.Session) != null)
            {
                var player =
                    level.Scheduler.Entities.GetTrackedEntity<RoomPlayerSession>(typeof(RoomPlayerSession).FullName);
                if (player != null && (player.Machine.State == 13 || player.Machine.State == 15 ||
                                       player.Machine.State == 23 || player.Machine.State == 25))
                    LavaWaiting.SetValue(lava, true);
            }

            orig(lava);
        }

        private static readonly MethodInfo ClutterDashed =
            typeof(OriginalClutterSwitchState).GetMethod("OnDashed", AllInstanceMethods);

        private void UpdateClutter(Action<OriginalClutterSwitchState> orig, OriginalClutterSwitchState clutter)
        {
            orig(clutter);
            if (clutter.Scene is OriginalLevelSceneRoot level && Owns(level) &&
                Settings(level.Session)?.Dashes == NumDashes.Zero && clutter.HasPlayerOnTop())
            {
                var player =
                    level.Scheduler.Entities.GetTrackedEntity<RoomPlayerSession>(typeof(RoomPlayerSession).FullName);
                ClutterDashed.Invoke(clutter, new object[] { player, Vector2.UnitY });
            }
        }

        private void AutoBubble(SceneEntityState entity, RoomPlayerSession player)
        {
            if (entity.Scene is OriginalLevelSceneRoot level && Owns(level) &&
                entity.SourceData is Celeste.Formats.Map.Entities.Structure.EntityData data &&
                EntityAttributeReader.Bool(data.Values, "AutoBubble"))
                player.OwnedComponents.Add(
                    new Celeste.Core.Scheduling.SimulationCoroutine(engine,
                        Entities.LifeBerry.AutoBubble(engine, player)));
        }

        private void CollectKey(Action<OriginalKeyState, RoomPlayerSession> orig, OriginalKeyState key,
            RoomPlayerSession player)
        {
            AutoBubble(key, player);
            orig(key, player);
        }

        private void CollectBerry(Action<OriginalStrawberryState, RoomPlayerSession> orig,
            OriginalStrawberryState berry, RoomPlayerSession player)
        {
            AutoBubble(berry, player);
            orig(berry, player);
        }

        private IEnumerator SmashGem(
            Func<OriginalSummitGemState, RoomPlayerSession, OriginalLevelSceneRoot, IEnumerator> orig,
            OriginalSummitGemState gem, RoomPlayerSession player, OriginalLevelSceneRoot level)
        {
            AutoBubble(gem, player);
            return orig(gem, player, level);
        }

        private void PatchDashlessAccess(ILContext il)
        {
            var cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(MoveType.After,
                    instruction => instruction.MatchCallvirt<RoomPlayerSession>("get_DashAttacking")))
                throw new InvalidOperationException("Randomizer cannot find the real dash attack test in " +
                                                    il.Method.FullName);
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.EmitDelegate<Func<bool, RoomPlayerSession, bool>>((attacking, player) => attacking ||
                player.Level is OriginalLevelSceneRoot level && Owns(level) &&
                Settings(level.Session)?.Dashes == NumDashes.Zero);
        }

        private void PatchGemRefill(ILContext il)
        {
            var cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(MoveType.After,
                    instruction => instruction.MatchCallvirt<Celeste.Core.Components.SourceOwnedComponentList>("Add")))
                throw new InvalidOperationException("Randomizer cannot find the real gem coroutine registration.");
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.EmitDelegate<Action<RoomPlayerSession>>(player =>
            {
                if (player.Level is OriginalLevelSceneRoot level && Owns(level) && Settings(level.Session) != null)
                    player.RefillDash();
            });
        }
    }
}
