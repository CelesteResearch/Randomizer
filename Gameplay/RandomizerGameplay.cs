using Celeste.Mods.Randomizer.Generation;
using Celeste.Mods.Randomizer.Generation.Configuration.Settings;
using Celeste.Mods.Randomizer.Generation.Configuration.Maps;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Celeste.Game.Runtime;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Gameplay.Doors.TempleGates;
using Celeste.Game.Gameplay.Holdables.TheoCrystal;
using Celeste.Simulation;
using Celeste.Simulation.Runtime.Session;
using Celeste.Simulation.Gameplay.Player.Inventory;
using Celeste.Simulation.Gameplay.Player.Intro;
using Celeste.Simulation.Gameplay.Environment.Core;
using Celeste.Simulation.Presentation.Player.Death;
using Celeste.Scene.Tracking;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.Randomizer.Gameplay
{
    public sealed partial class RandomizerGameplay : IDisposable
    {
        private readonly GameApplication engine;
        private readonly GenerationContext context;
        private readonly List<IDisposable> hooks = new List<IDisposable>();
        private bool loaded;
        private readonly Celeste.Mods.Randomizer.Interop.VariantResetImports variantResets;

        public RandomizerGameplay(GameApplication engine, GenerationContext context,
            Celeste.Mods.Randomizer.Interop.VariantResetImports variantResets)
        {
            this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
            this.context = context ?? throw new ArgumentNullException(nameof(context));
            this.variantResets = variantResets ?? throw new ArgumentNullException(nameof(variantResets));
        }

        public RandomizerSessionState SessionState(ChapterCompletionSession session)
        {
            var attached = context.State(session);
            var state = attached.Get<RandomizerSessionState>("RandomizerSession");
            if (state == null) attached.Set("RandomizerSession", state = new RandomizerSessionState());
            return state;
        }

        public RandoSettings Settings(ChapterCompletionSession session)
            => ResolveSettings(session);

        private bool Owns(OriginalLevelSceneRoot level) => ReferenceEquals(level.EngineContext, engine);

        public void Load()
        {
            if (loaded) throw new InvalidOperationException("Randomizer gameplay is already loaded.");
            try
            {
                hooks.Add(new Hook(typeof(OriginalLevelSceneRoot).GetMethod(nameof(OriginalLevelSceneRoot.LoadLevel)),
                    (Action<Action<OriginalLevelSceneRoot, PlayerIntroType, bool>, OriginalLevelSceneRoot,
                        PlayerIntroType, bool>)LoadLevel));
                hooks.Add(new Hook(
                    typeof(OriginalLevelSceneRoot).GetMethod(nameof(OriginalLevelSceneRoot.TransitionTo)),
                    (Action<Action<OriginalLevelSceneRoot, OriginalLevelRoomData, Vector2>, OriginalLevelSceneRoot,
                        OriginalLevelRoomData, Vector2>)Transition));
                hooks.Add(new Hook(typeof(OriginalLevelSceneRoot).GetMethod(nameof(OriginalLevelSceneRoot.TeleportTo)),
                    (Action<Action<OriginalLevelSceneRoot, RoomPlayerSession, string, PlayerIntroType, Vector2?>,
                        OriginalLevelSceneRoot, RoomPlayerSession, string, PlayerIntroType, Vector2?>)Teleport));
                hooks.Add(new Hook(typeof(RoomPlayerSession).GetMethod(nameof(RoomPlayerSession.UseRefill)),
                    (Func<Func<RoomPlayerSession, bool, bool>, RoomPlayerSession, bool, bool>)UseRefill));
                hooks.Add(new Hook(typeof(RoomPlayerSession).GetMethod(nameof(RoomPlayerSession.Die)),
                    (Func<Func<RoomPlayerSession, Vector2, bool, bool, PlayerDeathPresentation>, RoomPlayerSession,
                        Vector2, bool, bool, PlayerDeathPresentation>)Die));
                hooks.Add(new Hook(
                    typeof(OriginalTempleGateState).GetMethod(nameof(OriginalTempleGateState.TheoIsNearby)),
                    (Func<Func<OriginalTempleGateState, bool>, OriginalTempleGateState, bool>)TheoNearby));
                hooks.Add(new ILHook(
                    typeof(OriginalLevelSceneRoot).GetMethod(nameof(OriginalLevelSceneRoot.EnforceBounds)),
                    PatchBounds));
                LoadQualityOfLife();
                LoadContentHooks();
                LoadStoryHooks();
                engine.LevelUpdateEvents.OnLoadEntity += LoadEntity;
                loaded = true;
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            if (loaded) engine.LevelUpdateEvents.OnLoadEntity -= LoadEntity;
            loaded = false;
            for (int i = hooks.Count - 1; i >= 0; i--) hooks[i].Dispose();
            hooks.Clear();
        }

        private void LoadLevel(Action<OriginalLevelSceneRoot, PlayerIntroType, bool> orig,
            OriginalLevelSceneRoot level, PlayerIntroType intro, bool fromLoader)
        {
            var settings = Owns(level) ? Settings(level.Session) : null;
            if (settings != null && fromLoader && SessionState(level.Session).SeedCleanRandom)
                level.Session.FirstLevel = false;
            orig(level, intro, fromLoader);
            if (settings == null) return;
            if (!level.Transitioning && (intro != PlayerIntroType.Respawn || level.Session.Time == 0))
            {
                var mode = context.State(level.Session.LevelData).Get<RandoConfigCoreMode>("coreModes");
                level.CoreMode = level.Session.CoreMode = mode?.All ?? RoomCoreMode.None;
            }

            if (intro == PlayerIntroType.Transition)
                level.NextColorGrade(
                    context.GetArea(new Celeste.Game.Execution.Areas.OriginalAreaKey { ID = level.Session.AreaID })
                        .ColorGrade, 2f);
            engine.SaveContext.CurrentSave.SummitGems = new bool[6];
            if (settings.Length == MapLength.Short)
                for (int i = 0; i < 3; i++)
                    engine.SaveContext.CurrentSave.SummitGems[i] = true;
            var state = SessionState(level.Session);
            if (fromLoader && settings.HasLives)
            {
                if (settings.EndlessLevel == 0) state.CarryingLifeBerries = settings.EndlessLives;
                else if (state.CarryingLifeBerries < settings.EndlessLives) state.CarryingLifeBerries++;
            }

            if (intro != PlayerIntroType.Transition &&
                level.Scheduler.Entities.GetTrackedEntities(typeof(RoomPlayerSession).FullName) is
                    { Count: > 0 } players)
            {
                var player = (RoomPlayerSession)players[0];
                Vector2 position = player.Position;
                for (int i = 0; i < state.CarryingLifeBerries; i++)
                {
                    position += new Vector2(-12 * player.Facing, -8f);
                    level.Scheduler.QueueSourceEntity(new Entities.LifeBerry(engine, state, player, position,
                        new Celeste.Core.Maps.MapEntityId("CARRYING", i)));
                }
            }
        }

        private void Transition(Action<OriginalLevelSceneRoot, OriginalLevelRoomData, Vector2> orig,
            OriginalLevelSceneRoot level, OriginalLevelRoomData next, Vector2 direction)
        {
            if (Owns(level) && Settings(level.Session) is { } settings)
            {
                var modes = context.State(next).Get<RandoConfigCoreMode>("coreModes");
                RoomCoreMode mode = modes == null ? RoomCoreMode.None :
                    direction.X > 0 ? modes.Left :
                    direction.X < 0 ? modes.Right :
                    direction.Y < 0 ? modes.Down : modes.Up;
                level.CoreMode = level.Session.CoreMode = mode;
                level.Session.Flags.RemoveWhere(flag =>
                    flag.StartsWith("summit_checkpoint_", StringComparison.Ordinal) || flag == "MissTheBird");
                level.CameraUpwardMaxY = level.Camera.Y + 1000f;
                variantResets.Reset(context, (Celeste.Game.Content.Maps.OriginalChapterMapData)level.Session.MapData);
                level.Session.Inventory = new RoomPlayerInventory((int)settings.Dashes, true, false, false);
                level.CameraOffset = new Vector2(48f, 32f) * next.CameraOffset;
                foreach (var entity in level.Scheduler.Entities.GetTrackedEntities(typeof(RoomPlayerSession).FullName))
                {
                    CleanCameraTriggers(level, (RoomPlayerSession)entity);
                    ((RoomPlayerSession)entity).CameraAnchorLerp = Vector2.Zero;
                }
            }

            orig(level, next, direction);
        }

        public string WarpTarget(ChapterCompletionSession session, string oldTarget)
        {
            var mapping = context.State(session.LevelData).Get<Dictionary<string, string>>("WarpMapping");
            if (mapping == null || !mapping.TryGetValue(oldTarget, out var target))
                throw new InvalidOperationException("Randomizer has no warp mapping for " + oldTarget);
            return target;
        }

        private void Teleport(Action<OriginalLevelSceneRoot, RoomPlayerSession, string, PlayerIntroType, Vector2?> orig,
            OriginalLevelSceneRoot level, RoomPlayerSession player, string next, PlayerIntroType intro, Vector2? spawn)
        {
            if (Owns(level) && Settings(level.Session) != null)
            {
                next = context.State(level.Session.LevelData).Get<string>("CustomWarp")
                       ?? throw new InvalidOperationException(
                           "Randomizer has no custom warp for " + level.Session.Level);
                var target = (OriginalLevelRoomData)level.Session.MapData.GetLevel(next);
                spawn = target.Spawns[0] - target.Position;
            }

            orig(level, player, next, intro, spawn);
        }

        private bool UseRefill(Func<RoomPlayerSession, bool, bool> orig, RoomPlayerSession player, bool twoDashes)
        {
            if (player.Level is OriginalLevelSceneRoot level && Owns(level) &&
                Settings(level.Session)?.Dashes == NumDashes.Zero)
                twoDashes = false;
            return orig(player, twoDashes);
        }

        private PlayerDeathPresentation Die(Func<RoomPlayerSession, Vector2, bool, bool, PlayerDeathPresentation> orig,
            RoomPlayerSession player, Vector2 direction, bool evenIfInvincible, bool registerDeath)
        {
            var corpse = orig(player, direction, evenIfInvincible, registerDeath);
            if (corpse == null || !(player.Level is OriginalLevelSceneRoot level) || !Owns(level) ||
                Settings(level.Session)?.HasLives != true)
                return corpse;
            var state = SessionState(level.Session);
            if (state.SavedByTheBell) state.SavedByTheBell = false;
            else
                corpse.DeathAction = () => engine.Frame.Scenes.NextScene =
                    ((Celeste.Game.Chapters.Flow.IOriginalLevelExitEnvironment)engine).LevelExits.CreateLevelExit(
                        Celeste.Game.Chapters.Flow.LevelExitMode.GoldenBerryRestart, level.Session);
            return corpse;
        }

        private static readonly FieldInfo HoldingCheck =
            typeof(OriginalTempleGateState).GetField("holdingCheckFrom",
                BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo GateOpen =
            typeof(OriginalTempleGateState).GetField("open", BindingFlags.Instance | BindingFlags.NonPublic);

        private bool TheoNearby(Func<OriginalTempleGateState, bool> orig, OriginalTempleGateState gate)
        {
            if (!(gate.Scene is OriginalLevelSceneRoot level) || !Owns(level) || Settings(level.Session) == null)
                return orig(gate);
            var theos = level.Scheduler.Entities.GetTrackedEntities("Celeste.TheoCrystal");
            if (theos.Count == 0) return true;
            var theo = (TheoCrystalState)theos[0];
            return (theo.X > gate.X ? theo.X - gate.X <= 25f : gate.X - theo.X <= 10f)
                   || Vector2.DistanceSquared((Vector2)HoldingCheck.GetValue(gate), theo.Center) <
                   ((bool)GateOpen.GetValue(gate) ? 6400f : 4096f);
        }

        public static int ComputeScore(RandoSettings settings, long ticks, int carrying)
        {
            float time = (float)TimeSpan.FromTicks(ticks).TotalSeconds;
            float levelBonus = settings.SeedType == SeedType.Custom ? 150f : 450f;
            float berryBonus = settings.SeedType == SeedType.Custom ? 40f : 60f;
            float decay = settings.SeedType == SeedType.Custom ? .1f / (60f * 10f) : .1f / (60f * 15f);
            return (int)((settings.EndlessLevel + 1) * levelBonus + carrying * berryBonus - time -
                         decay / 2f * time * time);
        }
    }
}
