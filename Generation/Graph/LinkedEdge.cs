using System;
using System.Linq;
using System.Collections.Generic;
using System.Numerics;

namespace Celeste.Mods.Randomizer.Generation.Graph
{
    public class LinkedEdge
    {
        public StaticEdge StaticA, StaticB;
        public LinkedNode NodeA, NodeB;
        public Requirement ExtraReqsToA, ExtraReqsToB;

        public LinkedNode OtherNode(LinkedNode One)
        {
            return One == NodeA ? NodeB : One == NodeB ? NodeA : throw new Exception("Misplaced LinkedEdge call");
        }

        public StaticEdge CorrespondingEdge(LinkedNode One)
        {
            return One == NodeA ? StaticA : One == NodeB ? StaticB : throw new Exception("Misplaced LinkedEdge call");
        }

        public StaticEdge OtherEdge(LinkedNode One)
        {
            return One == NodeA ? StaticB : One == NodeB ? StaticA : throw new Exception("Misplaced LinkedEdge call");
        }

        public ref Requirement ExtraReqsFrom(LinkedNode One)
        {
            if (One == this.NodeA)
            {
                return ref this.ExtraReqsToB;
            }
            else if (One == this.NodeB)
            {
                return ref this.ExtraReqsToA;
            }

            throw new Exception("Misplaced LinkedEdge call");
        }

        public ref Requirement ExtraReqsTo(LinkedNode One)
        {
            if (One == this.NodeA)
            {
                return ref this.ExtraReqsToA;
            }
            else if (One == this.NodeB)
            {
                return ref this.ExtraReqsToB;
            }

            throw new Exception("Misplaced LinkedEdge call");
        }

        public Requirement ReqsFrom(LinkedNode one)
        {
            var extra = this.ExtraReqsTo(one);
            var total = new List<Requirement>
            {
                this.CorrespondingEdge(one).ReqOut,
                this.OtherEdge(one).ReqIn,
            };
            if (extra != null)
                total.Add(extra);
            return Requirement.And(total);
        }

        public Requirement ReqsTo(LinkedNode one)
        {
            var extra = this.ExtraReqsFrom(one);
            var total = new List<Requirement>
            {
                this.CorrespondingEdge(one).ReqIn,
                this.OtherEdge(one).ReqOut,
            };
            if (extra != null)
                total.Add(extra);
            return Requirement.And(total);
        }
    }
}
