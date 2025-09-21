using UnityEngine;

public class FallingObject : MonoBehaviour
{
    [Header("Rock Settings")]
    public float fallSpeed = 5f;
    public int damage = 10;
    public float lifetime = 10f; // How long before rock is destroyed if it doesn't hit anything
    public float rotationSpeed = 90f; // Degrees per second
    
    [Header("Effects")]
    public GameObject hitEffect; // Optional particle effect on player hit
    public AudioClip hitSound;   // Optional sound effect
    
    private Rigidbody2D rb;
    private bool hasHitPlayer = false;
    private Transform spriteTransform; // Reference to the child sprite object

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        // Set up the rigidbody for falling
        rb.gravityScale = 0; // We'll control movement manually for consistency
        rb.linearVelocity = Vector2.down * fallSpeed;
        spriteTransform = transform.Find("RockSprite");

        // Destroy the rock after lifetime to prevent memory leaks
        Destroy(gameObject, lifetime);
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        // Check if we hit the player
        if (other.CompareTag("Player") && !hasHitPlayer)
        {
            hasHitPlayer = true;
            
            SoundManager.Instance.PlaySound2D("Die");
            
            // Deal damage to player
            HealthManager healthSystem = other.GetComponent<HealthManager>();
            if (healthSystem != null)
            {
                healthSystem.TakeDamage(damage);
            }
            
            // Trigger visual flash effect
            BlockCollisionDetector collisionDetector = other.GetComponent<BlockCollisionDetector>();
            if (collisionDetector != null)
            {
                collisionDetector.TriggerInvulnerabilityFlash();
            }
            
            
            // Destroy the rock
            Destroy(gameObject);
        }
    }
    
    void Update()
    {
        // Keep the rock moving down at constant speed
        rb.linearVelocity = Vector2.down * fallSpeed;
        if (spriteTransform != null)
        {
            spriteTransform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
        }
        
        // Optional: Destroy if rock goes too far below the camera
        if (transform.position.y < Camera.main.transform.position.y - 20f)
        {
            Destroy(gameObject);
        }
    }
}