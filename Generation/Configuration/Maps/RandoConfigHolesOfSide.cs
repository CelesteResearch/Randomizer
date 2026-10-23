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
    public class RandoConfigHolesOfSide
    {
        public ScreenDirection Side { get; set; }
        public List<RandoConfigHole> Holes { get; set; } = new List<RandoConfigHole>();
    }
}
