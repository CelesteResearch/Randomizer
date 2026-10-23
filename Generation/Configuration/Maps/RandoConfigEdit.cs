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
    public class RandoConfigEdit
    {
        public String Name { get; set; }
        public int? ID { get; set; }
        public float? X { get; set; }
        public float? Y { get; set; }
        public RandoConfigDecalType Decal { get; set; }
        public RandoConfigUpdate Update { get; set; }

        [YamlIgnore] public bool Done;

        public override string ToString()
        {
            string o =
                $"{Name ?? "*"}:{(ID == null ? "*" : ID.ToString())}, X:{(X == null ? "*" : X.ToString())}, Y:{(Y == null ? "*" : Y.ToString())}";
            if (Decal != RandoConfigDecalType.None)
            {
                o += $" {Decal}";
            }

            return o;
        }
    }
}
