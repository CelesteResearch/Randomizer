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
    public class RandoConfigReq
    {
        public List<RandoConfigReq> And;
        public List<RandoConfigReq> Or;
        public Difficulty Difficulty = Difficulty.Normal;
        public NumDashes? Dashes;
        public bool Key;
        public int? KeyholeID;
        public string Flag;
    }
}
