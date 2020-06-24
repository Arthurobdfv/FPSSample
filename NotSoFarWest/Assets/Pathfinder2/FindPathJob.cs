using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Pathfinder.PFStructs;
using Pathfinder.Extensions;
using UnityEngine;
using System.Linq;

namespace Pathfinder.PFJobs
{

    [BurstCompile]
    public struct FindPathJob : IJob
    {
        NativeList<PathNode> Allnodes;
        [NativeDisableParallelForRestriction] NativeList<PathNode> OpenList;
        [NativeDisableParallelForRestriction] NativeList<PathNode> ClosedList;

        NativeArray<PathfinderUnit> GridUnits;

        PathfinderUnit startUnit;
        PathfinderUnit endUnit;

        public NativeArray<PathfinderUnit> Path;

        int GridSize;
        Vector3 GridPoint;

        public void SetUpPathJob(PathfinderUnit StartUnit, PathfinderUnit EndUnit, NativeArray<PathfinderUnit> AllUnits, int _gridSize, Vector3 _gridPos)
        {
            GridUnits = AllUnits;
            startUnit = StartUnit;
            endUnit = EndUnit;
            GridSize = _gridSize;
            GridPoint = _gridPos;
        }

        public void Execute()
        {
            int index = 0;
            PathNode startNode = new PathNode();
            startNode.unit = startUnit;
            PathNode endNode = new PathNode();
            endNode.unit = endUnit;
            startNode.gCost = 0f;
            startNode.SetIndex(ref index);
            //endNode.SetIndex(ref index);
            startNode.hCost = PathNode.CalculateHCost(startNode, endNode);
            startNode.CalculateFCost();

            NativeList<PathNode> openList = new NativeList<PathNode>(Allocator.Persistent);
            NativeList<PathNode> closedList = new NativeList<PathNode>(Allocator.Persistent);
            var allNodes = new NativeList<PathNode>(Allocator.Persistent);

            openList.Add(startNode);
            allNodes.Add(startNode);
            int loopcount = 0;
            while (openList.Length > 0 && loopcount < 1000000)
            {
                var currentNode = openList.GetLowestFNode();

                if (currentNode.Equals(endNode))
                {
                    endNode = currentNode;
                    break;
                }

                for (int i = 0; i < openList.Length; i++)
                {
                    if (currentNode.Equals(openList[i]))
                    {
                        openList.RemoveAtSwapBack(i);
                        break;
                    }
                }

                closedList.Add(currentNode);
                var neighs = GetNeighbours(currentNode, GridUnits, allNodes);
                neighs = neighs.Filter((n) => !closedList.SearchFor(n) && n.unit.m_walkable);

                for (int i = 0; i < neighs.Length; i++)
                {
                    var neighbour = neighs[i];
                    var tentativegCost = currentNode.gCost + PathNode.CalculateDistanceCost(currentNode, neighbour);
                    if (tentativegCost < neighbour.gCost)
                    {
                        if (neighbour.index <= 0)
                        {
                            neighbour.SetIndex(ref index);
                            allNodes.Add(neighbour);
                        }
                        neighbour.cameFromNodeIndex = currentNode.index;
                        neighbour.gCost = tentativegCost;
                        neighbour.hCost = PathNode.CalculateHCost(neighbour, endNode);
                        neighbour.CalculateFCost();
                        allNodes[neighbour.index] = neighbour;
                        var found = openList.SearchFor(neighbour);
                        if (!found) openList.Add(neighbour);
                    }

                }
                loopcount++;
                // testOpen = openList.ToArray().Select((n) => n.unit).ToArray();
                // testClosed = closedList.ToArray().Select((n) => n.unit).ToArray();


                // yield return new WaitForFixedUpdate();
            }

            if (endNode.cameFromNodeIndex <= 0)
            {
                Debug.LogError("Couldn't find path");
            }
            else
            {
                var path = CalculatePath(allNodes, endNode);
                var size = path.Length;
                Path = path.Select2((pn) => pn.unit);
                for (int i = 0; i < path.Length; i++)
                {
                    Debug.Log(path[i]);
                    allNodes.RemoveAt(path[i].index);
                }
                path.Dispose();
            }
            allNodes.Dispose();
            openList.Dispose();
            closedList.Dispose();
            GridUnits.Dispose();
        }


