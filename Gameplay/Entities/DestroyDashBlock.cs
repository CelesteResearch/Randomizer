using System.Numerics;
using System.Reflection;
using Celeste.Core.Components;
using Celeste.Core.Maps;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Scheduling;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Formats.Map.Entities.Attributes;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Gameplay.Walls.Breakable;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Triggers.Foundation;

namespace Celeste.Mods.Randomizer.Gameplay.Entities
{
    public sealed class DestroyDashBlock : SourceTriggerEntityState
    {
        private readonly int entityId;

        private static readonly FieldInfo BlockId =
            typeof(OriginalDashBlockState).GetField("id", BindingFlags.Instance | BindingFlags.NonPublic);

        public DestroyDashBlock(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset,
                data.Width, data.Height, ((ISourceExtremaEnvironment)engine).ExtremaArithmetic)
        {
            entityId = EntityAttributeReader.Int(data.Values, "entID");
        }

        public override void OnEnter(RoomPlayerSession player)
        {
            var level = (OriginalLevelSceneRoot)Scene;
            foreach (var entity in level.Scheduler.Entities.Entities)
                if (entity is OriginalDashBlockState block && ((MapEntityId)BlockId.GetValue(block)).ID == entityId)
                {
                    block.Break(Vector2.Zero, Vector2.Zero, true, true);
                    RemoveSelf();
                    return;
                }

            System.Diagnostics.Trace.WriteLine("Randomizer: missing dash block " + entityId + " in " +
                                               level.Session.Level);
            RemoveSelf();
        }
    }
}
