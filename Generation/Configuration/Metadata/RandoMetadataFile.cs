using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using System.Numerics;
using YamlDotNet.Core.Tokens;

namespace Celeste.Mods.Randomizer.Generation.Configuration.Metadata
{
    public class RandoMetadataFile
    {
        public string ConfigPath;
        public string ModName;
        public List<string> CollectableNames = new List<string>();
        public List<RandoMetadataMusic> Music = new List<RandoMetadataMusic>();
        public List<RandoMetadataCampaign> Campaigns = new List<RandoMetadataCampaign>();

        public List<RandoMetadataBackground> Backgrounds = new List<RandoMetadataBackground>(),
            BgEffects = new List<RandoMetadataBackground>(),
            FgEffects = new List<RandoMetadataBackground>();

        public Dictionary<char, string> FGTiles = new Dictionary<char, string>();

        [YamlIgnore]
        public Dictionary<string, RandoMetadataRuleset> RulesetsDict = new Dictionary<string, RandoMetadataRuleset>();

        public List<RandoMetadataRuleset> Rulesets
        {
            get => new List<RandoMetadataRuleset>(this.RulesetsDict.Values);
            set
            {
                foreach (var r in value)
                {
                    if (String.IsNullOrEmpty(r.Name))
                    {
                        throw new Exception("Rulesets must have Name specified");
                    }

                    if (this.RulesetsDict.ContainsKey(r.Name))
                    {
                        throw new Exception($"Ruleset name '{r.Name}' is duplicated");
                    }

                    this.RulesetsDict[r.Name] = r;
                }
            }
        }

        public void Add(RandoMetadataFile other, GenerationContext context)
        {
            this.CollectableNames.AddRange(other.CollectableNames);
            this.Music.AddRange(other.Music.Where(t => context.AudioEventExists(t.Name)));
            this.Campaigns.AddRange(other.Campaigns);
            this.Backgrounds.AddRange(other.Backgrounds);
            this.BgEffects.AddRange(other.BgEffects);
            this.FgEffects.AddRange(other.FgEffects);
            this.Rulesets = other.Rulesets; // rely on setter behavior to use this as an updater
            if (other.FGTiles is var tiles)
            {
                foreach (var t in tiles.Keys)
                {
                    if (this.FGTiles.TryGetValue(t, out var t2))
                    {
                        GenerationDiagnostics.Log(GenerationLogLevel.Debug, "randomizer",
                            $"Registering the rando tiles '{t}' from {other.ModName} but it already registered by other mod!");
                    }
                    else
                    {
                        this.FGTiles[t] = other.FGTiles[t];
                        GenerationDiagnostics.Log(GenerationLogLevel.Debug, "randomizer",
                            $"registered the rando tiles '{t}' from {other.ModName}");
                    }
                }
            }
        }
    }
}
