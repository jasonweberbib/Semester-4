using UnityEngine;
using TMPro; // Required for TextMeshPro
using UnityEngine.UI; // Required for UI elements like Button
using Unity.Netcode; // Required for network interaction

public class PlayerUIManagerr : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI scoreText;
    public Button spawnPillarButton;
    public Button spawnBallButton;

    // Reference to the NetPlayer script that owns this UI
    private NetPlayer ownerPlayer;

    /// <summary>
    /// Initializes the UI manager with the owning player's NetPlayer script.
    /// This should be called immediately after the UI is instantiated.
    /// </summary>
    /// <param name="player">The NetPlayer instance that owns this UI.</param>
    public void Initialize(NetPlayer player)
    {
        ownerPlayer = player;

        // Set up button click listeners
        if (spawnPillarButton != null)
        {
            spawnPillarButton.onClick.AddListener(OnSpawnPillarButtonClicked);
        }
        if (spawnBallButton != null)
        {
            spawnBallButton.onClick.AddListener(OnSpawnBallButtonClicked);
        }

        // Subscribe to the NetworkVariable's value change event
        if (ownerPlayer != null)
        {
            ownerPlayer.score.OnValueChanged += UpdateScoreUI;
            // Perform an initial UI update to display the current score immediately
            UpdateScoreUI(0, ownerPlayer.score.Value); // oldScore is dummy, newScore is actual
        }
        else
        {
            Debug.LogError("PlayerUIManagerr: Owner NetPlayer is null during initialization!");
        }
    }

    /// <summary>
    /// Called when the GameObject is destroyed. Used for cleanup to prevent memory leaks.
    /// </summary>
    void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks if the ownerPlayer still exists
        if (ownerPlayer != null)
        {
            ownerPlayer.score.OnValueChanged -= UpdateScoreUI;
        }
        // Remove button listeners
        if (spawnPillarButton != null)
        {
            spawnPillarButton.onClick.RemoveListener(OnSpawnPillarButtonClicked);
        }
        if (spawnBallButton != null)
        {
            spawnBallButton.onClick.RemoveListener(OnSpawnBallButtonClicked);
        }
    }

    /// <summary>
    /// Updates the score display text. This method is called automatically when 'score' changes.
    /// </summary>
    /// <param name="oldScore">The previous score value.</param>
    /// <param name="newScore">The current score value.</param>
    private void UpdateScoreUI(int oldScore, int newScore)
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + newScore.ToString();
        }
        // Optionally, update button interactivity based on score
        UpdateSpawnButtonStates(newScore);
    }

    /// <summary>
    /// Updates the enabled state of the spawn buttons based on the current score.
    /// </summary>
    /// <param name="currentScore">The player's current score.</param>
    private void UpdateSpawnButtonStates(int currentScore)
    {
        if (spawnPillarButton != null && ownerPlayer != null)
        {
            // Enable pillar button only if score is sufficient
            spawnPillarButton.interactable = currentScore >= ownerPlayer.pillerscore;
            // Optionally, update text on button to show cost
            spawnPillarButton.GetComponentInChildren<TextMeshProUGUI>().text = $"Spawn Pillar ({ownerPlayer.pillerscore})";
        }
        if (spawnBallButton != null && ownerPlayer != null)
        {
            // Enable ball button only if score is sufficient
            spawnBallButton.interactable = currentScore >= ownerPlayer.ballscore;
            // Optionally, update text on button to show cost
            spawnBallButton.GetComponentInChildren<TextMeshProUGUI>().text = $"Spawn Ball ({ownerPlayer.ballscore})";
        }
    }


    /// <summary>
    /// Called when the "Spawn Pillar" button is clicked.
    /// </summary>
    private void OnSpawnPillarButtonClicked()
    {
        if (ownerPlayer != null)
        {
            ownerPlayer.SpawnPillarServerRpc(); // Call the specific RPC for spawning a pillar
        }
    }

    /// <summary>
    /// Called when the "Spawn Ball" button is clicked.
    /// </summary>
    private void OnSpawnBallButtonClicked()
    {
        if (ownerPlayer != null)
        {
            ownerPlayer.SpawnBallServerRpc(); // Call the specific RPC for spawning a ball
        }
    }
}