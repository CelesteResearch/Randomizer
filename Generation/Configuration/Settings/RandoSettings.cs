using System;
using System.Linq;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace Celeste.Mods.Randomizer.Generation.Configuration.Settings
{
    public class RandoSettings
    {
        [YamlIgnore] public GenerationContext Context;

        public RandoSettings Copy()
        {
            var copy = (RandoSettings)MemberwiseClone();
            copy.IncludedMaps = new HashSet<AreaKeyNotStupid>(IncludedMaps);
            return copy;
        }

        public SeedType SeedType;
        public string Seed = "achene";
        public string Rules = "";

        [YamlIgnore]
        public RandoMetadataRuleset Ruleset
        {
            get
            {
                if (Context.Metadata.RulesetsDict.TryGetValue(this.Rules, out var result))
                {
                    return result;
                }

                this.Rules = "";
                return null;
            }
        }

        public bool RepeatRooms;
        public bool EnterUnknown;
        public bool SpawnGolden;
        public bool Variants = true;
        public bool RandomDecorations = true;
        public bool RandomColors = true;
        public bool RandomBackgrounds = true;
        public int EndlessLives = 3;
        public LogicType Algorithm;
        public MapLength Length;
        public NumDashes Dashes = NumDashes.One;
        public Difficulty Difficulty;
        public DifficultyEagerness DifficultyEagerness = DifficultyEagerness.Low;
        public ShineLights Lights = ShineLights.Hubs;
        public Darkness Darkness = Darkness.Vanilla;
        public StrawberryDensity Strawberries = StrawberryDensity.Low;
        [YamlIgnore] public HashSet<AreaKeyNotStupid> IncludedMaps = new HashSet<AreaKeyNotStupid>();
        [YamlIgnore] public int EndlessLevel;

        [YamlIgnore]
        public bool IsLabyrinth => this.Algorithm == LogicType.Labyrinth ||
                                   (this.Algorithm == LogicType.Endless && this.EndlessLevel % 5 == 4);

        [YamlIgnore] public bool HasLives => this.Algorithm == LogicType.Endless && this.EndlessLives != 0;

        public List<AreaKeyNotStupid> IncludedMapsList
        {
            get => new List<AreaKeyNotStupid>(this.IncludedMaps);
            set => this.IncludedMaps = new HashSet<AreaKeyNotStupid>(value);
        }

        public void PruneMaps()
        {
            this.IncludedMaps.RemoveWhere(a => Context.GetArea(a.SID) == null);
        }

        public void Enforce()
        {
            if (this.SeedType == SeedType.Random)
            {
                this.Seed = "";
                var ra = new Random();
                for (int i = 0; i < 6; i++)
                {
                    var val = ra.Next(36);
                    if (val < 10)
                    {
                        this.Seed += ((char)('0' + val)).ToString();
                    }
                    else
                    {
                        this.Seed += ((char)('a' + val - 10)).ToString();
                    }
                }
            }

            var r = this.Ruleset;
            if (r != null)
            {
                if (r.EnabledMaps == null)
                {
                    this.SetNormalMaps();
                }
                else
                {
                    this.IncludedMaps = new HashSet<AreaKeyNotStupid>(r.EnabledMaps);
                }

                this.RepeatRooms = r.RepeatRooms;
                this.EnterUnknown = r.EnterUnknown;
                this.Variants = r.Variants;
                this.Algorithm = r.Algorithm;
                this.Length = r.Length;
                this.Dashes = r.Dashes;
                this.Difficulty = r.Difficulty;
                this.DifficultyEagerness = r.DifficultyEagerness;
                this.Lights = r.Lights;
                this.Darkness = r.Darkness;
                this.Strawberries = r.Strawberries;
                this.EndlessLives = r.Lives;
            }
        }

        public void SetNormalMaps()
        {
            this.DisableAllMaps();
            foreach (var key in Context.LevelSets["Celeste"])
            {
                this.EnableMap(key);
            }
        }

        private IEnumerable<uint> HashParts()
        {
            yield return (uint)Context.Version.Major;
            yield return (uint)Context.Version.Minor;
            yield return (uint)Context.Version.Build;
            yield return this.IntSeed;
            yield return RepeatRooms ? 1u : 0u;
            yield return EnterUnknown ? 1u : 0u;
            yield return (uint)Algorithm;
            yield return (uint)Length;
            yield return (uint)Dashes;
            yield return (uint)Difficulty;
            yield return (uint)DifficultyEagerness;
            yield return (uint)Lights;
            yield return (uint)Darkness;
            var sortedMaps = new List<AreaKeyNotStupid>(IncludedMaps.Where(key => Context.GetArea(key.SID) != null));
            sortedMaps.Sort((AreaKeyNotStupid x, AreaKeyNotStupid y) =>
            {
                var xs = x.SID;
                var ys = y.SID;
                var cmp1 = xs.CompareTo(ys);
                if (cmp1 != 0)
                {
                    return cmp1;
                }

                if (x.Mode < y.Mode)
                {
                    return -1;
                }

                if (x.Mode > y.Mode)
                {
                    return 1;
                }

                return 0;
            });
            foreach (var thing in sortedMaps)
            {
                yield return (uint)thing.Mode;
                foreach (var ch in thing.SID)
                {
                    yield return (uint)ch;
                }

                yield return 0u;
            }
        }

        [YamlIgnore]
        public string Hash
        {
            get => djb2(this.HashParts()).ToString();
        }

        [YamlIgnore]
        public uint IntSeed
        {
            get
            {
                var euniSeed = this.Seed.Length <= 10;
                foreach (var i in this.Seed)
                {
                    if (!Char.IsDigit(i))
                    {
                        euniSeed = false;
                        break;
                    }
                }

                if (euniSeed)
                {
                    var big = ulong.Parse(this.Seed);
                    if (big <= (ulong)uint.MaxValue)
                    {
                        return (uint)big;
                    }
                }

                return djb2(this.Seed);
            }
        }

        public static uint djb2(IEnumerable<uint> parts)
        {
            uint h = 5381;
            foreach (var i in parts)
            {
                h = ((h << 5) + h) + i;
            }

            return h;
        }

        public static uint djb2(string parts)
        {
            return djb2(parts.Select(c => (uint)c));
        }

        [YamlIgnore]
        public int LevelCount
        {
            get
            {
                int sum = 0;
                foreach (var room in Context.AllRooms)
                {
                    if (this.MapIncluded(room.Area))
                    {
                        sum++;
                    }
                }

                return sum;
            }
        }

        public struct AreaKeyNotStupid
        {
            public string SID;
            public int Mode;

            public AreaKeyNotStupid(GenerationContext context, AreaKey Stupid)
            {
                this.SID = context.GetArea(Stupid).SID;
                this.Mode = Stupid.Mode;
            }
        }

        public bool MapIncluded(AreaKey map)
        {
            return this.IncludedMaps.Contains(new AreaKeyNotStupid(Context, map));
        }

        public void EnableMap(AreaKey map)
        {
            this.IncludedMaps.Add(new AreaKeyNotStupid(Context, map));
        }

        public void DisableMap(AreaKey map)
        {
            this.IncludedMaps.Remove(new AreaKeyNotStupid(Context, map));
        }

        public void DisableAllMaps()
        {
            this.IncludedMaps.Clear();
        }

        [YamlIgnore]
        public IEnumerable<AreaKey> EnabledMaps
        {
            get
            {
                var result = new List<AreaKey>();
                foreach (var area in this.IncludedMaps)
                {
                    result.Add(Context.Key(Context.GetArea(area.SID), area.Mode));
                }

                return result;
            }
        }
    }
}
