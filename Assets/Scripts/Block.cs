// Block types enum
public enum BlockType
{
    Empty = 0,
    Buffer = 1,
    Red = 2,
    Magenta = 3,
    Green = 4,
    Yellow = 5,
    Blue = 6,
}

[System.Serializable]
public class BlockData
{
    public BlockType blockType;
    public float health = 1f;
    public bool isMatched = false;
    
    public BlockData(BlockType type)
    {
        blockType = type;
    }
}