        public PathNode[] GetNeighbours(PathNode me, NativeArray<PathfinderUnit> gridUnits, NativeList<PathNode> allNodes)
        {
            var Directions = new Vector3[] {
            new Vector3(1,0,0) + me.unit.m_unit,
            new Vector3(0,1,0) + me.unit.m_unit,
            new Vector3(0,0,1) + me.unit.m_unit,
            new Vector3(-1,0,0) + me.unit.m_unit,
            new Vector3(0,-1,0) + me.unit.m_unit,
            new Vector3(0,0,-1) +me.unit.m_unit,
            };
            Vector3 _gp = GridPoint;
            int _gs = GridSize;
            var insideBoundariesDirections = Directions.Where((dir) => FindPathJob.IsInsideBoundaries(dir, _gp, _gs)).ToList();
            var neighbours = new PathNode[insideBoundariesDirections.Count()];
            for (int i = 0; i < neighbours.Length; i++)
            {
                var unit = FindUnitFromPosSequentional(gridUnits, insideBoundariesDirections[i]);

                bool exists = false;
                var newNode = new PathNode();
                newNode.gCost = float.MaxValue;
                for (int x = 0; x < allNodes.Length; x++)
                {
                    if (allNodes[x].unit.Equals(unit))
                    {
                        exists = true;
                        neighbours[i] = allNodes[x];
                    }
                }
                if (!exists)
                {
                    newNode.unit = unit;
                    neighbours[i] = newNode;
                }
            }

            return neighbours;
        }

        private PathfinderUnit FindUnitFromPosSequentional(NativeArray<PathfinderUnit> pathNodeArray, Vector3 pos)
        {
            var UnitFound = new PathfinderUnit();
            for (int i = 0; i < pathNodeArray.Length; i++)
            {
                var searchingNode = pathNodeArray[i];
                if (!searchingNode.Contains(pos)) continue;
                var searchingUnit = searchingNode.m_size > 1 ? searchingNode.GetClosestUnitary(pos) : searchingNode;
                if (searchingNode.Contains(pos))
                {
                    UnitFound = searchingUnit;
                }
            }
            return UnitFound;
        }


        static bool IsInsideBoundaries(Vector3 point, Vector3 gridPoint, int gridsize)
        {
            float gridHalf = (float)gridsize / 2;
            if (point.z > gridHalf + gridPoint.z || point.z < gridPoint.z - gridHalf) return false;
            if (point.y > gridHalf + gridPoint.y || point.y < gridPoint.y - gridHalf) return false;
            if (point.x > gridHalf + gridPoint.x || point.x < gridPoint.x - gridHalf) return false;
            return true;
        }

        private NativeList<PathNode> CalculatePath(NativeList<PathNode> pathNodeArray, PathNode endNode)
        {
            if (endNode.cameFromNodeIndex <= 0)
            {
                return new NativeList<PathNode>(Allocator.Temp);
            }
            else
            {
                var path = new NativeList<PathNode>(Allocator.Temp);
                path.Add(endNode);
                PathNode currentNode = endNode;
                var ordered = pathNodeArray.OrderBy((node) => node.index);
                int loopcount = 0;
                while (currentNode.cameFromNodeIndex > 0 && loopcount < 100000)
                {
                    PathNode cameFromNode = pathNodeArray[currentNode.cameFromNodeIndex];
                    path.Add(cameFromNode);
                    currentNode = cameFromNode;
                    loopcount++;
                }
                if (loopcount >= 100000) Debug.LogError("Loopcount exceeded on calculatePath");
                return path;
            }
        }


    }

}