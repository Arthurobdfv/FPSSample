using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Pathfinder.PFStructs;

namespace Pathfinder.PFJobs{

[BurstCompile]
public struct FindPathJob : IJobParallelFor
{
    NativeList<PathNode> Allnodes;
    [NativeDisableParallelForRestriction] NativeList<PathNode> OpenList;
    [NativeDisableParallelForRestriction] NativeList<PathNode> ClosedList;

    public void Execute(int index)
    {
        throw new System.NotImplementedException();
    }
}

}