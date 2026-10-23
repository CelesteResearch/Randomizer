using Celeste.Core.Scheduling;
using Celeste.Game.Menus.Title;
using Celeste.Scene.Entities;
using Celeste.Mods.Randomizer.Generation.Configuration.Settings;

namespace Celeste.Mods.Randomizer.UI
{
    public sealed class OuiRandoMode : GenericOui
    {
        public OuiRandoMode(ISourceEngineContext engine) : base(engine)
        {
        }

        protected override SceneEntityState ReloadMenu()
        {
            var menu = new WidgetRandoMode(Engine)
                { VerticalSpacing = 200f, IconScale = 0.5f, Y = 200f, Index = (int)Settings.Algorithm };
            menu.OnCancel = () =>
            {
                Module.Play("event:/ui/main/button_back");
                Overworld.Goto<MainMenuSequence>();
            };
            menu.OnSelect = () =>
            {
                var algorithm = (LogicType)menu.Index;
                if (algorithm != Settings.Algorithm) Settings.Rules = "";
                Settings.Algorithm = algorithm;
                Module.Play("event:/ui/main/button_climb");
                Overworld.Goto<OuiRandoDifficulty>();
            };
            menu.OnPause = () =>
            {
                Module.Play("event:/ui/main/button_select");
                Overworld.Goto<MainMenuSequence>();
            };
            return menu;
        }
    }
}
