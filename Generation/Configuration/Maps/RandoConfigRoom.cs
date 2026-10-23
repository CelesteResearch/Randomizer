using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using System.Numerics;
using YamlDotNet.Core.Tokens;

namespace Celeste.Mods.Randomizer.Generation.Configuration.Maps
{
    public class RandoConfigRoom
    {
        public string Room;

        public List<RandoConfigCollectable> Collectables
        {
            get => _Collectables;
            set
            {
                foreach (RandoConfigCollectable col in value)
                {
                    if (col.ReqIn != null || col.ReqOut != null)
                    {
                        InternalEdges.Add(new RandoConfigInternalEdge()
                            { Collectable = col.Idx, MustFly = col.MustFly, ReqIn = col.ReqIn, ReqOut = col.ReqOut, });
                    }
                    else
                    {
                        _Collectables.Add(col);
                    }
                }
            }
        }

        private List<RandoConfigCollectable> _Collectables = new List<RandoConfigCollectable>();
        public List<RandoConfigHole> Holes { get; set; } = new List<RandoConfigHole>();

        public List<RandoConfigHolesOfSide> HoldsOfSide
        {
            get => null;
            set
            {
                foreach (RandoConfigHolesOfSide side in value)
                {
                    foreach (var hold in side.Holes)
                    {
                        hold.Side = side.Side;
                        Holes.Add(hold);
                    }
                }
            }
        }

        public List<RandoConfigRoom> Subrooms { get; set; }
        public List<RandoConfigInternalEdge> InternalEdges { get; set; } = new List<RandoConfigInternalEdge>();

        public bool End
        {
            get => this.ReqEnd != null;
            set => this.ReqEnd = value ? new RandoConfigReq() : null;
        }

        public RandoConfigReq ReqEnd { get; set; }
        public bool Hub { get; set; }
        public List<RandoConfigEdit> Tweaks { get; set; }
        public RandoConfigCoreMode Core { get; set; }
        public List<RandoConfigRectangle> ExtraSpace { get; set; }

        public float? Worth;
        public bool SpinnersShatter;
        public bool ForceMovingSpinnerToDust;
        public List<string> Flags;
    }
}
