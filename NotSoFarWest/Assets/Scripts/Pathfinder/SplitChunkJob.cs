using UnityEngine;
using Unity.Jobs;
using Unity.Collections;

public struct SplitChunkJob : IJobParallelFor {

    public NativeArray<PathFinderWithSubQuads> UnitsToSplit;
    

    public int NumberOfSubChunks;

    public int Level;



    public void Execute(int index)
    {
        var chunkToSplit = UnitsToSplit[index];
        var splittedChunk = SplitChunk(chunkToSplit.unit,Level);
        for(int i = 0; i < NumberOfSubChunks; i++){
            chunkToSplit.subQuads[i] = splittedChunk[i];
        }

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
            var newChunk = new PathfinderUnit(center,false,newCubeSize);
            aux[i] = newChunk;
        }
        return aux;
    }
}