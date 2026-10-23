using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using System.Numerics;
using YamlDotNet.Core.Tokens;

namespace Celeste.Mods.Randomizer.Generation.Configuration.Maps
{
    public class RandoConfigUpdate
    {
        public bool Remove { get; set; }
        public bool Add { get; set; }
        public bool Default { get; set; }
        public string Name { get; set; }
        public float? X { get; set; }
        public float? Y { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public float? ScaleX { get; set; }
        public float? ScaleY { get; set; }
        public List<RandoConfigNode> Nodes { get; set; }
        public Dictionary<string, string> Values { get; set; }

        public string Tile;
    }
}
