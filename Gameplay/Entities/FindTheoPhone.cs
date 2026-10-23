using System.Numerics;
using Celeste.Core.Geometry;
using Celeste.Core.Maps;
using Celeste.Game.Components.Talking;
using Celeste.Game.Entities.Npcs.Theo;
using Celeste.Game.Runtime;
using Celeste.Game.Execution.Levels;
using Celeste.Simulation;

namespace Celeste.Mods.Randomizer.Gameplay.Entities
{
    public sealed class FindTheoPhone : OriginalTheoPhoneState
    {
        private readonly GameApplication engine;
        public readonly MapEntityId ID;

        public FindTheoPhone(GameApplication engine, Vector2 position, MapEntityId id) : base(engine, position)
        {
            this.engine = engine;
            ID = id;
            OwnedComponents.Add(new OriginalTalkComponent(engine, new IntRect(-12, -8, 24, 8),
                new Vector2(99999, 99999), OnTalk));
        }

        private void OnTalk(RoomPlayerSession player)
        {
            var level = (OriginalLevelSceneRoot)Scene;
            var actual =
                level.Scheduler.Entities.GetTrackedEntity<RoomPlayerSession>(typeof(RoomPlayerSession).FullName);
            level.Scheduler.QueueSourceEntity(new FindTheoPhoneCutscene(engine, actual, this));
        }
    }
}
