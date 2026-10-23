using System;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Game.Content.Dialogs;
using Celeste.Game.Controls.Source;
using Celeste.Game.Execution.Processes;
using Celeste.Game.Execution.Startup;
using Celeste.Game.Settings;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Content.Gui;
using Celeste.Graphics.Rendering;
using Celeste.Graphics.Text;
using Celeste.Scene.Entities;
using Celeste.Scene.Scenes;
using Celeste.Simulation.UI.Menus.Text;

namespace Celeste.Mods.Randomizer.UI
{
    public sealed class WidgetRandoMode : SceneEntityState
    {
        private readonly ISourceEngineContext engine;
        public float VerticalSpacing, IconScale;
        public float Indent = 100f;
        public Vector2 MenuOffset = new Vector2(-400f, 200f);
        public int Index;
        public Action OnSelect, OnCancel, OnPause;
        private readonly AtlasEntry[] icons;
        private readonly string[] names;
        private readonly float[] ease = { 0f, 0f, 0f };

        public WidgetRandoMode(ISourceEngineContext engine) : base(((ISourceEntitySourceDataEnvironment)engine)
            .EntitySourceDataRegistration)
        {
            this.engine = engine;
            Tags = ((IOriginalGameTagsEnvironment)engine).Tags.HUD.Value;
            string[] modes = { "pathway", "labyrinth", "endless" };
            icons = new AtlasEntry[modes.Length];
            names = new string[modes.Length];
            for (int i = 0; i < modes.Length; i++)
            {
                icons[i] = ((ISourceGuiAtlasResources)engine).GuiAtlas["menu/" + modes[i] + "_icon"];
                names[i] = ((IOriginalGameTextEnvironment)engine).Text.Dialog.Clean("modoptions_randomizer_logic_" +
                    modes[i]);
            }
        }

        public override void Update()
        {
            base.Update();
            for (int i = 0; i < ease.Length; i++)
                ease[i] = ScalarMath.Approach(ease[i], i == Index ? 1 : 0, engine.DeltaTime * 15,
                    ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);
            var input = ((IOriginalGameInputProcessEnvironment)engine).InputSource;
            var audio = ((IOriginalGameplayProcess)engine).AudioSession;
            if (input.MenuConfirm.Pressed && OnSelect != null)
            {
                OnSelect();
                return;
            }

            if (input.MenuCancel.Pressed && OnCancel != null) OnCancel();
            else if (input.Pause.Pressed && OnPause != null) OnPause();
            else if (input.MenuUp.Pressed && !input.MenuUp.Repeating && Index != 0)
            {
                audio.Play("event:/ui/main/savefile_rollover_up");
                Index--;
            }
            else if (input.MenuDown.Pressed && !input.MenuDown.Repeating && Index != names.Length - 1)
            {
                audio.Play("event:/ui/main/savefile_rollover_down");
                Index++;
            }
        }

        public override void Render(ISourceRenderContext context)
        {
            base.Render(context);
            var font = ((ISourceActiveFontScope)engine).ActiveFont;
            font.DrawEdgeOutline(context, this,
                ((IOriginalGameTextEnvironment)engine).Text.Dialog.Get("MODOPTIONS_RANDOMIZER_LOGIC"), Position,
                new Vector2(0.5f), Vector2.One * 2, PackedColor.Gray, 4, PackedColor.DarkSlateBlue, 2,
                PackedColor.Black);
            for (int i = 0; i < icons.Length; i++)
            {
                float x = X + MenuOffset.X + ease[i] * Indent;
                float y = Y + MenuOffset.Y + i * VerticalSpacing;
                var color = PackedColor.White;
                if (i == Index)
                    color = !((ISettingsContext)engine).Settings.DisableFlashes &&
                            !((SourceSceneState)Scene).BetweenInterval(0.1f)
                        ? TextMenuPresentation.HighlightColorB
                        : TextMenuPresentation.HighlightColorA;
                SourceAtlasDrawing.DrawCentered(context, this, icons[i], new Vector2(x, y), PackedColor.White,
                    IconScale);
                font.DrawOutline(context, this, names[i], new Vector2(x + icons[i].FrameWidth * IconScale, y),
                    new Vector2(0, 0.5f), Vector2.One * 1.5f, color, 2, PackedColor.Black);
            }
        }
    }
}
