using Celeste.Mods.Randomizer.Generation.Configuration.Settings;
using System.Collections.Generic;
using Celeste.Everest.Modules.Settings;
using Celeste.Everest.Settings.Metadata;

namespace Celeste.Mods.Randomizer.Settings
{
    public class RandoModuleSettings : EverestModuleSettings
    {
        [SettingIgnore] public int StartCounter { get; set; }
        [SettingIgnore] public Dictionary<uint, long> BestTimes { get; set; } = new Dictionary<uint, long>();

        [SettingIgnore]
        public Dictionary<string, RecordTuple> BestSetSeedTimes { get; set; } = new Dictionary<string, RecordTuple>();

        [SettingIgnore]
        public Dictionary<string, RecordTuple> BestRandomSeedTimes { get; set; } =
            new Dictionary<string, RecordTuple>();

        [SettingIgnore] public string CurrentVersion { get; set; }
        [SettingIgnore] public RandoSettings SavedSettings { get; set; }
        public bool FastMenu { get; set; }
        public bool LazyLoading { get; set; } = true;
    }
}
