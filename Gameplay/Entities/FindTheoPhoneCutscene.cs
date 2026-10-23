using System;
using System.Collections;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Scheduling;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Levels.Cutscenes;
using Celeste.Game.Runtime;
using Celeste.Game.UI.Dialogue.Textboxes;
using Celeste.Scene.Entities;
using Celeste.Simulation;

namespace Celeste.Mods.Randomizer.Gameplay.Entities
{
    public sealed class FindTheoPhoneCutscene : OriginalCutsceneEntityState
    {
        private readonly GameApplication engine;
        private readonly RoomPlayerSession player;
        private readonly FindTheoPhone phone;
        private readonly float targetX;
        private bool savedInvincible;
        public int State;

        public FindTheoPhoneCutscene(GameApplication engine, RoomPlayerSession player, FindTheoPhone phone) :
            base(engine)
        {
            this.engine = engine;
            this.player = player;
            this.phone = phone;
            targetX = phone.X + 8;
        }

        public override void OnBegin(OriginalLevelSceneRoot level) =>
            OwnedComponents.Add(new SimulationCoroutine(engine, Routine()));

        private IEnumerator Routine()
        {
            State = 1;
            player.Speed = Vector2.Zero;
            savedInvincible = engine.SaveContext.CurrentSave.Assists.Invincible;
            engine.SaveContext.CurrentSave.Assists.Invincible = true;
            player.Machine.State = 11;
            player.Data.Facing = Math.Sign(targetX - player.X);
            Tags |= OriginalSceneTags.Persistent;
            yield return .5f;
            var point = Level.Camera.CameraToScreen(player.Position);
            point.X = Math.Min(Math.Max(point.X, Level.Camera.Viewport.Width / 4f),
                Level.Camera.Viewport.Width * 3f / 4f);
            point.Y = Math.Min(Math.Max(point.Y, Level.Camera.Viewport.Height / 4f),
                Level.Camera.Viewport.Height * 3f / 4f);
            yield return Level.ZoomTo(point, 2f, .5f);
            yield return TextboxState.Say("RANDO_FOUNDTHEOPHONE", engine, WalkToPhone, StandBackUp);
            yield return Level.ZoomBack(.5f);
            EndCutscene(Level);
        }

        public override void OnEnd(OriginalLevelSceneRoot level)
        {
            var reset = new SceneEntityState(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration);
            reset.OwnedComponents.Add(new SimulationCoroutine(engine, ResetInvincible()));
            reset.Tags |= OriginalSceneTags.Global;
            ((Celeste.Scene.Tracking.ISourceScene)engine.Frame.Scenes.CurrentScene).SourceScene
                .QueueSourceEntity(reset);
            Tags &= ~OriginalSceneTags.Persistent;
            player.Machine.State = 0;
            Level.Session.DoNotLoad.Add(phone.ID);
            phone.RemoveSelf();
            State = 2;
        }

        private IEnumerator ResetInvincible()
        {
            yield return 1f;
            engine.SaveContext.CurrentSave.Assists.Invincible = savedInvincible;
        }

        private IEnumerator WalkToPhone()
        {
            yield return .25f;
            yield return player.DummyWalkToExact((int)targetX);
            player.Data.Facing = -1;
            yield return .5f;
            player.Dummy.AutoAnimate = false;
            player.Sprite.Play("duck");
            yield return .5f;
        }

        private IEnumerator StandBackUp()
        {
            phone.RemoveSelf();
            yield return .6f;
            player.Sprite.Play("idle");
            yield return .2f;
        }
    }
}
