using System;
using System.Collections.Generic;

namespace Celeste.Mods.Randomizer.Generation.Requirements
{
    using FlagSet = Dictionary<string, FlagState>;

    public class Disjunction : Requirement
    {
        public readonly List<Requirement> Children;

        public Disjunction(List<Requirement> Children)
        {
            Requirement.Normalize(Children);
            this.Children = Children;
        }

        public override bool Able(Capabilities state)
        {
            foreach (var child in this.Children)
            {
                if (child.Able(state))
                {
                    return true;
                }
            }

            return false;
        }

        public override Requirement Conflicts(Capabilities state)
        {
            var failures = new List<Requirement>();
            foreach (var child in this.Children)
            {
                var failure = child.Conflicts(state);
                if (failure is Possible)
                {
                    return failure;
                }

                if (failure is Impossible)
                {
                    continue;
                }

                failures.Add(failure);
            }

            if (failures.Count == 0)
            {
                return new Impossible();
            }
            else if (failures.Count == 1)
            {
                return failures[0];
            }
            else
            {
                return new Disjunction(failures);
            }
        }

        public override int Complexity()
        {
            int result = int.MaxValue;
            foreach (var child in Children)
            {
                result = Math.Min(result, child.Complexity());
            }

            return result;
        }

        public override bool Equals(Requirement other)
        {
            // TODO: this assumes that hashes are unique...
            if (!(other is Disjunction i))
            {
                return false;
            }

            if (this.Children.Count != i.Children.Count)
            {
                return false;
            }

            for (int j = 0; j < this.Children.Count; j++)
            {
                if (!this.Children[j].Equals(i.Children[j]))
                {
                    return false;
                }
            }

            return true;
        }

        public override int GetHashCode()
        {
            int result = 8888;
            foreach (var child in this.Children)
            {
                result ^= child.GetHashCode();
                result = (result << 10) | (result >> 22);
            }

            return result;
        }

        public override bool StrictlyBetterThan(Requirement other)
        {
            if (base.StrictlyBetterThan(other))
            {
                return true;
            }

            if (other is Disjunction d)
            {
                // for this to be a strict improvement, this must be larger than other and contain all other's elements
                if (this.Children.Count <= d.Children.Count)
                {
                    return false;
                }

                foreach (var dChild in d.Children)
                {
                    var found = false;
                    foreach (var thisChild in this.Children)
                    {
                        if (thisChild.Equals(dChild))
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        return false;
                    }
                }

                return true;
            }

            // can be considered an improvement if the child is a member of the disjunction
            if (this.Children.Count < 2)
            {
                return false;
            }

            foreach (var child in this.Children)
            {
                if (child.Equals(other))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
