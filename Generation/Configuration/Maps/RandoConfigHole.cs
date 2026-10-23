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
    public class RandoConfigHole
    {
        public ScreenDirection Side { get; set; }
        public int Idx { get; set; }
        public int? LowBound { get; set; }
        public int? HighBound { get; set; }

        public string Bound
        {
            get => null;
            set
            {
                if (value.Split(new char[] { ',' }, 2, StringSplitOptions.None) is string[] array &&
                    array.Length == 2 && int.TryParse(array[0], out int lowBound) &&
                    int.TryParse(array[1], out int highBound))
                {
                    LowBound = lowBound;
                    HighBound = highBound;
                }
            }
        }

        public bool? HighOpen { get; set; }
        public RandoConfigReq ReqIn { get; set; }
        public RandoConfigReq ReqOut { get; set; }

        public RandoConfigReq ReqBoth
        {
            get => null;
            set
            {
                this.ReqIn = value;
                this.ReqOut = value;
            }
        }

        public HoleKind Kind { get; set; }

        public int? Launch;
        public bool New;
        public RandoConfigHole Split;
    }
}
