using System;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Everest.Content.Dialogs;
using Celeste.Everest.UI.Controls;
using Celeste.Everest.UI.Settings;
using Celeste.Everest.Modules.Updating;
using Celeste.Mods.Randomizer.Generation.Configuration.Settings;
using Celeste.Simulation.UI.Menus.Text;

namespace Celeste.Mods.Randomizer.UI
{
    public sealed partial class OuiRandoSettings
    {
        private void BuildMaps()
        {
            var items = pages[2];
            items.Add(
                new TextMenuPresentation.Button(Module.Clean("MODOPTIONS_RANDOMIZER_MAPPICKER_TOGGLEALL")).Pressed(() =>
                {
                    TextMenuPresentation.OnOff first = null;
                    foreach (var item in items)
                        if (item is TextMenuPresentation.OnOff toggle)
                        {
                            first = toggle;
                            break;
                        }

                    if (first == null) return;
                    int value = 1 - first.Index;
                    foreach (var item in items)
                        if (item is TextMenuPresentation.OnOff toggle)
                        {
                            toggle.Index = value;
                            toggle.OnValueChange(toggle.Values[value].Item2);
                        }
                }));
            var total = Counts(Settings.LevelCount);
            items.Add(total);
            synchronizers.Add(() =>
                total.Title = Settings.LevelCount + " " + Module.Clean("MODOPTIONS_RANDOMIZER_MAPPICKER_LEVELS"));

            void AddToggle(string name, List<AreaKey> keys)
            {
                var toggle = new TextMenuPresentation.OnOff(name, false, Engine);
                int rooms = 0;
                foreach (var key in keys)
                    rooms += Module.Context.LevelCount[new RandoSettings.AreaKeyNotStupid(Module.Context, key)];
                toggle.Change(on =>
                {
                    foreach (var key in keys)
                        if (on) Settings.EnableMap(key);
                        else Settings.DisableMap(key);
                    Synchronize();
                });
                items.Add(toggle);
                items.Add(Counts(rooms));
                synchronizers.Add(() => toggle.Index = Settings.MapIncluded(keys[0]) ? 1 : 0);
            }

            void AddSet(string id)
            {
                var keys = Module.Context.LevelSets[id];
                items.Add(new TextMenuPresentation.SubHeader(((IEverestDialogLevelSetEnvironment)Engine).DialogLevelSets
                    .CleanLevelSet(id)));
                string[] suffixes = { "A", "B", "C" };
                for (int i = 0; i < suffixes.Length; i++)
                    suffixes[i] = EverestModuleDialogText.CleanOrNull(Engine, id + "_rando_album_" + suffixes[i]) ??
                                  suffixes[i];
                foreach (var key in keys)
                {
                    var area = Module.Context.GetArea(key);
                    string suffix = key.Mode != 0 || (area.Mode.Length != 1 && area.Mode[1] != null)
                        ? suffixes[key.Mode]
                        : null;
                    string name = EverestModuleDialogText.CleanOrNull(Engine, area.Name) ??
                                  EverestStringSource.SpacedPascalCase(area.Name);
                    AddToggle(name + " " + suffix, new List<AreaKey> { key });
                }
            }

            AddSet("Celeste");
            var completed = new List<string> { "Celeste" };
            foreach (var campaign in Module.Context.Metadata.Campaigns)
            {
                items.Add(new TextMenuPresentation.SubHeader(
                    ((IEverestDialogLevelSetEnvironment)Engine).DialogLevelSets.CleanLevelSet(campaign.Name)));
                foreach (var levelSet in campaign.LevelSets)
                    if (Module.Context.LevelSets.TryGetValue(levelSet.ID, out var keys))
                    {
                        AddToggle(levelSet.Name, keys);
                        completed.Add(levelSet.ID);
                    }
            }

            foreach (string id in Module.Context.LevelSets.Keys)
                if (!completed.Contains(id))
                    AddSet(id);
            if (Module.Context.LevelSets.Count > 1)
                items.Insert(2,
                    new TextMenuPresentation.Button(Module.Clean("MODOPTIONS_RANDOMIZER_MAPPICKER_RESET")).Pressed(() =>
                    {
                        Settings.SetNormalMaps();
                        int set = -1;
                        foreach (var item in items)
                            if (item is TextMenuPresentation.SubHeader && !(item is EverestMenuSubHeaderExt)) set++;
                            else if (item is TextMenuPresentation.OnOff toggle) toggle.Index = set == 0 ? 1 : 0;
                    }));
            synchronizers.Add(() =>
            {
                foreach (var item in items) item.Disabled = Locked;
            });
        }

        private EverestMenuSubHeaderExt Counts(int count) =>
            new EverestMenuSubHeaderExt(count + " " + Module.Clean("MODOPTIONS_RANDOMIZER_MAPPICKER_LEVELS"))
            {
                HeightExtra = -10f,
                Offset = new Vector2(30, -5)
            };
    }
}
