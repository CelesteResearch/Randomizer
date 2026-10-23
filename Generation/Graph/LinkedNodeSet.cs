using System;
using System.Linq;
using System.Collections.Generic;
using System.Numerics;

namespace Celeste.Mods.Randomizer.Generation.Graph
{
    public class LinkedNodeSet
    {
        public List<LinkedNode> Nodes;
        private Capabilities CapsForward;
        private Capabilities CapsReverse;
        private Random Random;

        public static LinkedNodeSet Closure(LinkedNode start, Capabilities capsForward, Capabilities capsReverse,
            bool internalOnly, int maxDistance = 9999)
        {
            var result = new LinkedNodeSet(new List<LinkedNode> { start });
            result.Extend(capsForward, capsReverse, internalOnly, maxDistance);
            return result;
        }

        public void Extend(Capabilities capsForward, Capabilities capsReverse, bool internalOnly,
            int maxDistance = 999999)
        {
            var queue = new Queue<Tuple<LinkedNode, int>>();
            foreach (var seen in this.Nodes)
            {
                queue.Enqueue(Tuple.Create(seen, maxDistance));
            }

            void enqueue(LinkedNode node, int remaining)
            {
                if (remaining > 0 && !this.Nodes.Contains(node))
                {
                    queue.Enqueue(Tuple.Create(node, remaining));
                    this.Nodes.Add(node);
                }
            }

            while (queue.Count != 0)
            {
                var item = queue.Dequeue();
                foreach (var succ in item.Item1.Successors(capsForward, capsReverse, internalOnly))
                {
                    enqueue(succ, item.Item2 - 1);
                }
            }

            this.CapsForward = capsForward;
            this.CapsReverse = capsReverse;
        }

        public static Requirement TraversalRequires(LinkedNode start, Capabilities capsForward, bool internalOnly,
            UnlinkedEdge end)
        {
            return Requirement.And(new List<Requirement>
            {
                TraversalRequires(start, capsForward, internalOnly, end.Node), end.Static.ReqOut.Conflicts(capsForward)
            });
        }

        public static Requirement TraversalRequires(LinkedNode start, Capabilities capsForward, bool internalOnly,
            LinkedNode end)
        {
            var queue = new PriorityQueue<Tuple<Requirement, LinkedNode>>();
            var seen = new Dictionary<LinkedNode, List<Requirement>>();
            Requirement p = new Possible();
            queue.Enqueue(Tuple.Create(p, start));
            seen[start] = new List<Requirement>
            {
                p
            };
            // implementation question: should this loop break as soon as one path to end is found, or should it exhaust the queue?
            // for now, let's go with exhaust the queue so if there's a Possible we don't miss it
            while (queue.Count != 0)
            {
                var entry = queue.Dequeue();
                var entryReq = entry.Item1;
                var entryNode = entry.Item2;
                foreach (var where in entryNode.SuccessorsRequires(capsForward))
                {
                    var realReq = Requirement.And(new List<Requirement> { entryReq, where.Item2 });
                    var nextNode = where.Item1;
                    if (!seen.TryGetValue(nextNode, out var seenLst))
                    {
                        seenLst = new List<Requirement>();
                        seen[nextNode] = seenLst;
                    }

                    // search for any requirement already seen which obsoletes this new requirement
                    var found = false;
                    foreach (var req in seenLst)
                    {
                        if (req.Equals(realReq) || req.StrictlyBetterThan(realReq))
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        seenLst.Add(realReq);
                        queue.Enqueue(Tuple.Create(realReq, nextNode));
                    }
                }
            }

            if (!seen.TryGetValue(end, out var disjunct))
            {
                return new Impossible();
            }

            return Requirement.Or(disjunct);
        }

        private LinkedNodeSet(List<LinkedNode> nodes)
        {
            this.Nodes = nodes;
        }

        public LinkedNodeSet(LinkedNodeSet toCopy)
        {
            this.Nodes = new List<LinkedNode>(toCopy.Nodes);
            this.CapsForward = toCopy.CapsForward;
            this.CapsReverse = toCopy.CapsReverse;
            this.Random = toCopy.Random;
        }

        public LinkedNodeSet Shuffle(Random random)
        {
            this.Random = random;
            return this;
        }

        public List<UnlinkedEdge> UnlinkedEdges(Func<UnlinkedEdge, bool> filter = null)
        {
            var result = new List<UnlinkedEdge>();
            foreach (var node in this.Nodes)
            {
                foreach (var edge in node.UnlinkedEdges(this.CapsForward, this.CapsReverse))
                {
                    var uEdge = new UnlinkedEdge(node, edge);
                    if (filter != null && !filter(uEdge))
                    {
                        continue;
                    }

                    result.Add(uEdge);
                }
            }

            if (this.Random != null)
            {
                result.Shuffle(this.Random);
            }

            return result;
        }

        public List<UnlinkedCollectable> UnlinkedCollectables()
        {
            var result = new List<UnlinkedCollectable>();
            foreach (var node in this.Nodes)
            {
                foreach (var col in node.UnlinkedCollectables())
                {
                    result.Add(new UnlinkedCollectable { Node = node, Static = col });
                }
            }

            if (this.Random != null)
            {
                result.Shuffle(this.Random);
            }

            return result;
        }
    }
}
