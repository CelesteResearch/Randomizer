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
    public class RandoMetadataMusic
    {
        public string Name;
        private float weight = 1;
        public Dictionary<string, int> Parameters = new Dictionary<string, int>();

        public float Weight
        {
            get => this.weight;
            set => this.weight = (value >= 0 && value <= 3) ? value : 1f;
        }
    }
}
