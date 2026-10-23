using System;
using System.Linq;
using System.Collections.Generic;
using System.Numerics;

namespace Celeste.Mods.Randomizer.Generation.Rooms
{
    public class LinkedRoom
    {
        protected GenerationContext Context => Static.Context;

        public Rectangle Bounds;
        public List<Rectangle> ExtraBounds;
        public StaticRoom Static;
        public Dictionary<string, LinkedNode> Nodes = new Dictionary<string, LinkedNode>();
        public Dictionary<string, LinkedRoom> WarpMap = new Dictionary<string, LinkedRoom>();
        public HashSet<int> UsedKeyholes = new HashSet<int>();
        public bool IsBacktrack;
        public LevelData BakedRoom { get; private set; }

        public Vector2 Position
        {
            get { return new Vector2(this.Bounds.Left, this.Bounds.Top); }
        }

        public override string ToString()
        {
            return $"{this.Static.Name}@{this.Position}";
        }

        public LinkedRoom(StaticRoom Room, Vector2 Position)
        {
            this.Static = Room;
            this.Bounds = new Rectangle((int)Position.X, (int)Position.Y, Room.Level.Bounds.Width,
                Room.Level.Bounds.Height);
            this.ExtraBounds = new List<Rectangle>();
            foreach (var r in Room.ExtraSpace)
            {
                this.ExtraBounds.Add(new Rectangle((int)Position.X + r.X, (int)Position.Y + r.Y, r.Width, r.Height));
            }

            foreach (var staticnode in Room.Nodes.Values)
            {
                var node = new LinkedNode()
                {
                    Static = staticnode,
                    Room = this
                };
                this.Nodes.Add(staticnode.Name, node);
            }
        }

