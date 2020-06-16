using UnityEngine;
using Unity.Jobs;
using Unity.Collections;

public struct BoxCastAllPathUnits : IJobParallelFor {

NativeArray<PathfinderUnit> PathfinderUnits;

public void Execute(int index){

        // commands[0] = new BoxcastCommand(center,m_halfCubeExtent,Quaternion.identity,Vector3.zero,m_layerMask);


        // var handle = BoxcastCommand.ScheduleBatch(commands,results,1,default(JobHandle));
        // handle.Complete();
        
        // RaycastHit batchedHit = results[0];

        

    }
}