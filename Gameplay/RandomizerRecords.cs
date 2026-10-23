using System;
using Celeste.Mods.Randomizer.Generation.Configuration.Settings;
using Celeste.Mods.Randomizer.Settings;

namespace Celeste.Mods.Randomizer.Gameplay
{
    public static class RandomizerRecords
    {
        public static RandomizerRecordResult Complete(RandoModuleSettings saved, RandoSettings settings,
            long time, bool seedCleanRandom, int score)
        {
            uint hash = uint.Parse(settings.Hash);
            bool beatTime = false, platinum = false;
            if (saved.BestTimes.TryGetValue(hash, out long previous))
            {
                if (time < previous)
                {
                    beatTime = true;
                    saved.BestTimes[hash] = time;
                }
            }
            else saved.BestTimes[hash] = time;

            if (!string.IsNullOrEmpty(settings.Rules))
            {
                bool endless = settings.Algorithm == LogicType.Endless;
                long submitted = endless ? score : time;
                bool Better(long old) => endless ? submitted > old : submitted < old;
                if (!endless || settings.SeedType != SeedType.Random)
                {
                    if (saved.BestSetSeedTimes.TryGetValue(settings.Rules, out var bestSet))
                    {
                        if (Better(bestSet.Item1))
                        {
                            platinum = true;
                            saved.BestSetSeedTimes[settings.Rules] = RecordTuple.Create(submitted, settings.Seed);
                        }
                    }
                    else saved.BestSetSeedTimes[settings.Rules] = RecordTuple.Create(submitted, settings.Seed);
                }

                if (seedCleanRandom)
                {
                    if (saved.BestRandomSeedTimes.TryGetValue(settings.Rules, out var bestRandom))
                    {
                        if (Better(bestRandom.Item1))
                        {
                            platinum = true;
                            saved.BestRandomSeedTimes[settings.Rules] = RecordTuple.Create(submitted, settings.Seed);
                        }
                    }
                    else saved.BestRandomSeedTimes[settings.Rules] = RecordTuple.Create(submitted, settings.Seed);
                }
            }

            return new RandomizerRecordResult(beatTime, platinum);
        }
    }
}
