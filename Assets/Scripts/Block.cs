// Block types enum
public enum BlockType
{
    Empty = 0,
    Buffer = 1,
    Hard = 2,
    Tool = 3, 
    Life = 4,
    Blue = 5,
    Orange = 6,
    Green = 7,
    Purple = 8,
    Magenta = 9

}

[System.Serializable]
public class BlockData
{
    public BlockType blockType;
    public float health = 1f;
    public bool isMatched = false;
    public Tool tool = Tool.None;
    public BlockData(BlockType type)
    {
        blockType = type;
        if (type == BlockType.Empty)
        {
            health = 0f;
        }
        else if (type == BlockType.Hard)
        {
            // SET HARD HP HERE
            health = 4f;
        }
    }
    public void Damage()
    {
        if (blockType != BlockType.Hard || health <= 0) return;
        health--;
    }
    public int Health()
    {
        return (int)health;
    }
    public bool IsCollectible()
    {
        return blockType == BlockType.Life || blockType == BlockType.Tool;
    }
    public void SetTool(int num)
    {
        tool = (Tool)num;
    }
}
