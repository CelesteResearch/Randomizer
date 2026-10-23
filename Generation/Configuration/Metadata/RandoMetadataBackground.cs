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
    public class RandoMetadataBackground
    {
        public string Texture;
        public string Effect;
        public int? CoverTop;
        public int? CoverBottom;
        public bool? Opaque;
        public bool? LoopX, LoopY;
        public bool? FlipX, FlipY;
        public bool? NeedsColor;
        public float? ScrollFactorX, ScrollFactorY;
        public bool? ProvidesWind;
        public float? Alpha;
        public float? SpeedX, SpeedY;
        public int? OffX, OffY;
        public string BlendMode;
        public RandoMetadataBackground AndThen;
        public List<RandoMetadataBackground> Or;

        public void Initialize()
        {
            //Texture = Texture ?? null
            //Effect = Effect ?? null
            CoverTop = CoverTop ?? 0;
            CoverBottom = CoverBottom ?? 0;
            Opaque = Opaque ?? false;
            LoopX = LoopX ?? true;
            LoopY = LoopY ?? false;
            FlipX = FlipX ?? false;
            FlipY = FlipY ?? false;
            Opaque = Opaque ?? false;
            ScrollFactorX = ScrollFactorX ?? 1f;
            ScrollFactorY = ScrollFactorY ?? 1f;
            NeedsColor = NeedsColor ?? false;
            ProvidesWind = ProvidesWind ?? false;
            Alpha = Alpha ?? 1f;
            SpeedX = SpeedX ?? 0;
            SpeedY = SpeedY ?? 0;
            OffX = OffX ?? 0;
            OffY = OffY ?? 0;
            BlendMode = BlendMode ?? "alphablend";
        }

        public void Initialize(RandoMetadataBackground bg)
        {
            bg.Initialize();
            Texture = Texture ?? bg.Texture;
            Effect = Effect ?? bg.Effect;
            CoverTop = CoverTop ?? bg.CoverTop;
            CoverBottom = CoverBottom ?? bg.CoverBottom;
            Opaque = Opaque ?? bg.Opaque;
            LoopX = LoopX ?? bg.LoopX;
            LoopY = LoopY ?? bg.LoopY;
            FlipX = FlipX ?? bg.FlipX;
            FlipY = FlipY ?? bg.FlipY;
            Opaque = Opaque ?? bg.Opaque;
            ScrollFactorX = ScrollFactorX ?? bg.ScrollFactorX;
            ScrollFactorY = ScrollFactorY ?? bg.ScrollFactorY;
            NeedsColor = NeedsColor ?? bg.NeedsColor;
            ProvidesWind = ProvidesWind ?? bg.ProvidesWind;
            Alpha = Alpha ?? bg.Alpha;
            SpeedX = SpeedX ?? bg.SpeedX;
            SpeedY = SpeedY ?? bg.SpeedY;
            OffX = OffX ?? bg.OffX;
            OffY = OffY ?? bg.OffY;
            BlendMode = BlendMode ?? bg.BlendMode;
        }
    }
}
