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

    [Header("Attack Cooldown Settings")]
    public float noneToolCooldown = 1.0f;      // No tool - slowest attacks
    public float pickaxeCooldown = 0.3f;       // Pickaxe - fast attacks
    public float hammerCooldown = 0.8f;        // Hammer - slower but powerful

    private float lastAttackTime = 0f;

    void Start()
    {
        player = FindAnyObjectByType<PlayerMovement>();
        blocksDestroyed = 0;
        toolTimer = 0;
        lastAttackTime = 0f;
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

    // Check if player can attack based on current tool's cooldown
    public bool CanAttack()
    {
        float requiredCooldown = GetCurrentCooldown();
        return Time.time - lastAttackTime >= requiredCooldown;
    }

    // Call this when an attack is made
    public void OnAttackMade()
    {
        lastAttackTime = Time.time;
    }

    // Get the current cooldown time based on equipped tool
    private float GetCurrentCooldown()
    {
        switch (player.tool)
        {
            case Tool.None:
                return noneToolCooldown;
            case Tool.Pickaxe:
                return pickaxeCooldown;
            case Tool.Hammer:
                return hammerCooldown;
            default:
                return noneToolCooldown;
        }
    }

    // Get time remaining until next attack is available
    public float GetAttackCooldownRemaining()
    {
        float requiredCooldown = GetCurrentCooldown();
        float timeSinceLastAttack = Time.time - lastAttackTime;
        return Mathf.Max(0f, requiredCooldown - timeSinceLastAttack);
    }

    public void ResetToolTimer()
    {
        blocksDestroyed = 0;
        toolTimer = 0;
        lastAttackTime = 0f;

        // Make sure player tool is set to None
        if (player != null)
        {
            player.tool = Tool.None;
        }

        Debug.Log("Tool timer reset");
    }
}