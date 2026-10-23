using Celeste.Core.Scheduling;
using Celeste.Simulation.UI.Menus.Text;

namespace Celeste.Mods.Randomizer.UI
{
    public sealed class DisablableTextMenu : TextMenuPresentation
    {
        public bool DisableMovement;

        public DisablableTextMenu(ISourceEngineContext engine) : base(engine)
        {
        }
    }
}
