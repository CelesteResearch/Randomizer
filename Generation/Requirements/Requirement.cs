using System;
using System.Collections.Generic;

namespace Celeste.Mods.Randomizer.Generation.Requirements
{
    using FlagSet = Dictionary<string, FlagState>;

    public abstract class Requirement : IComparable<Requirement>, IComparable
    {
        public abstract bool Able(Capabilities state);

        public virtual Requirement Conflicts(Capabilities state)
        {
            if (this.Able(state))
            {
                return new Possible();
            }

            return this;
        }

        public virtual int Complexity()
        {
            return 1;
        }

        public int CompareTo(Requirement obj)
        {
            return this.Complexity().CompareTo(obj.Complexity());
        }

        public int CompareTo(object obj)
        {
            if (!(obj is Requirement other))
            {
                throw new ArgumentException("Can only compare Requirement to Requirement");
            }

            return this.CompareTo(other);
        }

        public abstract bool Equals(Requirement other);

        protected static void Normalize(List<Requirement> requirements)
        {
            // XXX EXTREMELY IMPORTANT XXX
            // MAKE SURE GetHashCode NEVER CALLS ANY BASE CLASS GetHashCode
            // OTHERWISE YOU WILL SEE BEHAVIOR DIVERGENCES BETWEEN C# VERSIONS
            requirements.Sort((Requirement a, Requirement b) => a.GetHashCode().CompareTo(b.GetHashCode()));
            for (int i = 0; i < requirements.Count - 1;)
            {
                var a = requirements[i];
                var b = requirements[i + 1];
                if (a.Equals(b))
                {
                    requirements.RemoveAt(i + 1);
                }
                else
                {
                    i++;
                }
            }
        }

        public static Requirement And(IEnumerable<Requirement> things)
        {
            var children = new List<Requirement>();
            foreach (var thing in things)
            {
                if (thing is Conjunction c)
                {
                    children.AddRange(c.Children);
                }
                else
                {
                    children.Add(thing);
                }
            }

            for (int i = 0; i < children.Count;)
            {
                if (children[i] is Possible)
                {
                    children.RemoveAt(i);
                }
                else if (children[i] is Impossible)
                {
                    return children[i];
                }
                else
                {
                    i++;
                }
            }

            Normalize(children);
            if (children.Count == 0)
            {
                return new Possible();
            }

            if (children.Count == 1)
            {
                return children[0];
            }

            return new Conjunction(children);
        }

        public static Requirement Or(IEnumerable<Requirement> things)
        {
            var children = new List<Requirement>();
            foreach (var thing in things)
            {
                if (thing is Disjunction c)
                {
                    children.AddRange(c.Children);
                }
                else
                {
                    children.Add(thing);
                }
            }

            for (int i = 0; i < children.Count;)
            {
                if (children[i] is Impossible)
                {
                    children.RemoveAt(i);
                }
                else if (children[i] is Possible)
                {
                    return children[i];
                }
                else
                {
                    i++;
                }
            }

            Normalize(children);
            if (children.Count == 0)
            {
                return new Impossible();
            }

            if (children.Count == 1)
            {
                return children[0];
            }

            return new Disjunction(children);
        }

        public virtual bool StrictlyBetterThan(Requirement other)
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
                foreach (var child in c.Children)
                {
                    if (child.Equals(this))
                    {
                        return true;
                    }
                }

                return false;
            }

            return false;
        }
    }
}
