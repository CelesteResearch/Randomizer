using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Numerics;

namespace Celeste.Mods.Randomizer.Generation.Graph
{
    public class StaticEdge
    {
        public StaticNode FromNode;
        public StaticNode NodeTarget;
        public virtual Requirement ReqIn { get; set; }
        public virtual Requirement ReqOut { get; set; }

        public Hole HoleTarget;
        public bool CustomWarp;

        public override string ToString()
        {
            if (this.HoleTarget != null)
            {
                return $"{this.HoleTarget}@{this.FromNode}";
            }
            else if (this.CustomWarp)
            {
                return $"CustomWarp@{this.FromNode}";
            }
            else
            {
                return $"{this.FromNode} -> {this.NodeTarget}";
            }
        }
    }
}
