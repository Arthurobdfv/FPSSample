using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;



public class PathfinderBaker : MonoBehaviour
{
    //public int m_simulatedCubeSize;
    public int gridSize;
    public PathfinderUnit[] m_gridPoints;
    public List<PathfinderUnit> Units = new List<PathfinderUnit>();

    public bool m_showWalkable;
    public bool m_showNonWalkable;

    int m_bakersize;

    public LayerMask m_groundLayers;

    void Start()
    {

    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space)) Teste();
    }

   
    void Teste (){ 
        var t1 = DateTime.Now;
        var m_checkedGridUnits = new List<PathfinderUnit>();
        var chunkLevel = gridSize;
        var Chunks = new Queue<PathfinderUnit>();
        var chunkExtent = new Vector3(chunkLevel,chunkLevel,chunkLevel);
        var bigChunk = new PathfinderUnit(transform.position,CheckChunk(transform.position,chunkExtent,m_groundLayers),chunkLevel);
        Chunks.Enqueue(bigChunk);
        while(Chunks.Count > 0){
            int splitInto = 1;
            var chunk = Chunks.First();
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
            NativeArray<PathFinderWithSubQuads> _unitsToSplit = new NativeArray<PathFinderWithSubQuads>(numberOfUnitsToSplit,Allocator.TempJob);
            PathfinderUnit[] units = new PathfinderUnit[numberOfUnitsToSplit];
            for(int i=0; i<numberOfUnitsToSplit; i++){
                units[i] = Chunks.Dequeue();
                _unitsToSplit[i] = new PathFinderWithSubQuads(units[i], new NativeArray<PathfinderUnit>(numberOfSubChunks,Allocator.TempJob));

            }

            var chunkJob = new SplitChunkJob();
            chunkJob.UnitsToSplit = _unitsToSplit;
            chunkJob.Level = splitInto;
            chunkJob.NumberOfSubChunks = numberOfSubChunks;

            var jHandler = chunkJob.Schedule(numberOfUnitsToSplit,1);
            jHandler.Complete();

            var _splittedUnits = new List<PathfinderUnit>();
            for(int i=0; i<numberOfUnitsToSplit;i++){
                _splittedUnits = _splittedUnits.Concat(_unitsToSplit[i].subQuads).ToList();
            }

            Vector3 newSize = new Vector3(chunkLevel / splitInto,chunkLevel / splitInto,chunkLevel / splitInto);
            
            for(int i = 0; i< _splittedUnits.Count; i++){
                bool unitWalkable = CheckChunk(_splittedUnits[i].m_unit,newSize,m_groundLayers);
                var unit = new PathfinderUnit(_splittedUnits[i].m_unit,unitWalkable,_splittedUnits[i].m_size);
                if(unitWalkable) m_checkedGridUnits.Add(unit);
                else Chunks.Enqueue(unit);
            }

            // Debug.Log($"Chunk Size : {chunkLevel},\nSplitInto : {splitInto}");
            // var subChunks = SplitChunk(chunk,splitInto);
            // if(chunkLevel == 1){
            //     var Unwalkable = subChunks.Where((ch) => !ch.m_walkable).Count();
            //     m_checkedGridUnits = m_checkedGridUnits.Concat(subChunks).ToList();
            // }
            // else{
            //     m_checkedGridUnits = m_checkedGridUnits.Concat(subChunks.Where((ch) => ch.m_walkable)).ToList();
            //     var rescanChunk = subChunks.Where((ch) => !ch.m_walkable);
            //     foreach(var c in rescanChunk){
            //         Debug.Log($"Enqueueing chunk : {c.m_unit}\nSize : {c.m_size}\nWalkable : {c.m_walkable}");
            //         Chunks.Enqueue(c);
            //     }
            // }
            // m_gridPoints = m_checkedGridUnits.ToArray();
            // Units = m_checkedGridUnits;
        }
        Debug.Log($"Chunks : {m_gridPoints.Length}" );
        Debug.Log($"Non walkable Chunks: {m_gridPoints.Where((gp) => !gp.m_walkable).Count()}");
    var t2 = DateTime.Now;
    Debug.Log($"{t2.Subtract(t1).TotalMilliseconds} ms");
    }

    void OnDrawGizmos() {
        if(m_gridPoints != null && m_gridPoints.Length > 0)
        foreach(var v in m_gridPoints){
                Gizmos.color = v.m_walkable ? Color.green : Color.red;
                if((m_showWalkable && v.m_walkable) || (m_showNonWalkable && !v.m_walkable))Gizmos.DrawWireCube(v.m_unit,new Vector3(v.m_size,v.m_size,v.m_size));
        }
        else return;
    }

    public bool CheckChunk(Vector3 center, Vector3 size, LayerMask mask){
        return !Physics.CheckBox(center,size,Quaternion.identity,mask);
    }

    public PathfinderUnit[] SplitChunk(PathfinderUnit chunk, int level){
        var newCubeSize = chunk.m_size / level;
        var numberOfSubChunks = level * level * level;
        var area = level*level;
        var pivotChunk = chunk.m_unit - new Vector3(chunk.m_size/2,chunk.m_size/2,chunk.m_size/2);
        var aux = new PathfinderUnit[numberOfSubChunks];
        for(int i=0; i < numberOfSubChunks;i++){
            var x = i % level;
            var z = (i/level) % level;
            var y = i/(area);
            var center = pivotChunk + (new Vector3(x,y,z)*newCubeSize) + (new Vector3(newCubeSize/2,newCubeSize/2,newCubeSize/2));
            var newChunk = new PathfinderUnit(center,CheckChunk(center, new Vector3(newCubeSize,newCubeSize,newCubeSize) ,m_groundLayers),newCubeSize);
            aux[i] = newChunk;
        }
        return aux;
    }

     #region GroundCheck Baking Algorithim Iterations
    [Obsolete]
    IEnumerator SetUpCube1(){
        var t1 = DateTime.Now;
        var bakersize = gridSize*gridSize*gridSize;
        var areasize = gridSize*gridSize;
        var halfCubeExtent = new Vector3(1/2,1/2,1/2);
        m_gridPoints = new PathfinderUnit[bakersize];
        for(int i=0; i<bakersize; i++){
            var x = i % gridSize;
            var z = (i/gridSize) % gridSize;
            var y = i/(areasize);
            var center = new Vector3(x,y,z);
            m_gridPoints[i] = new PathfinderUnit(center,!Physics.CheckBox(center,halfCubeExtent,Quaternion.identity,m_groundLayers), 1);
            Debug.Log($"X,y,z : ({x},{y},{z})");
            yield return new WaitForFixedUpdate();
        }
        var t2 = DateTime.Now;
        Debug.Log(t2.Subtract(t1).TotalMilliseconds + "ms");
    }
    [Obsolete]
    IEnumerator SetUpCube2(){
        var t1 = DateTime.Now;
        var bakersize = gridSize*gridSize*gridSize;
        var cubeExtent = new Vector3(1f,1f,1f);
        var halfCubeExtent = cubeExtent/2;
        
        Debug.Log("Started");
        var pathUnitsArray = new NativeArray<PathfinderUnit>(bakersize, Allocator.TempJob);
        var job = new GroundCheckJob();

        var PhysicsScene = Physics.defaultPhysicsScene;

        job.SetUpJob(
            pathUnitsArray,
            gridSize,
            cubeExtent,
            m_groundLayers,
            PhysicsScene
        );
        var jobHandle = job.Schedule(bakersize, 1);


        while(!jobHandle.IsCompleted) yield return null;
        jobHandle.Complete();
        m_gridPoints = pathUnitsArray.ToArray();
        pathUnitsArray.Dispose();


        for(int i=0; i < m_gridPoints.Length; i++){
            m_gridPoints[i].m_walkable = !Physics.CheckBox(m_gridPoints[i].m_unit,halfCubeExtent,Quaternion.identity,m_groundLayers);
        }

        var t2 = DateTime.Now;
        Debug.Log(t2.Subtract(t1).TotalMilliseconds + "ms");
        Debug.Log(m_gridPoints.Length);
        Debug.Log(m_gridPoints.Where((p) => !p.m_walkable).Count());
    }
    [Obsolete]
    IEnumerator SetUpCube3 (){ 
        yield return new WaitForSeconds(5);
         var m_checkedGridUnits = new List<PathfinderUnit>();
        Debug.Log("Started");
        var chunkLevel = gridSize;
        var Chunks = new Queue<PathfinderUnit>();
        var bigChunk = new PathfinderUnit(transform.position,CheckChunk(transform.position,new Vector3(chunkLevel * 1,chunkLevel * 1,chunkLevel * 1),m_groundLayers),chunkLevel * 1);
        Chunks.Enqueue(bigChunk);
        while(Chunks.Count > 0){
            int splitInto = 0;
            var chunk = Chunks.Dequeue();
            chunkLevel = chunk.m_size;
            for(int i=2; i<= chunkLevel; i++){
                if(chunkLevel % i == 0) {
                    chunkLevel = chunkLevel / i; 
                    splitInto = i;
                    break;
                }
            }
            if(splitInto == 0) {
                Debug.LogError("Split 0");
            }
            Debug.Log($"Chunk Size : {chunkLevel},\nSplitInto : {splitInto}");
            var subChunks = SplitChunk(chunk,splitInto);
            if(chunkLevel == 1){
                var Unwalkable = subChunks.Where((ch) => !ch.m_walkable).Count();
                m_checkedGridUnits = m_checkedGridUnits.Concat(subChunks).ToList();
            }
            else{
                m_checkedGridUnits = m_checkedGridUnits.Concat(subChunks.Where((ch) => ch.m_walkable)).ToList();
                var rescanChunk = subChunks.Where((ch) => !ch.m_walkable);
                foreach(var c in rescanChunk){
                    Debug.Log($"Enqueueing chunk : {c.m_unit}\nSize : {c.m_size}\nWalkable : {c.m_walkable}");
                    Chunks.Enqueue(c);
                }
            }
            m_gridPoints = m_checkedGridUnits.ToArray();
            Units = m_checkedGridUnits;
            yield return new WaitForFixedUpdate();
        }
        Debug.Log($"Chunks : {m_gridPoints.Length}" );
        Debug.Log($"Non walkable Chunks: {m_gridPoints.Where((gp) => !gp.m_walkable).Count()}");


    }
    #endregion
}
