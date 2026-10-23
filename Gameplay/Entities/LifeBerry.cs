using System;
using System.Collections;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Components.Animation;
using Celeste.Core.Components.Timing;
using Celeste.Core.Display;
using Celeste.Core.Maps;
using Celeste.Core.Scheduling;
using Celeste.Game.Gameplay.Collectibles.Strawberries;
using Celeste.Game.Gameplay.Collectibles.Strawberries.Points;
using Celeste.Game.Execution.Levels;
using Celeste.Scene.Entities;
using Celeste.Scene.Following;
using Celeste.Scene.Components.Oscillation;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Collision.Components;
using Celeste.Simulation.Presentation.Effects.Mirrors;
using Celeste.Simulation.Presentation.Lighting;
using Celeste.Simulation.Presentation.PostProcessing.Bloom;
using Celeste.Physics.Collision.Shapes;
using Celeste.Graphics.Animation;

namespace Celeste.Mods.Randomizer.Gameplay.Entities
{
    public sealed class LifeBerry : SceneEntityState
    {
        private readonly IOriginalStrawberryProcess process;
        private readonly RandomizerSessionState state;
        private readonly bool autoBubble;
        public readonly MapEntityId ID;
        public readonly FollowerState Follower;
        private Sprite sprite;
        private WigglerComponent wiggler;
        private VertexLightState light;
        private BloomPointState bloom;
        private SourceTweenState lightTween;
        private float wobble;
        private bool firstStrawberry;

        public LifeBerry(IOriginalStrawberryProcess process, RandomizerSessionState state, Vector2 position,
            MapEntityId id, bool autoBubble)
            : base(((ISourceEntitySourceDataEnvironment)process).EntitySourceDataRegistration, position)
        {
            this.process = process;
            this.state = state;
            this.autoBubble = autoBubble;
            ID = id;
            Collider = new HitboxColliderGeometry(14f, 14f, -7f, -7f);
            OwnedComponents.Add(new PlayerColliderComponentState(OnPlayer));
            OwnedComponents.Add(new MirrorReflectionState());
            OwnedComponents.Add(Follower =
                new FollowerState(process, OriginalSceneTags.Persistent, id, onLoseLeader: OnLoseLeader));
        }

        public LifeBerry(IOriginalStrawberryProcess process, RandomizerSessionState state, RoomPlayerSession player,
            Vector2 position, MapEntityId id)
            : this(process, state, position, id, false)
        {
            player.Leader.GainFollower(Follower);
            firstStrawberry = IsFirst();
            Collidable = false;
            Depth = -1000000;
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            if (!Follower.HasLeader && state.TakenLifeBerries.Contains(ID))
            {
                RemoveSelf();
                return;
            }

            OwnedComponents.Add(sprite = process.GameGraphics.SpriteFactory.Create("strawberry"));
            sprite.OnFrameChange = OnAnimate;
            OwnedComponents.Add(wiggler = process.WigglerPool.Create(.4f, 4f,
                value => sprite.Scale = Vector2.One * (float)(1.0 + value * .3499999940395355)));
            OwnedComponents.Add(process.WigglerPool.Create(.5f, 4f,
                value => sprite.Rotation = (float)(value * 30.0 * (Math.PI / 180.0))));
            OwnedComponents.Add(bloom = new BloomPointState(1f, 12f));
            OwnedComponents.Add(light = new VertexLightState(process, Vector2.Zero, PackedColor.White, 1f, 16, 24));
            OwnedComponents.Add(lightTween = light.CreatePulseTween(process));
        }

        private void OnPlayer(RoomPlayerSession player)
        {
            if (Follower.HasLeader) return;
            process.AudioSession.Play("event:/game/general/strawberry_touch", Position, process.SaveContext);
            player.Leader.GainFollower(Follower);
            firstStrawberry = IsFirst();
            wiggler.Start();
            Depth = -1000000;
            state.CarryingLifeBerries++;
            state.TakenLifeBerries.Add(ID);
            if (autoBubble) player.OwnedComponents.Add(new SimulationCoroutine(process, AutoBubble(process, player)));
        }

        public static IEnumerator AutoBubble(ISourceEngineContext engine, RoomPlayerSession player)
        {
            yield return .3f;
            if (!player.Dead && player.Machine.State != 21)
            {
                var level = (OriginalLevelSceneRoot)player.Level;
                ((IOriginalStrawberryProcess)engine).AudioSession.Play("event:/game/general/cassette_bubblereturn",
                    level.Camera.Position + new Vector2(160f, 90f), ((IOriginalStrawberryProcess)engine).SaveContext);
                var respawn = level.Session.RespawnPoint.Value;
                player.StartCassetteFly(respawn, (respawn + player.Position) / 2 - 30 * Vector2.UnitY);
            }
        }

        public override void Update()
        {
            base.Update();
            if (!Follower.HasLeader)
            {
                wobble += process.DeltaTime * 4f;
                sprite.Y = bloom.Y = light.Y = (float)Math.Sin(wobble) * 2f;
            }
        }

        private bool IsFirst()
        {
            for (int i = Follower.FollowIndex - 1; i >= 0; i--)
                if (Follower.Leader.Followers[i].Entity is LifeBerry)
                    return false;
            return true;
        }

        private void OnLoseLeader()
        {
            if (!firstStrawberry) return;
            state.SavedByTheBell = true;
            state.CarryingLifeBerries--;
            process.AudioSession.Play("event:/game/general/strawberry_get", Position, process.SaveContext, "colour", 0f,
                "count", 0f);
            sprite.Play("collect");
            SourceAlarmState.Set(process.AlarmPool, OwnedComponents, .3f, () =>
            {
                ((OriginalLevelSceneRoot)Scene).Scheduler.QueueSourceEntity(
                    new OriginalStrawberryPointsState(Position, false, 6, false,
                        (IOriginalStrawberryPointsProcess)process));
                RemoveSelf();
            });
        }

        private void OnAnimate(string id)
        {
            if (sprite.CurrentAnimationFrame != 35) return;
            lightTween.Start();
            process.AudioSession.Play("event:/game/general/strawberry_pulse", Position, process.SaveContext);
            var level = (OriginalLevelSceneRoot)Scene;
            bool inside = false;
            inside = global::Celeste.Simulation.Gameplay.Collision.SourceSceneCollisionQueries.Check(this,
                         level.Scheduler.Entities.GetTrackedEntities("Celeste.Solid"))
                     || global::Celeste.Simulation.Gameplay.Collision.SourceSceneCollisionQueries.Check(this,
                         level.Scheduler.Entities.GetTrackedEntities("Celeste.FakeWall"));
            level.AddStrawberryDisplacementBurst(Position, .6f, 4f, 28f, !Follower.HasLeader && inside ? .1f : .2f);
        }
    }
}
