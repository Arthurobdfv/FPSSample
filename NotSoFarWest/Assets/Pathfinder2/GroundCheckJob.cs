using Pathfinder.PFStructs;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Pathfinder.PFJobs
{
    public struct GroundCheckJob : IJobParallelFor
    {
        public NativeArray<PathfinderUnit> PathfinderUnitArray;
        public int m_gridSize, m_areaSize, m_bakerSize;
        public Vector3 m_cubeExtent;
        public Vector3 m_halfCubeExtent;

        public PhysicsScene _physics;

        //public Physics _physics;

        public LayerMask m_layerMask;
        public void Execute(int index)
        {
            var data = PathfinderUnitArray[index];
            var x = index % m_gridSize;
            var z = (index / m_gridSize) % m_gridSize;
            var y = index / (m_areaSize);
            var center = new Vector3(x, y, z);

            PathfinderUnitArray[index] = new PathfinderUnit(center, false, (int)m_cubeExtent.x);
        }

        public void SetUpJob(NativeArray<PathfinderUnit> pathUnitsArray, int gridSize, Vector3 cubeSize, LayerMask m_groundLayers, PhysicsScene _scene)
        {
            PathfinderUnitArray = pathUnitsArray;
            m_areaSize = gridSize * gridSize;
            m_gridSize = gridSize;
            m_cubeExtent = cubeSize;
            m_halfCubeExtent = cubeSize / 2;
            m_layerMask = m_groundLayers;
            _physics = new PhysicsScene();
        }
    }
}