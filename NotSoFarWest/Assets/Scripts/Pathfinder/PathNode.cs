

public struct PathNode {
    public PathfinderUnit unit;

    public int index;

    public float gCost;
    public float hCost;
    public float fCost;

    public int cameFromNodeIndex;

    public void CalculateFCost(){
        fCost = gCost + hCost;
    }


}