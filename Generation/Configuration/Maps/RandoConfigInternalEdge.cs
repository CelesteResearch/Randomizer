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
    public class RandoConfigInternalEdge
    {
        public String To { get; set; }
        public String Warp { get; set; }
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

        public enum SplitKind
        {
            TopToBottom,
            BottomToTop,
            LeftToRight,
            RightToLeft,
        }

        public SplitKind? Split;
        public int? Collectable;
        public bool MustFly;
        public bool CustomWarp;
    }
}
