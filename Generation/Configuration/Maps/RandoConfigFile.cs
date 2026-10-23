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
    public class RandoConfigFile
    {
        // null means the side does not exist. zero rooms means the side needs to be lazy-loaded
        public List<RandoConfigRoom> ASide { get; set; }
        public List<RandoConfigRoom> BSide { get; set; }
        public List<RandoConfigRoom> CSide { get; set; }

        public Dictionary<char, char> RedirectFGTiles = new Dictionary<char, char>();
        public string ConfigPath;
        public string ModName;
        public static string SkeletonOutput;

        public List<RandoConfigRoom> GetRooms(int mode)
        {
            switch (mode)
            {
                case 0:
                default:
                    return this.ASide;
                case 1:
                    return this.BSide;
                case 2:
                    return this.CSide;
            }
        }
    }
}
