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
    public class RandoMetadataCampaign
    {
        public string Name;
        public List<RandoMetadataLevelSet> LevelSets;
    }
}
