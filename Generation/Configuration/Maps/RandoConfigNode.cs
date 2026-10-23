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
    public class RandoConfigNode
    {
        public int Idx { get; set; }
        public float? X { get; set; }
        public float? Y { get; set; }
    }
}
