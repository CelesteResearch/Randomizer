using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using System.Numerics;
using YamlDotNet.Core.Tokens;

namespace Celeste.Mods.Randomizer.Generation.Configuration.Metadata
{
    public class RandoMetadataRuleset
    {
        public string Name;
        private string longName;

        public string LongName
        {
            get => this.longName ?? "Ruleset " + this.Name;
            set => this.longName = value;
        }

        public List<RandoSettings.AreaKeyNotStupid> EnabledMaps = null;
        public bool RepeatRooms = false;
        public bool EnterUnknown = false;
        public bool Variants = false;
        public ShineLights Lights = ShineLights.Hubs;
        public Darkness Darkness = Darkness.Never;
        public LogicType Algorithm = LogicType.Pathway;
        public MapLength Length = MapLength.Short;
        public NumDashes Dashes = NumDashes.One;
        public Difficulty Difficulty = Difficulty.Normal;
        public DifficultyEagerness DifficultyEagerness = DifficultyEagerness.Medium;
        public StrawberryDensity Strawberries = StrawberryDensity.None;
        public int Lives = 0;
    }
}
