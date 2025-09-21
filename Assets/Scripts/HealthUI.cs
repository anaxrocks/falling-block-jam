using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class HealthUIController : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Text healthText;
    [SerializeField] private Image healthFill;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Button restartButton;

    [Header("Health Bar Colors")]
    [SerializeField] private Color healthyColor = Color.green;
    [SerializeField] private Color warningColor = Color.yellow;
    [SerializeField] private Color dangerColor = Color.red;
    [SerializeField] private float warningThreshold = 0.5f;
    [SerializeField] private float dangerThreshold = 0.25f;

    [Header("Animation Settings")]
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private bool enablePulseAnimation = true;

    private HealthManager healthSystem;
    private bool isLowHealth = false;
    private Coroutine pulseCoroutine;
    private Vector3 originalScale; // Store the original scale
    [SerializeField] private Text highScoreTxt;

    void Start()
    {
        // Store the original scale of the health slider
        if (healthSlider != null)
        {
            originalScale = healthSlider.transform.localScale;
        }

        // Find the health system
        healthSystem = FindFirstObjectByType<HealthManager>();

        if (healthSystem == null)
        {
            Debug.LogError("HealthSystem not found! Make sure it's attached to the player.");
            return;
        }

        // Subscribe to health events
        healthSystem.OnHealthChanged += UpdateHealthUI;
        healthSystem.OnPlayerDeath += ShowGameOverScreen;

        // Set up restart button
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(RestartGame);
        }

        // Initialize UI
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        UpdateHealthUI(1f); // Initialize at full health
    }

    void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        if (healthSystem != null)
        {
            healthSystem.OnHealthChanged -= UpdateHealthUI;
            healthSystem.OnPlayerDeath -= ShowGameOverScreen;
        }
    }

    private void UpdateHealthUI(float healthPercentage)
    {
        // Update slider value
        if (healthSlider != null)
        {
            healthSlider.value = healthPercentage;
        }

        // Update text
        if (healthText != null && healthSystem != null)
        {
            healthText.text = $"{Mathf.Ceil(healthSystem.GetCurrentHealth())}/{healthSystem.GetMaxHealth()}";
        }

        // Update health bar color
        UpdateHealthBarColor(healthPercentage);

        // Handle low health effects
        HandleLowHealthEffects(healthPercentage);
    }

    private void UpdateHealthBarColor(float healthPercentage)
    {
        if (healthFill == null) return;

        Color targetColor;

        if (healthPercentage > warningThreshold)
        {
            targetColor = healthyColor;
        }
        else if (healthPercentage > dangerThreshold)
        {
            targetColor = warningColor;
        }
        else
        {
            targetColor = dangerColor;
        }

        healthFill.color = targetColor;
    }

    private void HandleLowHealthEffects(float healthPercentage)
    {
        bool shouldPulse = healthPercentage <= dangerThreshold;

        if (shouldPulse && !isLowHealth)
        {
            // Start low health effects
            isLowHealth = true;
            if (enablePulseAnimation && pulseCoroutine == null)
            {
                pulseCoroutine = StartCoroutine(PulseHealthBar());
            }
        }
        else if (!shouldPulse && isLowHealth)
        {
            // Stop low health effects
            isLowHealth = false;
            if (pulseCoroutine != null)
            {
                StopCoroutine(pulseCoroutine);
                pulseCoroutine = null;
            }

            // Ensure scale is reset to original
            ResetHealthBarScale();
        }
    }

    private void ResetHealthBarScale()
    {
        if (healthSlider != null)
        {
            healthSlider.transform.localScale = originalScale;
        }
    }

    private IEnumerator PulseHealthBar()
    {
        float pulseAmount = 0.1f;

        while (isLowHealth)
        {
            // Scale up
            float time = 0f;
            while (time < 0.5f && isLowHealth) // Check isLowHealth to exit early if needed
            {
                time += Time.deltaTime * pulseSpeed;
                float scale = Mathf.Lerp(1f, 1f + pulseAmount, time * 2f);
                if (healthSlider != null)
                {
                    healthSlider.transform.localScale = originalScale * scale;
                }
                yield return null;
            }

            // Scale down
            time = 0f;
            while (time < 0.5f && isLowHealth) // Check isLowHealth to exit early if needed
            {
                time += Time.deltaTime * pulseSpeed;
                float scale = Mathf.Lerp(1f + pulseAmount, 1f, time * 2f);
                if (healthSlider != null)
                {
                    healthSlider.transform.localScale = originalScale * scale;
                }
                yield return null;
            }
        }

        // Final reset when exiting the loop
        ResetHealthBarScale();
    }

    private void ShowGameOverScreen()
    {
        ScoreKeeper scoreKeeper = GameObject.FindAnyObjectByType<ScoreKeeper>();
        if (scoreKeeper.score > GameBoardManager.Instance.highScore)
        {
            GameBoardManager.Instance.highScore = scoreKeeper.score;
        }
        highScoreTxt.text = GameBoardManager.Instance.highScore.ToString();
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        Debug.Log("Game Over! Health depleted.");
    }

    private void RestartGame()
    {
        if (healthSystem != null)
        {
            healthSystem.ResetHealth();
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        // Reset low health state
        isLowHealth = false;
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }

        // Ensure scale is reset
        ResetHealthBarScale();

        Debug.Log("Game restarted!");
    }

    public void ForceUpdateUI()
    {
        if (healthSystem != null)
        {
            UpdateHealthUI(healthSystem.GetHealthPercentage());
        }
    }

    public void RestartButton()
    {
        Debug.Log("Restart button pressed - initiating full game reset");

        // Reset health system first
        if (healthSystem != null)
        {
            healthSystem.ResetHealth();
        }

        // Hide game over panel
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        // Reset UI state
        isLowHealth = false;
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }
        ResetHealthBarScale();

        // Reset the entire game state through GameBoardManager
        if (GameBoardManager.Instance != null)
        {
            GameBoardManager.Instance.ResetGameState();
        }

        // Force update UI after reset
        StartCoroutine(ForceUpdateUIAfterFrame());
    }
    private IEnumerator ForceUpdateUIAfterFrame()
    {
        // Wait a frame to ensure everything is properly initialized
        yield return null;

        // Re-find health system if needed (in case objects were recreated)
        if (healthSystem == null)
        {
            healthSystem = FindFirstObjectByType<HealthManager>();
            if (healthSystem != null)
            {
                // Re-subscribe to events
                healthSystem.OnHealthChanged += UpdateHealthUI;
                healthSystem.OnPlayerDeath += ShowGameOverScreen;
            }
        }

        // Force update the UI
        ForceUpdateUI();
    }
}