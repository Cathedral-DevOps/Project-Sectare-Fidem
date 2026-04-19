using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
/// <summary>
/// Scene manager that loads the next scene when player reaches a checkpoint.
/// Attach to a checkpoint object (e.g., a trigger zone or the checkpoint itself).
/// </summary>
public class CheckpointSceneManager : MonoBehaviour
{
    [Header("Checkpoint Settings")]
    public string playerTag = "Player";
    public float interactionDistance = 3f;
    public bool useTrigger = true;  // If true, uses trigger collider; if false, uses distance check

    [Header("Scene Settings")]
    public int nextSceneIndex = -1;  // -1 = load next scene in build order
    public string nextSceneName = ""; // Alternative: specify scene name

    [Header("Debug")]
    public bool showGizmos = true;

    // State
    private bool hasTriggered = false;
    private Transform playerTransform;

    private void Start()
    {
        // Find the player
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null)
        {
            playerTransform = player.transform;
        }
        else
        {
            Debug.LogWarning($"Checkpoint: No object with tag '{playerTag}' found.");
        }
    }

    private void Update()
    {
        // Skip if already triggered or no player found
        if (hasTriggered || playerTransform == null) return;

        // Check if player is in range
        if (IsPlayerInRange())
        {
            LoadNextScene();
        }
    }

    /// <summary>
    /// Check if player is within interaction distance of the checkpoint.
    /// </summary>
    private bool IsPlayerInRange()
    {
        float distance = Vector3.Distance(playerTransform.position, transform.position);
        return distance <= interactionDistance;
    }

    /// <summary>
    /// Load the next scene based on settings.
    /// </summary>
    private void LoadNextScene()
    {
        hasTriggered = true;

        string sceneToLoad = "";

        // Determine which scene to load
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            sceneToLoad = nextSceneName;
        }
        else if (nextSceneIndex >= 0)
        {
            sceneToLoad = nextSceneIndex.ToString();
        }
        else
        {
            // Load next scene in build order
            int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
            int targetIndex = currentSceneIndex + 1;

            // Check if we're at the last scene
            if (targetIndex >= SceneManager.sceneCountInBuildSettings)
            {
                Debug.LogWarning("Already at last scene. Cannot load next scene.");
                hasTriggered = false; // Allow retry
                return;
            }

            sceneToLoad = targetIndex.ToString();
        }

        Debug.Log($"Loading scene: {sceneToLoad}");
        SceneManager.LoadScene(sceneToLoad);
    }

    // Draw gizmo in editor to show interaction range
    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);

        // Draw line to player if in editor
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null)
        {
            float distance = Vector3.Distance(player.transform.position, transform.position);
            Gizmos.color = distance <= interactionDistance ? Color.green : Color.red;
            Gizmos.DrawLine(transform.position, player.transform.position);
        }
    }
}

/// <summary>
/// Alternative: Trigger-based checkpoint that activates when player enters collider.
/// Use this version if you want the checkpoint to be a physical object the player walks into.
/// </summary>
public class CheckpointTrigger : MonoBehaviour
{
    [Header("Checkpoint Settings")]
    public string playerTag = "Player";
    public int nextSceneIndex = -1;
    public string nextSceneName = "";

    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;

        if (other.CompareTag(playerTag))
        {
            LoadNextScene();
        }
    }

    private void LoadNextScene()
    {
        hasTriggered = true;

        string sceneToLoad = "";

        if (!string.IsNullOrEmpty(nextSceneName))
        {
            sceneToLoad = nextSceneName;
        }
        else if (nextSceneIndex >= 0)
        {
            sceneToLoad = nextSceneIndex.ToString();
        }
        else
        {
            int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
            int targetIndex = currentSceneIndex + 1;

            if (targetIndex >= SceneManager.sceneCountInBuildSettings)
            {
                Debug.LogWarning("Already at last scene.");
                hasTriggered = false;
                return;
            }

            sceneToLoad = targetIndex.ToString();
        }

        Debug.Log($"Checkpoint reached! Loading: {sceneToLoad}");
        SceneManager.LoadScene(sceneToLoad);
    }
}

/*
 * SUMMARY: This script provides checkpoint-based scene management for Unity games.
 * CheckpointSceneManager uses distance-based detection to trigger scene transitions when the player approaches.
 * CheckpointTrigger uses Unity's trigger system for collision-based scene loading.
 * Configure nextSceneIndex or nextSceneName to specify which scene to load, or leave blank for auto-next.
 */