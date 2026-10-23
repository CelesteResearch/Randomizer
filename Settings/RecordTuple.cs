using Celeste.Mods.Randomizer.Generation.Configuration.Settings;

namespace Celeste.Mods.Randomizer.Settings
{
    public struct RecordTuple
    {
        public long Item1;
        public string Item2;

        public static RecordTuple Create(long time, string seed) => new RecordTuple { Item1 = time, Item2 = seed };
    }
}
