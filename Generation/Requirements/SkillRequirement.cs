using System;
using System.Collections.Generic;

namespace Celeste.Mods.Randomizer.Generation.Requirements
{
    using FlagSet = Dictionary<string, FlagState>;

    public class SkillRequirement : Requirement
    {
        public readonly Difficulty Difficulty;

        public SkillRequirement(Difficulty Difficulty)
        {
            this.Difficulty = Difficulty;
        }

        public override bool Able(Capabilities state)
        {
            return state.PlayerSkill >= this.Difficulty;
        }

        public override Requirement Conflicts(Capabilities state)
        {
            if (!this.Able(state))
            {
                return new Impossible();
            }

            return new Possible();
        }

        public override bool Equals(Requirement other)
        {
            if (!(other is SkillRequirement i))
            {
                return false;
            }

            return this.Difficulty == i.Difficulty;
        }

        public override int GetHashCode()
        {
            return 4444 ^ (int)this.Difficulty;
        }

        public override bool StrictlyBetterThan(Requirement other)
        {
            if (base.StrictlyBetterThan(other))
            {
                return true;
            }

            if (other is SkillRequirement s)
            {
                return this.Difficulty < s.Difficulty;
            }

            return false;
        }
    }
}
