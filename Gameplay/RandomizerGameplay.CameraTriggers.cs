using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Entities.Triggers.Camera;
using Celeste.Everest.Gameplay.Triggers.Camera;
using Celeste.Scene.Entities;
using Celeste.Scene.Triggers;
using Celeste.Simulation;

namespace Celeste.Mods.Randomizer.Gameplay
{
    public sealed partial class RandomizerGameplay
    {
        private static readonly FieldInfo InsideCameraTriggers = typeof(PlayerTriggerSet<RoomPlayerSession>)
            .GetField("inside", BindingFlags.Instance | BindingFlags.NonPublic);

        private void CleanCameraTriggers(OriginalLevelSceneRoot level, RoomPlayerSession player)
        {
            var inside =
                (HashSet<ISourcePlayerTrigger<RoomPlayerSession>>)InsideCameraTriggers.GetValue(player.PlayerTriggers);
            foreach (var trigger in level.Scheduler.Entities.GetTrackedEntities("Celeste.Trigger").ToArray())
            {
                if (trigger is OriginalCameraTargetTriggerState or OriginalCameraAdvanceTargetTriggerState
                    or OriginalCameraOffsetTriggerState or SmoothCameraOffsetTrigger)
                {
                    inside.Remove((ISourcePlayerTrigger<RoomPlayerSession>)trigger);
                    ((SceneEntityState)trigger).RemoveSelf();
                }
            }
        }
    }
}
