using System;
using System.Numerics;
using System.Reflection;
using Celeste.Core.Components;
using Celeste.Core.Maps;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Scheduling;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Formats.Map.Entities.Attributes;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Gameplay.Mechanisms.DashSwitches;
using Celeste.Game.Gameplay.Mechanisms.Switches;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Triggers.Foundation;

namespace Celeste.Mods.Randomizer.Gameplay.Entities
{
    public sealed class ActivateSwitches : SourceTriggerEntityState
    {
        private readonly string type;
        private readonly int entityId;

        private static readonly FieldInfo ButtonId =
            typeof(OriginalDashSwitchState).GetField("id", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo PressDirection =
            typeof(OriginalDashSwitchState).GetField("pressDirection", BindingFlags.Instance | BindingFlags.NonPublic);

        public ActivateSwitches(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset,
                data.Width, data.Height, ((ISourceExtremaEnvironment)engine).ExtremaArithmetic)
        {
            type = data.Values["type"].ToString();
            entityId = EntityAttributeReader.Int(data.Values, "entID");
        }

        public override void OnEnter(RoomPlayerSession player)
        {
            var level = (OriginalLevelSceneRoot)Scene;
            bool found = false;
            foreach (var entity in level.Scheduler.Entities.Entities)
            {
                if (type == "touchswitch" && entity is OriginalTouchSwitchState touch)
                {
                    touch.TurnOn();
                    found = true;
                }
                else if (type == "button" && entity is OriginalDashSwitchState button
                                          && ((MapEntityId)ButtonId.GetValue(button)).ID == entityId)
                {
                    button.OnDashed(player, (Vector2)PressDirection.GetValue(button));
                    found = true;
                    break;
                }
            }

            if (found) RemoveSelf();
            else
                System.Diagnostics.Trace.WriteLine("Randomizer: missing " + type + " switch " + entityId + " in " +
                                                   level.Session.Level);
        }
    }
}
