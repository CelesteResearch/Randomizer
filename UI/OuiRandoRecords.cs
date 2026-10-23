using Celeste.Mods.Randomizer;
using Celeste.Mods.Randomizer.Generation;
using Celeste.Mods.Randomizer.Generation.Configuration.Settings;
using Celeste.Mods.Randomizer.Generation.Configuration.Metadata;
using Celeste.Mods.Randomizer.Generation.Errors;
using Celeste.Mods.Randomizer.Settings;
using System;
using System.Collections.Generic;
using Celeste.Core.Scheduling;
using Celeste.Game.Content.Dialogs;
using Celeste.Game.Menus.Overworld;
using Celeste.Simulation.UI.Menus.Text;

namespace Celeste.Mods.Randomizer.UI
{
    public sealed class OuiRandoRecords : GenericOui
    {
        public OuiRandoRecords(ISourceEngineContext engine) : base(engine)
        {
        }

        protected override bool IsDeeperThan(OuiState other) => true;

        protected override Celeste.Scene.Entities.SceneEntityState ReloadMenu()
        {
            var menu = new TextMenuPresentation(Engine);
            menu.Add(new TextMenuPresentation.Header(Module.Clean("MODOPTIONS_RANDOMIZER_RECORDS_HEADER")));
            menu.Add(new TextMenuPresentation.SubHeader(Module.Clean("MODOPTIONS_RANDOMIZER_RECORDS_RANDOM")));
            AddRecords(menu, Module.SavedData.BestRandomSeedTimes);
            menu.Add(new TextMenuPresentation.SubHeader(Module.Clean("MODOPTIONS_RANDOMIZER_RECORDS_SET")));
            AddRecords(menu, Module.SavedData.BestSetSeedTimes);
            menu.OnCancel = () =>
            {
                Module.Play("event:/ui/main/button_back");
                Overworld.Goto<OuiRandoSettings>();
            };
            return menu;
        }

        private void AddRecords(TextMenuPresentation menu, Dictionary<string, RecordTuple> records)
        {
            foreach (var rules in Module.Context.Metadata.Rulesets)
            {
                if (!records.TryGetValue(rules.Name, out var record)) continue;
                string value = rules.Algorithm == LogicType.Endless
                    ? record.Item1.ToString()
                    : ((IOriginalGameTextEnvironment)Engine).Text.Time(record.Item1);
                menu.Add(new TextMenuPresentation.Button(rules.LongName + ": " + value + " (" + record.Item2 + ")")
                    .Pressed(() =>
                    {
                        Settings.Rules = rules.Name;
                        Settings.SeedType = SeedType.Custom;
                        Settings.Seed = record.Item2;
                        Settings.Enforce();
                        Overworld.Goto<OuiRandoSettings>();
                    }));
            }
        }
    }
}
