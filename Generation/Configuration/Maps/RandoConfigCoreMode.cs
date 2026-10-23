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
    public class RandoConfigCoreMode
    {
        private RoomCoreMode? left, right, up, down;
        public RoomCoreMode All = RoomCoreMode.None;

        public RoomCoreMode Left
        {
            get => left ?? All;
            set => left = value;
        }

        public RoomCoreMode Right
        {
            get => right ?? All;
            set => right = value;
        }

        public RoomCoreMode Up
        {
            get => up ?? All;
            set => up = value;
        }

        public RoomCoreMode Down
        {
            get => down ?? All;
            set => down = value;
        }
    }
}
