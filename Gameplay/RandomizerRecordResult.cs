namespace Celeste.Mods.Randomizer.Gameplay
{
    public readonly struct RandomizerRecordResult
    {
        public readonly bool BeatBestTime;
        public readonly bool BeatBestTimePlatinum;

        public RandomizerRecordResult(bool beatBestTime, bool beatBestTimePlatinum)
        {
            BeatBestTime = beatBestTime;
            BeatBestTimePlatinum = beatBestTimePlatinum;
        }
    }
}
