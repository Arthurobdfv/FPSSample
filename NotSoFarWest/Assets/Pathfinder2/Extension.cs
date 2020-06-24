using System;
using System.Collections.Generic;
using System.Linq;
using Pathfinder.PFStructs;
using Unity.Collections;
using UnityEngine;

namespace Pathfinder.Extensions
{
    public static class Extension
    {
        public static PathfinderUnit GetClosestUnitary(this PathfinderUnit me, PathfinderUnit from)
        {
            return me.GetClosestUnitary(me.m_unit);
        }

        public static PathfinderUnit GetClosestUnitary(this PathfinderUnit me, Vector3 from)
        {
            var closestUnit = me;
            int loopCount = 0;
            while (closestUnit.m_size > 1 && loopCount < 100000)
            {
                int chunkSize = closestUnit.m_size;
                int splitInto = 1;
                for (int i = 2; i <= chunkSize; i++)
                {
                    if (chunkSize % i == 0)
                    {
                        splitInto = i;
                        break;
                    }
                }
                loopCount++;
                var sub = closestUnit.SplitChunk(splitInto);
                var closest = sub[0];
                for (int x = 0; x < sub.Length; x++)
                {
                    if ((sub[x].m_unit - from).sqrMagnitude <= (closest.m_unit - from).sqrMagnitude) closest = sub[x];
                }

                closestUnit = closest;
            }
            if (loopCount >= 100000) Debug.LogError("GetClosestUnitary loop exceeded");
            return closestUnit;
        }

        public static PathfinderUnit[] SplitChunk(this PathfinderUnit chunk, int subdivideInto)
        {
            var newCubeSize = chunk.m_size / subdivideInto;
            var numberOfSubChunks = subdivideInto * subdivideInto * subdivideInto;
            var area = subdivideInto * subdivideInto;
            var pivotChunk = chunk.m_unit - new Vector3(chunk.m_size / 2, chunk.m_size / 2, chunk.m_size / 2);
            var aux = new PathfinderUnit[numberOfSubChunks];
            for (int i = 0; i < numberOfSubChunks; i++)
            {
                var x = i % subdivideInto;
                var z = (i / subdivideInto) % subdivideInto;
                var y = i / (area);
                var center = pivotChunk + (new Vector3(x, y, z) * newCubeSize) + (new Vector3(newCubeSize / 2, newCubeSize / 2, newCubeSize / 2));
                var newChunk = new PathfinderUnit(center, true, newCubeSize);
                aux[i] = newChunk;
            }
            return aux;
        }

        public static bool SearchFor(this NativeList<PathNode> array, PathNode value)
        {
            bool contain = false;
            for (int i = 0; i < array.Length; i++)
            {
                if (value.Equals(array[i]))
                {
                    contain = true;
                    break;
                }
            }
            return contain;
        }

        public static PathNode[] Filter(this PathNode[] origin, Func<PathNode, bool> func)
        {
            var aux = new List<PathNode>();
            foreach (var node in origin)
            {
                if (func(node)) aux.Add(node);
            }
            return aux.ToArray();
        }

        public static NativeArray<TResult> Select2<TSource, TResult>(this NativeList<TSource> origin, Func<TSource, TResult> func)
         where TSource : struct
         where TResult : struct
        {
            var t = new NativeArray<TResult>(origin.Length,Allocator.Persistent);
            for (int i = 0; i < origin.Length; i++)
            {
                t[i] = func(origin.ElementAt(i));
            }
            return t;
        }

        public static PathNode GetLowestFNode(this NativeList<PathNode> openList)
        {
            PathNode lowestCostPathNode = openList[0];
            for (int i = 0; i < openList.Length; i++)
            {
                if (openList[i].fCost < lowestCostPathNode.fCost) lowestCostPathNode = openList[i];
            }
            return lowestCostPathNode;
        }


    }

}