using System.Linq;
using UnityEngine;

public static class Extension {
    public static PathfinderUnit GetClosestUnitary(this PathfinderUnit me, PathfinderUnit from){
        var closestUnit = me;
        while(closestUnit.m_size > 1){
            int chunkSize = closestUnit.m_size;
            int splitInto = 1;
            for(int i=2; i<chunkSize; i++){
                if(chunkSize % i == 0){
                    splitInto = i;
                    break;
                }
            }
            closestUnit = me.SplitChunk(splitInto).Aggregate((p1,p2) => (p1.m_unit - from.m_unit).sqrMagnitude > (p2.m_unit - from.m_unit).sqrMagnitude ? p1 : p2);
        }
        return closestUnit;
    }

    public static PathfinderUnit[] SplitChunk(this PathfinderUnit chunk, int subdivideInto){
        var newCubeSize = chunk.m_size / subdivideInto;
        var numberOfSubChunks = subdivideInto * subdivideInto * subdivideInto;
        var area = subdivideInto*subdivideInto;
        var pivotChunk = chunk.m_unit - new Vector3(chunk.m_size/2,chunk.m_size/2,chunk.m_size/2);
        var aux = new PathfinderUnit[numberOfSubChunks];
        for(int i=0; i < numberOfSubChunks;i++){
            var x = i % subdivideInto;
            var z = (i/subdivideInto) % subdivideInto;
            var y = i/(area);
            var center = pivotChunk + (new Vector3(x,y,z)*newCubeSize) + (new Vector3(newCubeSize/2,newCubeSize/2,newCubeSize/2));
            var newChunk = new PathfinderUnit(center, true ,newCubeSize);
            aux[i] = newChunk;
        }
        return aux;
    }
}