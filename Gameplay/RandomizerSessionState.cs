using System.Collections.Generic;
using Celeste.Core.Maps;

namespace Celeste.Mods.Randomizer.Gameplay
{
    public sealed class RandomizerSessionState
    {
        public bool SeedCleanRandom;
        public bool BeatBestTimePlatinum;
        public bool SavedByTheBell;
        public int CarryingLifeBerries;
        public readonly HashSet<MapEntityId> TakenLifeBerries = new HashSet<MapEntityId>();
    }
}
