using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using UnityEditor;
using UnityEngine;



public class PathfinderBaker : MonoBehaviour
{
    //public int m_simulatedCubeSize;
    public int gridSize;
    public HashSet<PathfinderUnit> m_gridPoints;
    public List<PathfinderUnit> Units = new List<PathfinderUnit>();

    public bool m_showWalkable;
    public bool m_showNonWalkable;

    int m_bakersize;

    public LayerMask m_groundLayers;

    void Start()
    {
        Teste();
        FindPath(new Vector3(10f,10f,0f),new Vector3(20f,20f,0f));
    }

    void Update()
    {

    }
  
    void Teste (){ 
        var t1 = DateTime.Now;
        var m_checkedGridUnits = new HashSet<PathfinderUnit>();
        var chunkLevel = gridSize;
        var Chunks = new List<PathfinderUnit>();
        var chunkExtent = new Vector3(chunkLevel,chunkLevel,chunkLevel);
        var bigChunk = new PathfinderUnit(transform.position,CheckChunk(transform.position,chunkExtent,m_groundLayers),chunkLevel);
        int loopcount = 0;
        var maxLoop = (gridSize);
        Debug.Log($"Maxloop = {maxLoop}");
        Chunks.Add(bigChunk);
        while(Chunks.Count > 0 && loopcount < maxLoop){
            int splitInto = 1;
            var chunk = Chunks[0];
            chunkLevel = chunk.m_size;


            if(chunkLevel == 1){
                m_checkedGridUnits.Add(chunk);
                continue;
            }

            for(int i=2; i<= chunkLevel; i++){
                if(chunkLevel % i == 0) {
                    chunkLevel = chunkLevel / i; 
                    splitInto = i;
                    break;
                }
            }

            int numberOfUnitsToSplit = Chunks.Count;
            int numberOfSubChunks = splitInto * splitInto * splitInto;
            NativeArray<PathfinderUnit> _unitsToSplit = new NativeArray<PathfinderUnit>(numberOfUnitsToSplit,Allocator.TempJob);
            NativeArray<PathfinderUnit> _subChunksHash = new NativeArray<PathfinderUnit>(numberOfUnitsToSplit*numberOfSubChunks,Allocator.TempJob);
            PathfinderUnit[] units = new PathfinderUnit[numberOfUnitsToSplit];
            for(int i=0; i<numberOfUnitsToSplit; i++){
                units[i] = Chunks[i];
                _unitsToSplit[i] = new PathfinderUnit(units[i]);
            }

            var chunkJob = new SplitChunkJob();
            chunkJob.UnitsToSplit = _unitsToSplit;
            chunkJob.SplittedChunks = _subChunksHash;
            chunkJob.Level = splitInto;
            chunkJob.NumberOfSubChunks = numberOfSubChunks;

            var jHandler = chunkJob.Schedule(numberOfUnitsToSplit,1);
            jHandler.Complete();


            Vector3 newSize = new Vector3(_subChunksHash[0].m_size,_subChunksHash[0].m_size,_subChunksHash[0].m_size) / 2;
            PathfinderUnit[] subUnits = new PathfinderUnit[_subChunksHash.Length];
            for(int i = 0; i< _subChunksHash.Length; i++){
                bool unitWalkable = CheckChunk(_subChunksHash[i].m_unit,newSize,m_groundLayers);
                var unit = new PathfinderUnit(_subChunksHash[i].m_unit,unitWalkable,_subChunksHash[i].m_size);
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
        m_gridPoints = m_checkedGridUnits;
        if(loopcount > maxLoop) Debug.LogError("Loopcount Exceeded!! Infinite Loop Detected");
        Debug.Log($"Chunks : {m_gridPoints.Count}" );
        Debug.Log($"Non walkable Chunks: {m_gridPoints.Where((gp) => !gp.m_walkable).Count()}");
    var t2 = DateTime.Now;
    Debug.Log($"{t2.Subtract(t1).TotalMilliseconds} ms");
    }

    void OnDrawGizmos() {
        if(m_gridPoints != null && m_gridPoints.Count > 0)
        foreach(var v in m_gridPoints){
                Gizmos.color = v.m_walkable ? Color.green : Color.red;
                if((m_showWalkable && v.m_walkable) || (m_showNonWalkable && !v.m_walkable))Gizmos.DrawWireCube(v.m_unit,new Vector3(v.m_size,v.m_size,v.m_size));
        }
        else return;
    }

    void FindPath(Vector3 from, Vector3 to){
        PathfinderUnit startUnit, endUnit;
        startUnit = FindClosestNodeFrom(from);
        endUnit = FindClosestNodeFrom(to);

        NativeArray<PathNode> pathNodeArray = new NativeArray<PathNode>(m_gridPoints.Count, Allocator.Temp);    
    
        for(int i=0; i< m_gridPoints.Count; i++){
            PathNode pathNode = new PathNode();
            pathNode.unit = m_gridPoints.ElementAt(i);
            pathNode.index = i;

            pathNode.gCost = int.MaxValue;
            pathNode.hCost = CalculateDistanceCost(pathNode.unit.m_unit,endUnit.m_unit);
            pathNode.CalculateFCost();
            pathNode.cameFromNodeIndex = -1;
            pathNodeArray[i] = pathNode;
        }

        PathNode startNode = pathNodeArray.Where((n) => n.unit.Equals(startUnit)).FirstOrDefault();
        PathNode endNode = pathNodeArray.Where((n) => n.unit.Equals(endUnit)).FirstOrDefault();

        startNode.gCost = 0f;
        startNode.CalculateFCost();
        pathNodeArray[startNode.index] = startNode;

        NativeList<int> openList = new NativeList<int>(Allocator.Temp);
        NativeList<int> closedList = new NativeList<int>(Allocator.Temp);

        openList.Add(startNode.index);

        while(openList.Length > 0){
            int currentNodeIndex = GetLowestFNodeIndex(openList,pathNodeArray);
            PathNode currentNode = pathNodeArray[currentNodeIndex];
            if(currentNodeIndex == endNode.index){
                break;
            }

            for(int i=0; i< openList.Length; i++){
                if(openList[i] == currentNodeIndex){
                    openList.RemoveAtSwapBack(i);
                    break;
                }
            }

            closedList.Add(currentNodeIndex);

            var neigh = GetNeighbours(currentNode, pathNodeArray).Where((n) => !closedList.Contains(n.index) && n.unit.m_walkable).ToArray();

            for(int i=0; i< neigh.Length; i++){
                var neighbour = neigh[i];
                var tentativegCost = currentNode.gCost + CalculateDistanceCost(currentNode.unit.m_unit, neigh.ElementAt(i).unit.GetClosestUnitary(currentNode.unit).m_unit);
                if(tentativegCost < neigh.ElementAt(i).gCost){
                    neighbour.cameFromNodeIndex = currentNodeIndex;
                    neighbour.gCost = tentativegCost;
                    neighbour.CalculateFCost();
                    pathNodeArray[neighbour.index] = neighbour;
                    if(!openList.Contains(neighbour.index)){
                        openList.Add(neighbour.index);
                    }
                }

            }

        }

        if(endNode.cameFromNodeIndex == -1){

        } else {
            NativeList<PathfinderUnit> path = CalculatePath(pathNodeArray, endNode);
            path.Dispose();
        }

        openList.Dispose();
        closedList.Single();
        pathNodeArray.Dispose();
    }

    private NativeList<PathfinderUnit> CalculatePath(NativeArray<PathNode> pathNodeArray, PathNode endNode){
        if(endNode.cameFromNodeIndex == 1){
            return new NativeList<PathfinderUnit>(Allocator.Temp);
        }else {
            var path = new NativeList<PathfinderUnit>(Allocator.Temp);
            path.Add(endNode.unit.GetClosestUnitary(pathNodeArray[endNode.cameFromNodeIndex].unit));
            PathNode currentNode = endNode;
            while(currentNode.cameFromNodeIndex != -1){
                PathNode cameFromNode = pathNodeArray[currentNode.cameFromNodeIndex];
                path.Add(cameFromNode.unit.GetClosestUnitary(cameFromNode.unit.GetClosestUnitary(pathNodeArray[cameFromNode.cameFromNodeIndex].unit)));
                currentNode = cameFromNode; 
            }
            return path;
        }
    }

    private int GetLowestFNodeIndex(NativeList<int> openList, NativeArray<PathNode> pathNodeArray){
        PathNode lowestCostPathNode = pathNodeArray[openList[0]];
        for(int i=0;i<openList.Length; i++){
            PathNode testPathNode = pathNodeArray[openList[i]];
            if(testPathNode.fCost < lowestCostPathNode.fCost){
                lowestCostPathNode = testPathNode;
            }
        }
        return lowestCostPathNode.index;
    }

    private float CalculateDistanceCost(Vector3 aPos,Vector3 bPos){
        var xDistance = Math.Abs(aPos.x - bPos.x);
        var yDistance = Math.Abs(aPos.y - bPos.y);
        var zDistance = Math.Abs(aPos.z - bPos.z);
        return xDistance+yDistance+zDistance * 10;
    }

    public PathNode[] GetNeighbours(PathNode me,NativeArray<PathNode> pathNodeArray){
        var Directions = new Vector3[]{
            new Vector3(1,0,0),
            new Vector3(0,1,0),
            new Vector3(0,0,1),
            new Vector3(-1,0,0),
            new Vector3(0,-1,0),
            new Vector3(0,0,-1),
        };
        var insideBoundariesDirections = Directions.Where((dir) => IsInsideBoundaries(dir));
        var neighbours = new PathNode[insideBoundariesDirections.Count()]; 
        for (int i = 0; i < neighbours.Length; i++)
        {
            neighbours[i] = pathNodeArray.Where((gp) => gp.unit.Contains(insideBoundariesDirections.ElementAt(i))).First();
        }

        return neighbours;
    }

    bool IsInsideBoundaries(Vector3 point){
        float gridHalf = (float)gridSize/2;
        if(point.z > gridHalf+transform.position.z || point.z < gridHalf-transform.position.z) return false;
        if(point.y > gridHalf+transform.position.y || point.y < gridHalf-transform.position.y) return false;
        if(point.x > gridHalf+transform.position.x || point.x < gridHalf-transform.position.x) return false;
        return true;
    }

    PathfinderUnit FindClosestNodeFrom(Vector3 point){
        return m_gridPoints.Where((node) => node.m_walkable).Aggregate((p1,p2) => (p1.m_unit - point).sqrMagnitude > (p2.m_unit - point).sqrMagnitude ? p1 : p2);
    }

    public bool CheckChunk(Vector3 center, Vector3 size, LayerMask mask){
        return !Physics.CheckBox(center,size,Quaternion.identity,mask);
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