        public virtual LevelData Bake(int? nonce, RandoSettings settings, Random random)
        {
            var result = this.Static.MakeLevelData(new Vector2(this.Bounds.Left, this.Bounds.Top), nonce);
            this.BakedRoom = result;
            bool hasCassetteBlocks = false;
            foreach (var e in result.Entities)
            {
                if (e.Name == "cassetteBlock")
                {
                    hasCassetteBlocks = true;
                    break;
                }
            }

            bool ohgodwhat = random.Next(100) == 0; // :)

            string pickCrystalColor()
            {
                tryagain:
                switch (random.Next(14))
                {
                    case 0:
                    case 1:
                    case 2:
                        return "blue";
                    case 3:
                    case 4:
                    case 5:
                        return "red";
                    case 6:
                    case 7:
                    case 8:
                        return "purple";
                    case 9:
                        return "rainbow";
                    default:
                        // dust bunnies can't be shattered, lmao
                        if (this.Static.SpinnersShatter)
                        {
                            goto tryagain;
                        }

                        return "dust";
                }
            }

            string crystalcolor = pickCrystalColor();
            string canonCrystalcolor =
                this.Static.Area.ID == 3 || this.Static.Area.ID == 7 && this.Static.Level.Name.StartsWith("d-")
                    ? "dust"
                    : this.Static.Area.ID == 5
                        ? "red"
                        : this.Static.Area.ID == 6
                            ? "purple"
                            : this.Static.Area.ID == 10
                                ? "rainbow"
                                : "blue";

            string pickSpinnerColor()
            {
                return new string[]
                {
                    "dust",
                    "spike",
                    "star"
                }[random.Next(3)];
            }

            string spinnercolor = pickSpinnerColor();
            string canonSpinnerColor = this.Static.Area.ID == 10
                ? "star"
                : this.Static.Area.ID == 3 || this.Static.Area.ID == 7 && this.Static.Level.Name.StartsWith("d-")
                    ? "dust"
                    : "spike";

            string pickSpikeColor()
            {
                tryagain:
                var result2 = new string[]
                {
                    "outline",
                    "reflection",
                    "tentacles",
                    "cliffside"
                }[random.Next(4)];
                if (hasCassetteBlocks && result2 == "tentacles")
                {
                    goto tryagain;
                }

                return result2;
            }

            string spikecolor = pickSpikeColor();
            string canonSpikecolor = Context.GetArea(this.Static.Area).Spike;
            int maxID = 0;
            var toRemove = new List<EntityData>();
            foreach (var e in result.Entities)
            {
                maxID = Math.Max(maxID, e.ID);
                switch (e.Name)
                {
                    case "spinner":
                        if (ohgodwhat)
                        {
                            crystalcolor = pickCrystalColor();
                        }

                        if (e.Values == null)
                            e.Values = new Dictionary<string, object>();
                        if (settings.RandomDecorations)
                        {
                            if (crystalcolor == "dust")
                            {
                                e.Values["dust"] = true;
                            }
                            else
                            {
                                e.Values.Remove("dust");
                                e.Values["color"] = crystalcolor;
                            }
                        }
                        else if (!e.Has("dust") && !e.Has("color"))
                        {
                            if (canonCrystalcolor == "dust")
                            {
                                e.Values["dust"] = true;
                            }
                            else
                            {
                                e.Values["color"] = canonCrystalcolor;
                            }
                        }

                        break;
                    case "trackSpinner":
                    case "rotateSpinner":
                        if (ohgodwhat)
                        {
                            spinnercolor = pickSpinnerColor();
                        }

                        if (e.Values == null)
                            e.Values = new Dictionary<string, object>();
                        // Single room can contain hundreds of the track dust (e.g. 3D of monika d-side pack). but if them is blades or stars then game will be freeze.
                        if (this.Static.ForceMovingSpinnerToDust)
                        {
                            e.Values.Remove("star");
                            e.Values["dust"] = true;
                        }
                        else if (settings.RandomDecorations)
                        {
                            if (spinnercolor == "star" || spinnercolor == "dust")
                            {
                                e.Values[spinnercolor] = true;
                            }
                            else
                            {
                                e.Values.Remove("star");
                                e.Values.Remove("dust");
                            }
                        }
                        else if (!e.Has("star") && !e.Has("dust"))
                        {
                            if (canonSpinnerColor == "star" || canonSpinnerColor == "dust")
                            {
                                e.Values[canonSpinnerColor] = true;
                            }
                            else
                            {
                                // no action
                            }
                        }

                        break;
                    case "spikesUp":
                    case "spikesDown":
                    case "spikesLeft":
                    case "spikesRight":
                        if (ohgodwhat)
                        {
                            spikecolor = pickSpikeColor();
                        }

                        if (e.Values == null)
                            e.Values = new Dictionary<string, object>();
                        if (settings.RandomDecorations)
                        {
                            bool single = ((e.Name == "spikesUp" || e.Name == "spikesDown") && e.Width == 8) ||
                                          ((e.Name == "spikesLeft" || e.Name == "spikesRight") && e.Height == 8);
                            e.Values["type"] = single && spikecolor == "tentacles" ? "outline" : spikecolor;
                        }
                        else if (e.Attr("type", "default") == "default")
                        {
                            e.Values["type"] = canonSpikecolor;
                        }

                        break;
                    case "lockBlock":
                        if (!this.UsedKeyholes.Contains(e.ID))
                        {
                            toRemove.Add(e);
                        }

                        break;
                }
            }

            foreach (var e in toRemove)
            {
                result.Entities.Remove(e);
            }

            void blockHole(Hole hole)
            {
                var topbottom = hole.Side == ScreenDirection.Up || hole.Side == ScreenDirection.Down;
                var farside = hole.Side == ScreenDirection.Down || hole.Side == ScreenDirection.Right;
                Vector2 corner;
                switch (hole.Side)
                {
                    case ScreenDirection.Up:
                        corner = new Vector2(0, -8);
                        break;
                    case ScreenDirection.Left:
                        corner = new Vector2(-8, 0);
                        break;
                    case ScreenDirection.Down:
                        corner = new Vector2(0, this.Bounds.Height);
                        break;
                    case ScreenDirection.Right:
                    default:
                        corner = new Vector2(this.Bounds.Width, 0);
                        break;
                }

                corner += hole.AlongDir.Unit() * hole.LowBound * 8;
                var e = Context.CreateEntity(new GenerationEntitySpec
                {
                    ID = ++maxID, Name = "invisibleBarrier", Width = !topbottom ? 8 : hole.Size * 8,
                    Height = topbottom ? 8 : hole.Size * 8, Position = corner, Level = result,
                });
                result.Entities.Add(e);
            }

            void partiallyBlockHole(Hole hole, Hole hole2)
            {
                int diff = hole.Compatible(hole2);
                var newHighBound = diff + hole2.LowBound - 1;
                var newHole = new Hole(hole.Side, hole.LowBound, newHighBound, false);
                blockHole(newHole);
            }

            Hole neuterHole(Hole hole1, Hole hole2)
            {
                if (hole1.Size <= hole2.Size)
                {
                    return hole1;
                }

                if ((hole1.Side == ScreenDirection.Up && hole1.Launch != null) ||
                    (hole2.Side == ScreenDirection.Up && hole2.Launch != null))
                {
                    return hole1;
                }

                var align = hole1.Compatible(hole2);
                bool alignLeft = hole1.LowBound - align == hole2.LowBound;
                if (alignLeft)
                {
                    return new Hole(hole1.Side, hole1.LowBound, hole1.LowBound + hole2.Size, false);
                }
                else
                {
                    return new Hole(hole1.Side, hole1.HighBound - hole2.Size, hole1.HighBound, false);
                }
            }

            void beamHole(Hole hole, Hole hole2)
            {
                Vector2 center;
                int rotation;
                switch (hole.Side)
                {
                    case ScreenDirection.Up:
                        center = new Vector2(0, 0);
                        rotation = 0;
                        break;
                    case ScreenDirection.Left:
                        center = new Vector2(0, 0);
                        rotation = 270;
                        break;
                    case ScreenDirection.Down:
                        center = new Vector2(0, this.Bounds.Height);
                        rotation = 180;
                        break;
                    case ScreenDirection.Right:
                    default:
                        center = new Vector2(this.Bounds.Width, 0);
                        rotation = 90;
                        break;
                }

                int offset = hole.LowBound;
                int smaller = Math.Min(hole2.Size, hole.Size);
                if (hole.Side == ScreenDirection.Left || hole.Side == ScreenDirection.Right)
                {
                    if (hole.Size > hole2.Size)
                    {
                        offset += (hole.Size - hole2.Size);
                    }
                }
                else if (hole.Side == ScreenDirection.Up || hole.Side == ScreenDirection.Down)
                {
                    int align = hole.Compatible(hole2);
                    if (hole.LowBound - align != hole2.LowBound)
                    {
                        offset = align + hole2.LowBound;
                        if (hole2.Size > hole.Size)
                        {
                            offset += (hole2.Size - hole.Size);
                        }
                    }
                }

                center += hole.AlongDir.Unit() * (offset * 8 + smaller * 4);
                var e = Context.CreateEntity(new GenerationEntitySpec
                {
                    ID = ++maxID, Name = "lightbeam", Width = smaller * 8, Height = 24, Position = center,
                    Level = result, Values = new Dictionary<string, object> { { "rotation", (object)rotation }, }
                });
                result.Entities.Add(e);
            }

            void gateTopHole(Hole hole)
            {
                if (!hole.LowOpen)
                {
                    var coord = (hole.LowBound - 1) * 8;
                    var e = Context.CreateEntity(new GenerationEntitySpec
                    {
                        Name = "invisibleBarrier", Position = new Vector2(coord, -80), Width = 8, Height = 80,
                        Level = result, ID = ++maxID,
                    });
                    result.Entities.Add(e);
                }

                if (!hole.HighOpen)
                {
                    var coord = (hole.HighBound + 1) * 8;
                    var e = Context.CreateEntity(new GenerationEntitySpec
                    {
                        Name = "invisibleBarrier", Position = new Vector2(coord, -80), Width = 8, Height = 80,
                        Level = result, ID = ++maxID,
                    });
                    result.Entities.Add(e);
                }
            }

            var unusedHorizontalHoles = new HashSet<Hole>();
            var unusedTopHoles = new HashSet<Hole>();
            var usedVerticalHoles = new List<Hole>();
            foreach (var hole in this.Static.Holes)
            {
                if (hole.Side == ScreenDirection.Left || hole.Side == ScreenDirection.Right)
                {
                    unusedHorizontalHoles.Add(hole);
                }

                if (hole.Side == ScreenDirection.Up)
                {
                    unusedTopHoles.Add(hole);
                }
            }

            foreach (var node in this.Nodes.Values)
            {
                foreach (var edge in node.Edges)
                {
                    var edgeStatic = edge.CorrespondingEdge(node);
                    var hole = edgeStatic.HoleTarget;
                    var hole2 = edge.OtherEdge(node).HoleTarget;
                    if (hole != null && hole2 != null &&
                        (hole.Side == ScreenDirection.Down || hole.Side == ScreenDirection.Up))
                    {
                        usedVerticalHoles.Add(neuterHole(hole, hole2));
                    }

                    if (hole != null && (hole.Side == ScreenDirection.Left || hole.Side == ScreenDirection.Right))
                    {
                        unusedHorizontalHoles.Remove(hole);
                    }

                    if (hole != null && hole.Side == ScreenDirection.Up)
                    {
                        unusedTopHoles.Remove(hole);
                    }

                    // Block off holes connected to edges which should not be re-entered
                    if (hole != null && hole2 != null && hole2.Kind == HoleKind.Out)
                    {
                        blockHole(hole);
                    }

                    // Block off pieces of holes which are partially unused
                    if (hole != null && hole2 != null && hole.Size > hole2.Size)
                    {
                        partiallyBlockHole(hole, hole2);
                    }

                    // Add beams
                    var shine = Context.Settings.Lights;
                    if ((shine == ShineLights.On || (shine == ShineLights.Hubs && this.Static.Hub)) && hole != null &&
                        hole2 != null && (hole.Kind == HoleKind.Out || hole.Kind == HoleKind.InOut) &&
                        (hole2.Kind == HoleKind.In || hole2.Kind == HoleKind.Unknown || hole2.Kind == HoleKind.InOut))
                    {
                        beamHole(hole, hole2);
                    }
                }

                foreach (var kv in node.Collectables)
                {
                    string name = null;
                    var col = kv.Value.Item1;
                    switch (col)
                    {
                        case LinkedCollectable.Key:
                            name = "key";
                            break;
                        case LinkedCollectable.Strawberry:
                        case LinkedCollectable.WingedStrawberry:
                            name = "strawberry";
                            break;
                        case LinkedCollectable.Gem1:
                        case LinkedCollectable.Gem2:
                        case LinkedCollectable.Gem3:
                        case LinkedCollectable.Gem4:
                        case LinkedCollectable.Gem5:
                        case LinkedCollectable.Gem6:
                            name = "summitgem";
                            break;
                        case LinkedCollectable.LifeBerry:
                            name = "randomizer/LifeBerry";
                            break;
                    }

                    var e = Context.CreateEntity(new GenerationEntitySpec
                    {
                        ID = ++maxID, Name = name, Level = result, Position = kv.Key.Position,
                        Values = new Dictionary<string, object>(),
                    });
                    if (kv.Value.Item2)
                    {
                        e.Values["AutoBubble"] = true;
                    }

                    if (col == LinkedCollectable.WingedStrawberry)
                    {
                        e.Values["winged"] = "true";
                    }

                    if (col >= LinkedCollectable.Gem1 && col <= LinkedCollectable.Gem6)
                    {
                        e.Values["gem"] = (col - LinkedCollectable.Gem1).ToString();
                    }

                    result.Entities.Add(e);
                }
            }

            //result.DisableDownTransition = false; // overridden in hook, doesn't matter
            Context.State(result).Set("UsedVerticalHoles", usedVerticalHoles);
            foreach (var hole in unusedHorizontalHoles)
            {
                blockHole(hole);
            }

            foreach (var hole in unusedTopHoles)
            {
                gateTopHole(hole);
            }

            return result;
        }

