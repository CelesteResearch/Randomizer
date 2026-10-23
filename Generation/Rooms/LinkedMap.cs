using System;
using System.Linq;
using System.Collections.Generic;
using System.Numerics;

namespace Celeste.Mods.Randomizer.Generation.Rooms
{
    public class LinkedMap
    {
        private List<LinkedRoom> Rooms = new List<LinkedRoom>();
        private LinkedRoom CachedHit;
        private int nonce;
        public float Worth { get; private set; }

        public bool AreaFree(Rectangle rect)
        {
            if (this.CachedHit != null)
            {
                if (CachedHit.Bounds.Intersects(rect))
                {
                    return false;
                }

                foreach (var r in CachedHit.ExtraBounds)
                {
                    if (r.Intersects(rect))
                    {
                        return false;
                    }
                }
            }

            foreach (var room in this.Rooms)
            {
                if (room.Bounds.Intersects(rect))
                {
                    this.CachedHit = room;
                    return false;
                }

                foreach (var r in room.ExtraBounds)
                {
                    if (r.Intersects(rect))
                    {
                        this.CachedHit = room;
                        return false;
                    }
                }
            }

            return true;
        }

        public bool AreaFree(LinkedRoom room)
        {
            if (!this.AreaFree(room.Bounds))
            {
                return false;
            }

            foreach (var r in room.ExtraBounds)
            {
                if (!this.AreaFree(r))
                {
                    return false;
                }
            }

            return true;
        }

        public bool HoleFree(LinkedRoom level, Hole hole)
        {
            Vector2 outward = hole.Side.Unit() *
                              (hole.Side == ScreenDirection.Up || hole.Side == ScreenDirection.Down ? 180 : 320);
            Vector2 pt1v = hole.LowCoord(level.Bounds) + outward;
            Vector2 pt2v = hole.HighCoord(level.Bounds) + outward;
            Point pt1 = new Point((int)pt1v.X, (int)pt1v.Y);
            Point pt2 = new Point((int)pt2v.X, (int)pt2v.Y);
            foreach (var room in this.Rooms)
            {
                if (room.Bounds.Contains(pt1.X, pt1.Y))
                {
                    return false;
                }
            }

            foreach (var room in this.Rooms)
            {
                if (room.Bounds.Contains(pt2.X, pt2.Y))
                {
                    return false;
                }
            }

            return true;
        }

        public void AddRoom(LinkedRoom room)
        {
            this.Rooms.Add(room);
            this.Worth += room.Static.Worth;
            if (room.IsBacktrack)
            {
                this.Worth += room.Static.Worth;
            }
        }

        public void RemoveRoom(LinkedRoom room)
        {
            this.Rooms.Remove(room);
            this.Worth -= room.Static.Worth;
            if (room.IsBacktrack)
            {
                this.Worth -= room.Static.Worth;
            }

            if (room == this.CachedHit)
            {
                this.CachedHit = null;
            }
        }

        private static bool isExtendedVariantsEntity(string name)
        {
            return name == "ExtendedVariantTrigger" || name.StartsWith("ExtendedVariantMode/");
        }

        public void FillMap(MapData map, RandoSettings settings, Random random)
        {
            foreach (var room in this.Rooms)
            {
                map.Levels.Add(room.Bake(this.nonce++, settings, random));
            }

            // set warp targets
            foreach (var room in this.Rooms)
            {
                foreach (var node in room.Nodes.Values)
                {
                    foreach (var edge in node.Edges)
                    {
                        var s = edge.CorrespondingEdge(node);
                        if (s.CustomWarp)
                        {
                            var baked = room.BakedRoom;
                            var dyn = settings.Context.State(baked);
                            dyn.Set<string>("CustomWarp", edge.OtherNode(node).Room.BakedRoom.Name);
                        }
                    }
                }

                if (room.WarpMap != null)
                {
                    var newMap = new Dictionary<string, string>();
                    foreach (var kv in room.WarpMap)
                    {
                        newMap[kv.Key] = kv.Value.BakedRoom.Name;
                    }

                    var dyn = settings.Context.State(room.BakedRoom);
                    dyn.Set<Dictionary<string, string>>("WarpMapping", newMap);
                }

                room.Bake2();
            }

            var hasExtendedVariantTriggers = map.Levels.Exists(levelData =>
                levelData.Triggers.Exists(entityData => isExtendedVariantsEntity(entityData.Name)) ||
                levelData.Entities.Exists(entityData => isExtendedVariantsEntity(entityData.Name)));
            var hasIsaVariantTriggers = map.Levels.Exists(levelData =>
                levelData.Triggers.Exists(entityData => entityData.Name == "ForceVariantTrigger"));
            var dyn2 = settings.Context.State(map);
            dyn2.Set<bool?>("HasExtendedVariantTriggers", hasExtendedVariantTriggers);
            dyn2.Set<bool?>("HasIsaVariantTriggers", hasIsaVariantTriggers);
        }

        public int Count
        {
            get { return this.Rooms.Count; }
        }

        public void Clear()
        {
            this.Rooms.Clear();
            this.CachedHit = null;
            this.nonce = 0;
        }
    }
}
