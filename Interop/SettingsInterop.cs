using System;
using System.Collections.Generic;
using Celeste.Game.Execution.Areas;
using Celeste.Mods.Randomizer.Generation;
using Celeste.Mods.Randomizer.Generation.Configuration.Settings;
using MonoMod.ModInterop;
using Celeste.Core.Reflection.Runtime;

namespace Celeste.Mods.Randomizer.Interop
{
    [RuntimeTypeAlias("Celeste.Mod.Randomizer.Interoperability.SettingsInterop")]
    [ModExportName("Randomizer.SettingsInterop")]
    public sealed class SettingsInterop
    {
        private readonly Func<GenerationContext> context;

        public SettingsInterop(Func<GenerationContext> context) => this.context = context;

        public object GetSettingsObject() => new RandoSettings { Context = context() };

        public void EnableMap(object settings, OriginalAreaKey area) =>
            InteropHelper.Settings(settings).EnableMap(area);

        public void EnableMaps(object settings, IEnumerable<OriginalAreaKey> areas)
        {
            var actual = InteropHelper.Settings(settings);
            if (areas == null) throw new ArgumentNullException(nameof(areas));
            foreach (var area in areas) actual.EnableMap(area);
        }

        public void EnableVanillaMaps(object settings)
        {
            var actual = InteropHelper.Settings(settings);
            for (int id = 0; id <= 10; id++)
                if (id != 8)
                    actual.EnableMap(new OriginalAreaKey { ID = id, Mode = 0 });
            for (int mode = 1; mode <= 2; mode++)
            for (int id = 1; id <= 9; id++)
                if (id != 8)
                    actual.EnableMap(new OriginalAreaKey { ID = id, Mode = mode });
        }

        public void SetSeed(object settings, string seed) => InteropHelper.Settings(settings).Seed = seed;
        public void SetRules(object settings, string rules) => InteropHelper.Settings(settings).Rules = rules;

        public bool SetSeedType(object settings, string value) =>
            Enum.TryParse(value, out InteropHelper.Settings(settings).SeedType);

        public bool SetAlgorithm(object settings, string value) =>
            Enum.TryParse(value, out InteropHelper.Settings(settings).Algorithm);

        public bool SetDashes(object settings, string value) =>
            Enum.TryParse(value, out InteropHelper.Settings(settings).Dashes);

        public bool SetDifficulty(object settings, string value) =>
            Enum.TryParse(value, out InteropHelper.Settings(settings).Difficulty);

        public bool SetDifficultyEagerness(object settings, string value) =>
            Enum.TryParse(value, out InteropHelper.Settings(settings).DifficultyEagerness);

        public bool SetLength(object settings, string value) =>
            Enum.TryParse(value, out InteropHelper.Settings(settings).Length);

        public bool SetLights(object settings, string value) =>
            Enum.TryParse(value, out InteropHelper.Settings(settings).Lights);

        public bool SetDarkness(object settings, string value) =>
            Enum.TryParse(value, out InteropHelper.Settings(settings).Darkness);

        public bool SetStrawberries(object settings, string value) =>
            Enum.TryParse(value, out InteropHelper.Settings(settings).Strawberries);
    }
}
