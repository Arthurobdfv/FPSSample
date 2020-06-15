using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public struct GroundCheckJob : IJobParallelFor
{
    public NativeArray<PathfinderUnit> PathfinderUnitArray;
    public int m_gridSize,m_areaSize,m_bakerSize;
    public Vector3 m_cubeExtent;
    public Vector3 m_halfCubeExtent;

    public PhysicsScene _physics;

    //public Physics _physics;

    public LayerMask m_layerMask;
    public void Execute(int index)
    {
        var data = PathfinderUnitArray[index];
        var x = index % m_gridSize;
        var z = (index/m_gridSize) % m_gridSize;
        var y = index/(m_areaSize);
        var center = new Vector3(x,y,z);
        RaycastHit hit;
        PathfinderUnitArray[index] = new PathfinderUnit(center,!_physics.BoxCast(center,m_halfCubeExtent,Vector3.zero,out hit,Quaternion.identity,m_layerMask),(int)m_cubeExtent.x);
    }
}

public class PathfinderBaker : MonoBehaviour
{
    public int m_simulatedCubeSize;
    public int gridSize;
    public PathfinderUnit[] m_gridPoints;

    int m_bakersize;

    public LayerMask[] m_groundLayers;
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space)) StartCoroutine(SetUpCube2());
    }

    IEnumerator SetUpCube1(){
        var t1 = DateTime.Now;
        var bakersize = gridSize*gridSize*gridSize;
        var areasize = gridSize*gridSize;
        var halfCubeExtent = new Vector3(m_simulatedCubeSize/2,m_simulatedCubeSize/2,m_simulatedCubeSize/2);
        m_gridPoints = new PathfinderUnit[bakersize];
        for(int i=0; i<bakersize; i++){
            var x = i % gridSize;
            var z = (i/gridSize) % gridSize;
            var y = i/(areasize);
            var center = new Vector3(x,y,z) * m_simulatedCubeSize;
            m_gridPoints[i] = new PathfinderUnit(center,!Physics.CheckBox(center,halfCubeExtent,Quaternion.identity,m_groundLayers[0]), m_simulatedCubeSize);
            Debug.Log($"X,y,z : ({x},{y},{z})");
            yield return new WaitForFixedUpdate();
        }
        var t2 = DateTime.Now;
        Debug.Log(t2.Subtract(t1).TotalMilliseconds + "ms");
    }

    IEnumerator SetUpCube2(){
        var t1 = DateTime.Now;
        var bakersize = gridSize*gridSize*gridSize;
        var halfCubeExtent = new Vector3(m_simulatedCubeSize/2,m_simulatedCubeSize/2,m_simulatedCubeSize/2);
        
        Debug.Log("Started");
        var pathUnitsArray = new NativeArray<PathfinderUnit>(bakersize, Allocator.TempJob);
        var job = new GroundCheckJob();
        job.PathfinderUnitArray = pathUnitsArray;
        job.m_areaSize = gridSize*gridSize;
        job.m_gridSize = gridSize;
        job.m_cubeExtent = 2*halfCubeExtent;
        job.m_halfCubeExtent = halfCubeExtent;
        job.m_layerMask = m_groundLayers[0];
        job._physics  = Physics.defaultPhysicsScene;
        var jobHandle = job.Schedule(bakersize, 1);
        while(!jobHandle.IsCompleted) yield return null;
        jobHandle.Complete();
        m_gridPoints = pathUnitsArray.ToArray();
        pathUnitsArray.Dispose();
        var t2 = DateTime.Now;
        Debug.Log(t2.Subtract(t1).TotalMilliseconds + "ms");
        Debug.Log(m_gridPoints.Length);
        foreach(var v in m_gridPoints){
            Debug.Log($"X,y,z : ({v.m_unit.x},{v.m_unit.y},{v.m_unit.z})");
            yield return null;
        }
    }

    void OnDrawGizmos() {
        if(m_gridPoints != null && m_gridPoints.Length > 0)
        foreach(var v in m_gridPoints){
                Gizmos.color = v.m_walkable ? Color.green : Color.red;
                Gizmos.DrawWireCube(new Vector3(v.m_unit.x,v.m_unit.y,v.m_unit.z),new Vector3(m_simulatedCubeSize,m_simulatedCubeSize,m_simulatedCubeSize));
        }
        else return;
    }
}
