using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HealthManager : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    [SerializeField] private float healthDecayRate = 2f; // Health lost per second
    [SerializeField] private float blockDamage = 20f; // Damage when hit by falling block
    [SerializeField] private float lifeItemHealAmount = 30f; // Health gained from life items

    [Header("UI References")]
    [SerializeField] private Slider healthBar;
    [SerializeField] private Text healthText; // Optional text display

    [Header("Audio/Visual Effects")]
    [SerializeField] private AudioClip damageSound;
    [SerializeField] private AudioClip healSound;
    [SerializeField] private AudioClip lowHealthSound;
    [SerializeField] private GameObject damageEffect; // Particle effect for damage
    
    private AudioSource audioSource;
    private PlayerMovement playerMovement;
    private bool isGameOver = false;
    private bool lowHealthWarning = false;
    private float lowHealthThreshold = 25f;

    // Events for other systems to listen to
    public System.Action OnPlayerDeath;
    public System.Action<float> OnHealthChanged;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        
        playerMovement = GetComponent<PlayerMovement>();
        currentHealth = maxHealth;
    }

    void Start()
    {
        UpdateHealthUI();
    }

    void Update()
    {
        if (isGameOver) return;

        // Decrease health over time
        DecreaseHealthOverTime();
        
        // Check for low health warning
        CheckLowHealthWarning();
        
        UpdateHealthUI();
    }

    private void DecreaseHealthOverTime()
    {
        currentHealth -= healthDecayRate * Time.deltaTime;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        
        OnHealthChanged?.Invoke(currentHealth / maxHealth);
        
        if (currentHealth <= 0 && !isGameOver)
        {
            GameOver();
        }
    }

    private void CheckLowHealthWarning()
    {
        if (currentHealth <= lowHealthThreshold && !lowHealthWarning)
        {
            lowHealthWarning = true;
            PlaySound(lowHealthSound);
            // Could add screen flash or other warning effects here
        }
        else if (currentHealth > lowHealthThreshold && lowHealthWarning)
        {
            lowHealthWarning = false;
        }
    }

    public void TakeDamage(float damage)
    {
        if (isGameOver) return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        
        PlaySound(damageSound);
        ShowDamageEffect();
        
        OnHealthChanged?.Invoke(currentHealth / maxHealth);
        
        if (currentHealth <= 0)
        {
            GameOver();
        }
        
        Debug.Log($"Player took {damage} damage. Health: {currentHealth}/{maxHealth}");
    }

    public void Heal(float healAmount)
    {
        if (isGameOver) return;

        currentHealth += healAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        
        PlaySound(healSound);
        OnHealthChanged?.Invoke(currentHealth / maxHealth);
        
        Debug.Log($"Player healed {healAmount}. Health: {currentHealth}/{maxHealth}");
    }

    public void OnBlockFallDamage()
    {
        TakeDamage(blockDamage);
        Debug.Log("Player hit by falling block!");
    }

    public void OnLifeItemCollected()
    {
        Heal(lifeItemHealAmount);
        Debug.Log("Life item collected!");
    }

    private void UpdateHealthUI()
    {
        if (healthBar != null)
        {
            healthBar.value = currentHealth / maxHealth;
        }
        
        if (healthText != null)
        {
            healthText.text = $"{Mathf.Ceil(currentHealth)}/{maxHealth}";
        }
    }

    private void ShowDamageEffect()
    {
        if (damageEffect != null)
        {
            GameObject effect = Instantiate(damageEffect, transform.position, Quaternion.identity);
            Destroy(effect, 1f);
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private void GameOver()
    {
        isGameOver = true;
        OnPlayerDeath?.Invoke();
        Debug.Log("Game Over!");
        
        // Disable player movement
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        isGameOver = false;
        lowHealthWarning = false;
        
        if (playerMovement != null)
        {
            playerMovement.enabled = true;
        }
        
        UpdateHealthUI();
    }

    // Getters for other scripts
    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
    public float GetHealthPercentage() => currentHealth / maxHealth;
    public bool IsGameOver() => isGameOver;
}