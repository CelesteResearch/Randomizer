using System;
using System.Collections.Generic;

namespace Celeste.Mods.Randomizer.Generation.Requirements
{
    using FlagSet = Dictionary<string, FlagState>;

    public class Conjunction : Requirement
    {
        public readonly List<Requirement> Children;

        public Conjunction(List<Requirement> Children)
        {
            Requirement.Normalize(Children);
            this.Children = Children;
        }

        public override bool Able(Capabilities state)
        {
            foreach (var child in this.Children)
            {
                if (!child.Able(state))
                {
                    return false;
                }
            }

            return true;
        }

        public override Requirement Conflicts(Capabilities state)
        {
            var failures = new List<Requirement>();
            foreach (var child in this.Children)
            {
                var failure = child.Conflicts(state);
                if (failure is Possible)
                {
                    continue;
                }

                if (failure is Impossible)
                {
                    return failure;
                }

                failures.Add(failure);
            }

            if (failures.Count == 0)
            {
                return new Possible();
            }
            else if (failures.Count == 1)
            {
                return failures[0];
            }
            else
            {
                return new Conjunction(failures);
            }
        }

        public override int Complexity()
        {
            int result = 0;
            foreach (var child in Children)
            {
                result += child.Complexity();
            }

            return result;
        }

        public override bool Equals(Requirement other)
        {
            // TODO: this assumes that hashes are unique...
            if (!(other is Conjunction i))
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
            int result = 7777;
            foreach (var child in this.Children)
            {
                result ^= child.GetHashCode();
                result = (result << 10) | (result >> 22);
            }

            return result;
        }

        public override bool StrictlyBetterThan(Requirement other)
        {
            if (other is Possible)
            {
                return false;
            }

            if (other is Impossible)
            {
                return true;
            }

            if (other is Conjunction c)
            {
                // in order for this to be strictly better than other, all of this' elements must be present in other
                // and other must have extra elements
                if (this.Children.Count >= c.Children.Count)
                {
                    return false;
                }

                foreach (var thisChild in this.Children)
                {
                    var found = false;
                    foreach (var cChild in c.Children)
                    {
                        if (cChild.Equals(thisChild))
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

            // a conjunction cannot possibly be a strict improvement over something which is not a conjunction
            return false;
        }
    }
}
