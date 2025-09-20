using UnityEngine;
public enum Tool
{
    None = 0,
    Pickaxe = 1, //speedyBreak
    Hammer = 2 //3x3 destroy
}

public class ToolTimer : MonoBehaviour
{
    private PlayerMovement player;
    private int blocksDestroyed = 0;
    public int hammerDurationInBlocks = 10;
    public int pickaxeDurationInBlocks = 20;
    private int toolTimer = 0;

    void Start()
    {
        player = FindAnyObjectByType<PlayerMovement>();
        blocksDestroyed = 0;
        toolTimer = 0;
    }
    //call when picking up a tool
    public void StartTimer(Tool tool)
    {
        if (tool == Tool.None) return;
        toolTimer = blocksDestroyed + (tool == Tool.Hammer ? hammerDurationInBlocks : pickaxeDurationInBlocks);
    }
    //call when a block is destroyed
    public void ToolTimerTick()
    {
        blocksDestroyed++;
        if (blocksDestroyed == toolTimer)
        {
            player.tool = Tool.None;
        }
    }

}
