

using System;
using UnityEngine;
namespace Pathfinder.PFStructs
{
    public struct PathNode : IEquatable<PathNode>
    {
        public PathfinderUnit unit;

        public int index;

        public float gCost;
        public float hCost;
        public float fCost;

        public int cameFromNodeIndex;
        public void CalculateFCost()
        {
            fCost = gCost + hCost;
        }

        public static float CalculateDistanceCost(Vector3 aPos, Vector3 bPos)
        {
            var xDistance = Math.Abs(aPos.x - bPos.x);
            var yDistance = Math.Abs(aPos.y - bPos.y);
            var zDistance = Math.Abs(aPos.z - bPos.z);
            return (xDistance + yDistance + zDistance) * 5;
        }

        public static float CalculateHCost(PathNode a, PathNode b)
        {
            return (a.unit.m_unit - b.unit.m_unit).sqrMagnitude * 1.3f;
        }

        public static float CalculateDistanceCost(PathNode a, PathNode b)
        {
            return PathNode.CalculateDistanceCost(a.unit.m_unit, b.unit.m_unit);
        }

        public void SetIndex(ref int i)
        {
            index = i;
            i++;
        }

        public bool Equals(PathNode other)
        {
            return (other.unit.Equals(unit) && (other.index == index || (other.unit.m_size == 1 && unit.m_size == 1)));
        }

        public override string ToString()
        {
            return $"Node (x,y,z): ({unit.m_unit.x},{unit.m_unit.y},{unit.m_unit.z})";
        }
    }
}