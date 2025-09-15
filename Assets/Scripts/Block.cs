// Block types enum
using UnityEditor.ShaderKeywordFilter;

public enum BlockType
{
    Empty = 0,
    Buffer = 1,
    Hard = 2,
    Blue = 3,
    Magenta = 4,
    Orange = 5,
    Green = 6,
    Purple = 7,
    Life = 8
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
        return blockType == BlockType.Life;
    }
}