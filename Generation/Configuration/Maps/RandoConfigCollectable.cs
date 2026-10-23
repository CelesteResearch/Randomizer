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
    public class RandoConfigCollectable
    {
        public int? Idx;
        public int? X;
        public int? Y;
        public bool MustFly;
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
    }
}
