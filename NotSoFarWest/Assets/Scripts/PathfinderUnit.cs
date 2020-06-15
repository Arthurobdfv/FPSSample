using UnityEngine;

public struct PathfinderUnit {
    public Vector3 m_unit;
    public int m_size;
    public bool m_walkable;

    public PathfinderUnit(Vector3 position, bool walkable,int size){
        m_unit = position;
        m_walkable = walkable;
        m_size = size;
    }

    public bool Contains(Vector3 point){
        if(point.x > m_unit.x+(m_size/2) || point.x < m_unit.x-(m_size/2)) return false;
        if(point.y > m_unit.y+(m_size/2) || point.y < m_unit.y-(m_size/2)) return false;
        if(point.z > m_unit.z+(m_size/2) || point.z < m_unit.z-(m_size/2)) return false;
        return true;
    }
}