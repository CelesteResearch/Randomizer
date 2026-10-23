using System.Numerics;
using Celeste.Formats.Map.Entities.Attributes;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Scene.Entities;

namespace Celeste.Mods.Randomizer.Gameplay
{
    public sealed partial class RandomizerGameplay
    {
        private bool LoadEntity(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
            OriginalLevelEntityData data)
        {
            if (!Owns(level)) return false;
            SceneEntityState entity;
            switch (data.Name)
            {
                case "randomizer/ActivateSwitches": entity = new Entities.ActivateSwitches(engine, data, offset); break;
                case "randomizer/DestroyDashBlock": entity = new Entities.DestroyDashBlock(engine, data, offset); break;
                case "randomizer/TheoPhone":
                    entity = new Entities.FindTheoPhone(engine, data.Position + offset,
                        level.CreateEntityId(room, data)); break;
                case "randomizer/LifeBerry":
                    entity = new Entities.LifeBerry(engine, SessionState(level.Session), data.Position + offset,
                        level.CreateEntityId(room, data), EntityAttributeReader.Bool(data.Values, "AutoBubble")); break;
                default: return false;
            }

            entity.SourceData = data;
            entity.SourceId = level.CreateEntityId(room, data);
            level.Scheduler.QueueSourceEntity(entity);
            return true;
        }
    }
}
