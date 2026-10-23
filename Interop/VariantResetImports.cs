using System;
using MonoMod.ModInterop;
using Celeste.Game.Content.Maps;
using Celeste.Mods.Randomizer.Generation;

namespace Celeste.Mods.Randomizer.Interop
{
    public sealed class VariantResetImports
    {
        [ModImportName("ExtendedVariantMode.ResetToDefaultSettings")]
        public Action ResetExtendedVariants;

        [ModImportName("IsaGrabBag.ResetSession")]
        public Action ResetIsaVariants;

        internal void Reset(GenerationContext context, OriginalChapterMapData map)
        {
            var mapState = context.State(map);
            if (mapState.Get<bool?>("HasExtendedVariantTriggers") ?? false) ResetExtendedVariants?.Invoke();
            if (mapState.Get<bool?>("HasIsaVariantTriggers") ?? false) ResetIsaVariants?.Invoke();
        }
    }
}
