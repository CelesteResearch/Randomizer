using Celeste.Simulation.UI.Menus.Text;

namespace Celeste.Mods.Randomizer.UI
{
    public sealed class SpecialSizeSubheader : TextMenuPresentation.SubHeader
    {
        public SpecialSizeSubheader(string title) : base(title)
        {
        }

        public override float LeftWidth() => 0;
        public override float RightWidth() => 0;
    }
}
