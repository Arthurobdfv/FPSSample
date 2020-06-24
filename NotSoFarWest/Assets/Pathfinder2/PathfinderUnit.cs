using System;
using Unity.Collections;
using UnityEngine;

namespace Pathfinder.PFStructs
{
    public struct PathfinderUnit : IEquatable<PathfinderUnit>
    {
        public Vector3 m_unit;
        public int m_size;
        public bool m_walkable;

        public int thisIndex;

        public Vector3 HalfExtent => new Vector3((float)m_size / 2, (float)m_size / 2, (float)m_size / 2);

        public PathfinderUnit(Vector3 position, bool walkable, int size, int index = -1)
        {
            m_unit = position;
            m_walkable = walkable;
            m_size = size;
            thisIndex = index;
        }

        public PathfinderUnit(PathfinderUnit unit)
        {
            m_unit = unit.m_unit;
            m_walkable = unit.m_walkable;
            m_size = unit.m_size;
            thisIndex = unit.thisIndex;
        }

        public bool Contains(Vector3 point)
        {
            if (point.x > m_unit.x + (m_size / 2) || point.x < m_unit.x - (m_size / 2)) return false;
            if (point.y > m_unit.y + (m_size / 2) || point.y < m_unit.y - (m_size / 2)) return false;
            if (point.z > m_unit.z + (m_size / 2) || point.z < m_unit.z - (m_size / 2)) return false;
            return true;
        }

        public void SetUnitIndex(int i)
        {
            thisIndex = i;
        }

        public bool Equals(PathfinderUnit other)
        {
            return (other.m_size == m_size && other.m_unit == m_unit && other.m_walkable == m_walkable);
        }
    }
}