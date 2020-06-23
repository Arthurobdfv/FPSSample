using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Pathfinder.Extensions;
using Pathfinder.PFJobs;
using Pathfinder.PFStructs;
using Unity.Collections;
using Unity.Jobs;
using UnityEditor;
using UnityEngine;

namespace Pathfinder.MonoBehaviours
{

    public class PathfinderBaker : MonoBehaviour
    {
        //public int m_simulatedCubeSize;
        public int gridSize;
        public List<PathfinderUnit> m_gridPoints;
        public List<PathfinderUnit> Units = new List<PathfinderUnit>();

        public PathfinderUnit[] TestPath;

        public bool m_showWalkable;
        public bool m_showNonWalkable;

        int m_bakersize;

        public LayerMask m_groundLayers;


        public PathfinderUnit[] testOpen;
        public PathfinderUnit[] testClosed;

        void Start()
        {
            Teste();
            StartCoroutine(FindPathCoroutine(new Vector3(10f, 10f, 10f), new Vector3(10f, 10f, -20f)));
        }

        void Teste()
        {
            var t1 = DateTime.Now;
            var m_checkedGridUnits = new HashSet<PathfinderUnit>();
            var chunkLevel = gridSize;
            var Chunks = new List<PathfinderUnit>();
            var chunkExtent = new Vector3(chunkLevel, chunkLevel, chunkLevel);
            var bigChunk = new PathfinderUnit(transform.position, CheckChunk(transform.position, chunkExtent, m_groundLayers), chunkLevel);
            int loopcount = 0;
            var maxLoop = (gridSize);
            Debug.Log($"Maxloop = {maxLoop}");
            Chunks.Add(bigChunk);
            while (Chunks.Count > 0 && loopcount < maxLoop)
            {
                int splitInto = 1;
                var chunk = Chunks[0];
                chunkLevel = chunk.m_size;


                if (chunkLevel == 1)
                {
                    m_checkedGridUnits.Add(chunk);
                    continue;
                }

                for (int i = 2; i <= chunkLevel; i++)
                {
                    if (chunkLevel % i == 0)
                    {
                        chunkLevel = chunkLevel / i;
                        splitInto = i;
                        break;
                    }
                }

                int numberOfUnitsToSplit = Chunks.Count;
                int numberOfSubChunks = splitInto * splitInto * splitInto;
                NativeArray<PathfinderUnit> _unitsToSplit = new NativeArray<PathfinderUnit>(numberOfUnitsToSplit, Allocator.TempJob);
                NativeArray<PathfinderUnit> _subChunksHash = new NativeArray<PathfinderUnit>(numberOfUnitsToSplit * numberOfSubChunks, Allocator.TempJob);
                PathfinderUnit[] units = new PathfinderUnit[numberOfUnitsToSplit];
                for (int i = 0; i < numberOfUnitsToSplit; i++)
                {
                    units[i] = Chunks[i];
                    _unitsToSplit[i] = new PathfinderUnit(units[i]);
                }

                var chunkJob = new SplitChunkJob();
                chunkJob.UnitsToSplit = _unitsToSplit;
                chunkJob.SplittedChunks = _subChunksHash;
                chunkJob.Level = splitInto;
                chunkJob.NumberOfSubChunks = numberOfSubChunks;

                var jHandler = chunkJob.Schedule(numberOfUnitsToSplit, 1);
                jHandler.Complete();


                Vector3 newSize = new Vector3(_subChunksHash[0].m_size, _subChunksHash[0].m_size, _subChunksHash[0].m_size) / 2;
                PathfinderUnit[] subUnits = new PathfinderUnit[_subChunksHash.Length];
                for (int i = 0; i < _subChunksHash.Length; i++)
                {
                    bool unitWalkable = CheckChunk(_subChunksHash[i].m_unit, newSize, m_groundLayers);
                    var unit = new PathfinderUnit(_subChunksHash[i].m_unit, unitWalkable, _subChunksHash[i].m_size);
                    // if(unitWalkable || unit.m_size == 1) m_checkedGridUnits.Add(unit);
                    // else Chunks.Add(unit);
                    subUnits[i] = unit;
                }
                m_checkedGridUnits.UnionWith(subUnits.Where((un) => (un.m_walkable || un.m_size == 1)));
                Chunks = subUnits.Where((un) => (!un.m_walkable && un.m_size > 1)).ToList();
                _unitsToSplit.Dispose();
                _subChunksHash.Dispose();
                loopcount++;
            }
            m_gridPoints = m_checkedGridUnits.ToList();
            if (loopcount > maxLoop) Debug.LogError("Loopcount Exceeded!! Infinite Loop Detected");
            Debug.Log($"Chunks : {m_gridPoints.Count}");
            Debug.Log($"Non walkable Chunks: {m_gridPoints.Where((gp) => !gp.m_walkable).Count()}");
            var t2 = DateTime.Now;
            Debug.Log($"{t2.Subtract(t1).TotalMilliseconds} ms");
        }

