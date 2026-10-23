using System;
using System.Linq;
using System.Collections.Generic;
using System.Numerics;

namespace Celeste.Mods.Randomizer.Generation.Graph
{
    public class LinkedNode : IComparable<LinkedNode>, IComparable
    {
        public StaticNode Static;
        public LinkedRoom Room;
        public List<LinkedEdge> Edges = new List<LinkedEdge>();

        public Dictionary<StaticCollectable, Tuple<LinkedCollectable, bool>> Collectables =
            new Dictionary<StaticCollectable, Tuple<LinkedCollectable, bool>>();

        public override string ToString()
        {
            return $"{this.Static.Name}@{this.Room}";
        }

        public int CompareTo(LinkedNode obj)
        {
            return 0;
        }

        public int CompareTo(object obj)
        {
            if (!(obj is LinkedNode other))
            {
                throw new ArgumentException("Must compare LinkedNode to LinkedNode");
            }

            return this.CompareTo(other);
        }

        public IEnumerable<LinkedNode> Successors(Capabilities capsForward, Capabilities capsReverse,
            bool onlyInternal = false)
        {
            foreach (var iedge in this.Static.Edges)
            {
                if (iedge.NodeTarget == null)
                {
                    continue;
                }

                if (capsForward != null && !iedge.ReqOut.Able(capsForward))
                {
                    continue;
                }

                if (capsReverse != null && !iedge.ReqIn.Able(capsReverse))
                {
                    continue;
                }

                if (iedge.NodeTarget.ParentRoom == this.Room.Static)
                {
                    yield return this.Room.Nodes[iedge.NodeTarget.Name];
                }
                else
                {
                    yield return this.Room.WarpMap[iedge.NodeTarget.ParentRoom.Level.Name].Nodes[iedge.NodeTarget.Name];
                }
            }

            if (!onlyInternal)
            {
                foreach (var edge in this.Edges)
                {
                    var check1 = edge.CorrespondingEdge(this);
                    var check2 = edge.OtherEdge(this);
                    if (capsForward != null && (!check1.ReqOut.Able(capsForward) || !check2.ReqIn.Able(capsForward)))
                    {
                        continue;
                    }

                    if (capsReverse != null && (!check1.ReqIn.Able(capsReverse) || !check2.ReqOut.Able(capsReverse)))
                    {
                        continue;
                    }

                    yield return edge.OtherNode(this);
                }
            }
        }

        public IEnumerable<Tuple<LinkedNode, Requirement>> SuccessorsRequires(Capabilities capsForward,
            bool onlyInternal = false)
        {
            foreach (var iedge in this.Static.Edges)
            {
                if (iedge.NodeTarget == null)
                {
                    continue;
                }

                var reqs = iedge.ReqOut.Conflicts(capsForward);
                if (reqs is Impossible)
                {
                    continue;
                }

                if (iedge.NodeTarget.ParentRoom == this.Room.Static)
                {
                    yield return Tuple.Create(this.Room.Nodes[iedge.NodeTarget.Name], reqs);
                }
                else
                {
                    yield return Tuple.Create(
                        this.Room.WarpMap[iedge.NodeTarget.ParentRoom.Level.Name].Nodes[iedge.NodeTarget.Name], reqs);
                }
            }

            if (!onlyInternal)
            {
                foreach (var edge in this.Edges)
                {
                    var check1 = edge.CorrespondingEdge(this);
                    var check2 = edge.OtherEdge(this);
                    var reqs = Requirement.And(new List<Requirement>
                        { check1.ReqOut.Conflicts(capsForward), check2.ReqIn.Conflicts(capsForward) });
                    if (reqs is Impossible)
                    {
                        continue;
                    }

                    yield return Tuple.Create(edge.OtherNode(this), reqs);
                }
            }
        }

        public IEnumerable<StaticEdge> UnlinkedEdges(Capabilities capsForward, Capabilities capsReverse)
        {
            foreach (var staticedge in this.Static.Edges)
            {
                if (staticedge.HoleTarget == null && !staticedge.CustomWarp)
                {
                    continue;
                }

                bool found = false;
                foreach (var linkededge in this.Edges)
                {
                    if (linkededge.CorrespondingEdge(this) == staticedge)
                    {
                        found = true;
                        break;
                    }
                }

                if (found)
                {
                    continue;
                }

                if (capsForward != null && !staticedge.ReqOut.Able(capsForward))
                {
                    continue;
                }

                if (capsReverse != null && !staticedge.ReqIn.Able(capsReverse))
                {
                    continue;
                }

                yield return staticedge;
            }
        }

        public IEnumerable<StaticCollectable> UnlinkedCollectables()
        {
            foreach (var c in this.Static.Collectables)
            {
                if (!this.Collectables.ContainsKey(c))
                {
                    yield return c;
                }
            }
        }
    }
}
