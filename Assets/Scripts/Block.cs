// Block types enum
using UnityEditor.ShaderKeywordFilter;

public enum BlockType
{
    Empty = 0,
    Buffer = 1,
    Hard = 2,
    Magenta = 3,
    Green = 4,
    Yellow = 5,
    Blue = 6,
    Red = 7
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
}
