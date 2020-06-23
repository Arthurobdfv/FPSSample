using Pathfinder.Extensions;
using Pathfinder.PFStructs;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Pathfinder.PFJobs
{
    public struct FindUnitNodeFromPosJob : IJobParallelFor
    {

        [ReadOnly]
        public NativeArray<PathNode> NodesToSearch;

        [WriteOnly]
        public PathNode UnitFound;

        public Vector3 Pos;

        public void Execute(int index)
        {
            var searchingNode = NodesToSearch[index];
            var searchingUnit = searchingNode.unit.m_size > 1 ? searchingNode.unit.GetClosestUnitary(Pos) : searchingNode.unit;
            if (searchingNode.unit.Contains(Pos)) UnitFound = searchingNode;
        }
    }

    public struct FindUnitaryUnitFromPosJob : IJobParallelFor
    {

        [ReadOnly]
        public NativeArray<PathfinderUnit> UnitsToSearch;

        [NativeDisableParallelForRestriction]
        public NativeArray<PathfinderUnit> UnitFound;

        public int Index;


        public Vector3 Pos;

        public void Execute(int index)
        {
            var searchingNode = UnitsToSearch[index];
            if (!searchingNode.Contains(Pos)) return;
            var searchingUnit = searchingNode.m_size > 1 ? searchingNode.GetClosestUnitary(Pos) : searchingNode;
            if (searchingNode.Contains(Pos))
            {
                UnitFound[0] = searchingUnit;
                Index = index;
            }
        }
    }


}