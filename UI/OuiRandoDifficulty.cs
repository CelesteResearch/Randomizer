using Celeste.Mods.Randomizer;
using Celeste.Mods.Randomizer.Generation;
using Celeste.Mods.Randomizer.Generation.Configuration.Settings;
using Celeste.Mods.Randomizer.Generation.Configuration.Metadata;
using Celeste.Mods.Randomizer.Generation.Errors;
using Celeste.Mods.Randomizer.Settings;
using Celeste.Core.Scheduling;
using Celeste.Game.Menus.Overworld;
using Celeste.Game.Menus.Title;
using Celeste.Simulation.UI.Menus.Text;
using Celeste.Everest.UI.Settings;

namespace Celeste.Mods.Randomizer.UI
{
    public sealed class OuiRandoDifficulty : GenericOui
    {
        public OuiRandoDifficulty(ISourceEngineContext engine) : base(engine)
        {
        }

        protected override Celeste.Scene.Entities.SceneEntityState ReloadMenu()
        {
            var menu = new TextMenuPresentation(Engine);
            menu.Add(new TextMenuPresentation.Header(Module.Clean("MODOPTIONS_RANDOMIZER_DIFFICULTY")));
            var explanation = new SpecialSizeSubheader("Filler");
            string[] icons = { "strawberry", "skullblue", "skullred", "skullgold", "skullorange", "skullpurple" };
            for (int i = 0; i < (int)Difficulty.Last; i++)
            {
                var difficulty = (Difficulty)i;
                string suffix = difficulty.ToString().ToUpperInvariant();
                var button = new EverestMenuButtonExt(Module.Clean("MODOPTIONS_RANDOMIZER_DIFFICULTY_" + suffix),
                    "menu/skulls/" + icons[i]);
                button.OnEnter = () =>
                    explanation.Title = Module.Clean("MODOPTIONS_RANDOMIZER_DIFFICULTY_EXPLAIN_" + suffix);
                button.Pressed(() =>
                {
                    if (Settings.Difficulty != difficulty) Settings.Rules = "";
                    Settings.Difficulty = difficulty;
                    Module.Play("event:/ui/main/button_climb");
                    Overworld.Goto<OuiRandoSettings>();
                });
                menu.Add(button);
            }

            menu.Add(explanation);
            menu.Selection = (int)Settings.Difficulty + 1;
            menu.Current.OnEnter?.Invoke();
            menu.OnCancel = () =>
            {
                Module.Play("event:/ui/main/button_back");
                Overworld.Goto<OuiRandoMode>();
            };
            menu.OnPause = () =>
            {
                Module.Play("event:/ui/main/button_select");
                Overworld.Goto<MainMenuSequence>();
            };
            return menu;
        }

        protected override bool IsDeeperThan(OuiState other) => other is OuiRandoMode || !(other is GenericOui);
    }
}
