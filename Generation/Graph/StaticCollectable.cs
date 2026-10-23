using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Numerics;

namespace Celeste.Mods.Randomizer.Generation.Graph
{
    public class StaticCollectable
    {
        public Vector2 Position;
        public bool MustFly;
        public StaticNode ParentNode;

        public override string ToString()
        {
            return $"{this.ParentNode}:{this.Position}";
        }
    }
}
