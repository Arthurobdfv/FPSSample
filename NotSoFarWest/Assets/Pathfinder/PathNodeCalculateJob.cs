using Unity.Collections;
using Unity.Jobs;

namespace Pathfinder.PFStructs
{
    public struct PathNodeCalculationJob : IJobParallelFor
    {
        public NativeArray<PathNode> Nodes;

        public NativeArray<PathfinderUnit> Units;

        public PathfinderUnit EndNode;
        public void Execute(int index)
        {
            PathNode pathNode = new PathNode();
            pathNode.unit = Units[index];
            pathNode.index = index;

            pathNode.gCost = int.MaxValue;
            pathNode.hCost = PathNode.CalculateDistanceCost(pathNode.unit.m_unit, EndNode.m_unit);
            pathNode.CalculateFCost();
            //pathNode.CameFromNodeIndex = -1;
            Nodes[index] = pathNode;
        }
    }
}