        void OnDrawGizmos()
        {
            //     if(m_gridPoints != null && m_gridPoints.Count > 0)
            //     foreach(var v in m_gridPoints){
            //             Gizmos.color = v.m_walkable ? Color.green : Color.red;
            //             if((m_showWalkable && v.m_walkable) || (m_showNonWalkable && !v.m_walkable))Gizmos.DrawWireCube(v.m_unit,new Vector3(v.m_size,v.m_size,v.m_size));
            //     }
            //     else return;

            if (TestPath != null && TestPath.Length > 0)
            {
                for (int i = 0; i < (TestPath.Length - 1); i++)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(TestPath[i].m_unit, TestPath[i + 1].m_unit);

                }
                foreach (var v in TestPath)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireCube(v.m_unit, new Vector3(.9f, .9f, .9f));
                }
            }

            if (testOpen != null && testOpen.Length > 0)
            {
                Gizmos.color = Color.blue;
                foreach (var t in testOpen)
                {
                    Gizmos.DrawWireCube(t.m_unit, t.HalfExtent * 2);
                }
            }
            if (testClosed != null && testClosed.Length > 0)
            {
                Gizmos.color = Color.green;
                foreach (var t in testClosed)
                {
                    Gizmos.DrawWireCube(t.m_unit, t.HalfExtent * 2);
                }
            }
        }

        void FindPath(Vector3 from, Vector3 to)
        {
            // yield return new WaitForSeconds(5);
            DateTime t1 = DateTime.Now;
            int index = 0;
            NativeArray<PathfinderUnit> GridUnits = new NativeArray<PathfinderUnit>();
            GridUnits = m_gridPoints.ToNativeArray(Allocator.TempJob);

            PathfinderUnit startUnit, endUnit;
            startUnit = FindUnitFromPosParallel(GridUnits, from);

            endUnit = FindUnitFromPosParallel(GridUnits, to);

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

                //var findPathJob = new FindPathJob();








                var currentNode = GetLowestFNode(openList);
                Debug.Log($"Testing {currentNode}");


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
                testOpen = openList.ToArray().Select((n) => n.unit).ToArray();
                testClosed = closedList.ToArray().Select((n) => n.unit).ToArray();


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
                TestPath = new PathfinderUnit[size];
                TestPath = path.Select2((pn) => pn.unit).ToArray();
                for (int i = 0; i < path.Length; i++)
                {
                    Debug.Log(path[i]);
                    allNodes.RemoveAt(path[i].index);
                }
                path.Dispose();
            }
            var t2 = DateTime.Now;
            Debug.Log($"It took {t2.Subtract(t1).TotalMilliseconds}ms to find the path");
            allNodes.Dispose();
            openList.Dispose();
            closedList.Dispose();
            GridUnits.Dispose();
        }

        IEnumerator FindPathCoroutine(Vector3 from, Vector3 to)
        {
            yield return new WaitForSeconds(5);
            DateTime t1 = DateTime.Now;
            int index = 0;
            NativeArray<PathfinderUnit> GridUnits = new NativeArray<PathfinderUnit>();
            GridUnits = m_gridPoints.ToNativeArray(Allocator.TempJob);

            PathfinderUnit startUnit, endUnit;
            startUnit = FindUnitFromPosParallel(GridUnits, from);

            endUnit = FindUnitFromPosParallel(GridUnits, to);

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
                var currentNode = GetLowestFNode(openList);
                Debug.Log($"Testing {currentNode}");


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
                testOpen = openList.ToArray().Select((n) => n.unit).ToArray();
                testClosed = closedList.ToArray().Select((n) => n.unit).ToArray();


                yield return new WaitForFixedUpdate();
            }

            if (endNode.cameFromNodeIndex <= 0)
            {
                Debug.LogError("Couldn't find path");
            }
            else
            {
                var path = CalculatePath(allNodes, endNode);
                var size = path.Length;
                TestPath = new PathfinderUnit[size];
                TestPath = path.Select2((pn) => pn.unit).ToArray();
                for (int i = 0; i < path.Length; i++)
                {
                    Debug.Log(path[i]);
                    allNodes.RemoveAt(path[i].index);
                }
                path.Dispose();
            }
            var t2 = DateTime.Now;
            Debug.Log($"It took {t2.Subtract(t1).TotalMilliseconds}ms to find the path");
            allNodes.Dispose();
            openList.Dispose();
            closedList.Dispose();
            GridUnits.Dispose();
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

        private PathNode GetLowestFNode(NativeList<PathNode> openList)
        {
            PathNode lowestCostPathNode = openList[0];
            for (int i = 0; i < openList.Length; i++)
            {
                if (openList[i].fCost < lowestCostPathNode.fCost) lowestCostPathNode = openList[i];
            }
            return lowestCostPathNode;
        }

        private PathNode FindNodeFromPosParallel(NativeArray<PathNode> pathNodeArray, Vector3 pos)
        {
            var findNodeJob = new FindUnitNodeFromPosJob();
            findNodeJob.NodesToSearch = pathNodeArray;
            findNodeJob.Pos = pos;
            var handle = findNodeJob.Schedule(pathNodeArray.Length, 1);
            handle.Complete();
            return findNodeJob.UnitFound;
        }


        private PathfinderUnit FindUnitFromPosParallel(NativeArray<PathfinderUnit> pathNodeArray, Vector3 pos)
        {
            var findNodeJob = new FindUnitaryUnitFromPosJob();
            var resultArray = new NativeArray<PathfinderUnit>(1, Allocator.TempJob);
            PathfinderUnit result = new PathfinderUnit();
            findNodeJob.UnitsToSearch = pathNodeArray;
            findNodeJob.Pos = pos;
            findNodeJob.UnitFound = resultArray;
            var handle = findNodeJob.Schedule(pathNodeArray.Length, 1);
            handle.Complete();
            var idx = findNodeJob.Index;
            result = new PathfinderUnit(resultArray[0]);
            resultArray.Dispose();
            return result;
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




        public PathNode[] GetNeighbours(PathNode me, NativeArray<PathfinderUnit> gridUnits, NativeList<PathNode> allNodes)
        {
            var Directions = new Vector3[]{
            new Vector3(1,0,0) + me.unit.m_unit,
            new Vector3(0,1,0) + me.unit.m_unit,
            new Vector3(0,0,1) + me.unit.m_unit,
            new Vector3(-1,0,0) + me.unit.m_unit,
            new Vector3(0,-1,0) + me.unit.m_unit,
            new Vector3(0,0,-1) +me.unit.m_unit,
        };
            var insideBoundariesDirections = Directions.Where((dir) => IsInsideBoundaries(dir)).ToList();
            var neighbours = new PathNode[insideBoundariesDirections.Count()];
            for (int i = 0; i < neighbours.Length; i++)
            {
                var unit = FindUnitFromPosParallel(gridUnits, insideBoundariesDirections[i]);

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

        bool IsInsideBoundaries(Vector3 point)
        {
            float gridHalf = (float)gridSize / 2;
            if (point.z > gridHalf + transform.position.z || point.z < transform.position.z - gridHalf) return false;
            if (point.y > gridHalf + transform.position.y || point.y < transform.position.y - gridHalf) return false;
            if (point.x > gridHalf + transform.position.x || point.x < transform.position.x - gridHalf) return false;
            return true;
        }

        // PathfinderUnit FindClosestUnitaryFrom(Vector3 point){
        //     return m_gridPoints.Where((node) => node.m_walkable).Aggregate((p1,p2) => (point - p1.GetClosestUnitary(point).m_unit).sqrMagnitude < (point - p2.GetClosestUnitary(point).m_unit).sqrMagnitude ? p1 : p2).GetClosestUnitary(point);
        // }

        public bool CheckChunk(Vector3 center, Vector3 size, LayerMask mask)
        {
            return !Physics.CheckBox(center, size, Quaternion.identity, mask);
        }



        #region GroundCheck Baking Algorithim Iterations
        // [Obsolete]
        // IEnumerator SetUpCube1(){
        //     var t1 = DateTime.Now;
        //     var bakersize = gridSize*gridSize*gridSize;
        //     var areasize = gridSize*gridSize;
        //     var halfCubeExtent = new Vector3(1/2,1/2,1/2);
        //     m_gridPoints = new PathfinderUnit[bakersize];
        //     for(int i=0; i<bakersize; i++){
        //         var x = i % gridSize;
        //         var z = (i/gridSize) % gridSize;
        //         var y = i/(areasize);
        //         var center = new Vector3(x,y,z);
        //         m_gridPoints[i] = new PathfinderUnit(center,!Physics.CheckBox(center,halfCubeExtent,Quaternion.identity,m_groundLayers), 1);
        //         Debug.Log($"X,y,z : ({x},{y},{z})");
        //         yield return new WaitForFixedUpdate();
        //     }
        //     var t2 = DateTime.Now;
        //     Debug.Log(t2.Subtract(t1).TotalMilliseconds + "ms");
        // }
        // [Obsolete]
        // IEnumerator SetUpCube2(){
        //     var t1 = DateTime.Now;
        //     var bakersize = gridSize*gridSize*gridSize;
        //     var cubeExtent = new Vector3(1f,1f,1f);
        //     var halfCubeExtent = cubeExtent/2;

        //     Debug.Log("Started");
        //     var pathUnitsArray = new NativeArray<PathfinderUnit>(bakersize, Allocator.TempJob);
        //     var job = new GroundCheckJob();

        //     var PhysicsScene = Physics.defaultPhysicsScene;

        //     job.SetUpJob(
        //         pathUnitsArray,
        //         gridSize,
        //         cubeExtent,
        //         m_groundLayers,
        //         PhysicsScene
        //     );
        //     var jobHandle = job.Schedule(bakersize, 1);


        //     while(!jobHandle.IsCompleted) yield return null;
        //     jobHandle.Complete();
        //     m_gridPoints = pathUnitsArray.ToArray();
        //     pathUnitsArray.Dispose();


        //     for(int i=0; i < m_gridPoints.Length; i++){
        //         m_gridPoints[i].m_walkable = !Physics.CheckBox(m_gridPoints[i].m_unit,halfCubeExtent,Quaternion.identity,m_groundLayers);
        //     }

        //     var t2 = DateTime.Now;
        //     Debug.Log(t2.Subtract(t1).TotalMilliseconds + "ms");
        //     Debug.Log(m_gridPoints.Length);
        //     Debug.Log(m_gridPoints.Where((p) => !p.m_walkable).Count());

        // }
        // [Obsolete]
        // IEnumerator SetUpCube3 (){ 
        //     yield return new WaitForSeconds(5);
        //      var m_checkedGridUnits = new List<PathfinderUnit>();
        //     Debug.Log("Started");
        //     var chunkLevel = gridSize;
        //     var Chunks = new Queue<PathfinderUnit>();
        //     var bigChunk = new PathfinderUnit(transform.position,CheckChunk(transform.position,new Vector3(chunkLevel * 1,chunkLevel * 1,chunkLevel * 1),m_groundLayers),chunkLevel * 1);
        //     Chunks.Enqueue(bigChunk);
        //     while(Chunks.Count > 0){
        //         int splitInto = 0;
        //         var chunk = Chunks.Dequeue();
        //         chunkLevel = chunk.m_size;
        //         for(int i=2; i<= chunkLevel; i++){
        //             if(chunkLevel % i == 0) {
        //                 chunkLevel = chunkLevel / i; 
        //                 splitInto = i;
        //                 break;
        //             }
        //         }
        //         if(splitInto == 0) {
        //             Debug.LogError("Split 0");
        //         }
        //         Debug.Log($"Chunk Size : {chunkLevel},\nSplitInto : {splitInto}");
        //         var subChunks = chunk.SplitChunk(splitInto);
        //         if(chunkLevel == 1){
        //             var Unwalkable = subChunks.Where((ch) => !ch.m_walkable).Count();
        //             m_checkedGridUnits = m_checkedGridUnits.Concat(subChunks).ToList();
        //         }
        //         else{
        //             m_checkedGridUnits = m_checkedGridUnits.Concat(subChunks.Where((ch) => ch.m_walkable)).ToList();
        //             var rescanChunk = subChunks.Where((ch) => !ch.m_walkable);
        //             foreach(var c in rescanChunk){
        //                 Debug.Log($"Enqueueing chunk : {c.m_unit}\nSize : {c.m_size}\nWalkable : {c.m_walkable}");
        //                 Chunks.Enqueue(c);
        //             }
        //         }
        //         m_gridPoints = m_checkedGridUnits.ToArray();
        //         Units = m_checkedGridUnits;
        //         yield return new WaitForFixedUpdate();
        //     }
        //     Debug.Log($"Chunks : {m_gridPoints.Length}" );
        //     Debug.Log($"Non walkable Chunks: {m_gridPoints.Where((gp) => !gp.m_walkable).Count()}");


        // }
        #endregion
    }
}