        public void Bake2()
        {
            foreach (var e in this.BakedRoom.Entities.Union(this.BakedRoom.Triggers))
            {
                string target = "";
                switch (e.Name)
                {
                    case "AcidHelper/InstantTeleportTrigger":
                    case "AcidHelper/InstantTeleporter":
                    case "ContortHelper/TeleportationTrigger":
                    case "ContortHelper/TeleportationTriggerSimple":
                    case "ContortHelper/TeleportationTriggerMinimal":
                        var attr = e.Name.StartsWith("ContortHelper/") ? "roomName" : "targetRoomId";
                        target = e.Attr(attr, "");
                        if (target != "")
                        {
                            if (!this.WarpMap.TryGetValue(target, out var targetRoom))
                            {
                                GenerationDiagnostics.Log("randomizer", "Additional info for unconfigured warp");
                                GenerationDiagnostics.Log("randomizer", $"Warp: {this.Static.Name} -> {target}");
                                GenerationDiagnostics.Log("randomizer", $"Name/ID: {e.Name} {e.ID}");
                                GenerationDiagnostics.Log("randomizer", $"Position: {e.Position}");
                                throw new GenerationError($"{this.Static.Name}: warp to {target} but no config");
                            }

                            e.Values[attr] = targetRoom.BakedRoom.Name;
                            // spawn coordinates for acidhelper warps are in map-space, so rebase them according to the target room
                            var x = e.Int("respawnPositionX", 0);
                            var y = e.Int("respawnPositionY", 0);
                            if (x != 0 && y != 0)
                            {
                                // this condition is copied from acidhelper directly
                                x -= (int)targetRoom.Static.Level.Position.X;
                                y -= (int)targetRoom.Static.Level.Position.Y;
                                x += (int)targetRoom.BakedRoom.Position.X;
                                y += (int)targetRoom.BakedRoom.Position.Y;
                                e.Values["respawnPositionX"] = x;
                                e.Values["respawnPositionY"] = y;
                            }
                        }

                        break;
                    case "AcidHelper/GradualColorGradeChangeTrigger":
                    case "ShroomHelper/GradualColorGradeChangeTrigger":
                        e.Values["speed"] = 2f;
                        break;
                    case "VivHelper/BasicInstantTeleportTrigger":
                    case "VivHelper/MainInstantTeleportTrigger":
                    case "VivHelper/CustomInstantTeleportTrigger":
                    case "VivHelper/ITPT1Way":
                        target = e.Attr("RoomName", "");
                        if (target != "")
                        {
                            if (!this.WarpMap.TryGetValue(target, out var targetRoom))
                            {
                                GenerationDiagnostics.Log("randomizer", "Additional info for unconfigured warp");
                                GenerationDiagnostics.Log("randomizer", $"Warp: {this.Static.Name} -> {target}");
                                GenerationDiagnostics.Log("randomizer", $"Name/ID: {e.Name} {e.ID}");
                                GenerationDiagnostics.Log("randomizer", $"Position: {e.Position}");
                                throw new GenerationError($"{this.Static.Name}: warp to {target} but no config");
                            }

                            e.Values["RoomName"] = targetRoom.BakedRoom.Name;
                        }
                        else
                        {
                            target = e.Attr("TargetID", null);
                            if (target == null)
                            {
                                throw new GenerationError(
                                    "${this.Static.Name}: I think a viv helper teleport is configured wrong; no RoomName or TargetID");
                            }

                            string foundIt = null;
                            foreach (var room in this.WarpMap.Values)
                            {
                                foreach (var e2 in room.BakedRoom.Entities.Union(room.BakedRoom.Triggers))
                                {
                                    if (e.Name == "VivHelper/TeleportTarget" && e.Attr("TargetID", null) == target)
                                    {
                                        foundIt = room.BakedRoom.Name;
                                        goto doublebreak;
                                    }
                                }
                            }

                            doublebreak:
                            if (foundIt == null)
                            {
                                throw new GenerationError(
                                    "${this.Static.Name}: I think a viv helper teleport is configured wrong; no matching TargetID found");
                            }

                            e.Values["RoomName"] = foundIt;
                        }

                        break;
                }
            }
        }
    }
}
