using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Numerics;

namespace Celeste.Mods.Randomizer.Generation.Graph
{
    public class StaticEdgeReversed : StaticEdge
    {
        public StaticEdge Child;

        public StaticEdgeReversed(StaticEdge Child, StaticNode target)
        {
            this.Child = Child;
            this.NodeTarget = target;
            this.FromNode = Child.FromNode;
        }

        public override Requirement ReqIn
        {
            get { return Child.ReqOut; }
        }

        public override Requirement ReqOut
        {
            get { return Child.ReqIn; }
        }
    }
}
