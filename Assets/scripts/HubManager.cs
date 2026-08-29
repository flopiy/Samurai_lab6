using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class HubManager : MonoBehaviour
{
    [Header("Player and Trainer")]
    public GameObject player;
    public GameObject trainer;

    [Header("Dojo Door Setup")]
    public Vector2 dojoDoorPosition = new Vector2(0.3f, 5.1f);
    public float dojoDoorRadius = 1.2f;

    [Header("Dialogue Config")]
    public float talkDistance = 2.2f;

    [Header("UI Reference")]
    public Text statusText;
    public GameObject p1LanternsGroup;
    public GameObject p2LanternsGroup;

    [Header("Audio Setup")]
    public AudioSource sfxSource;
    public AudioClip gongSound;

    private bool isTransitioning = false;
    private bool spokeToTrainer = false;

    void Start()
    {
        // Ensure scoreboard lanterns are hidden in peaceful hub
        if (p1LanternsGroup != null) p1LanternsGroup.SetActive(false);
        if (p2LanternsGroup != null) p2LanternsGroup.SetActive(false);

        if (statusText != null)
        {
            statusText.text = "Walk around with left clicks. Talk to the Trainer.";
        }

        // Make sure player has PlayerMovement
        if (player != null)
        {
            var move = player.GetComponent<PlayerMovement>();
            if (move == null)
            {
                move = player.AddComponent<PlayerMovement>();
            }
            move.speed = 5f;
        }
    }

    void Update()
    {
        if (isTransitioning) return;

        if (player == null) return;

        Vector2 playerPos = player.transform.position;

        // 1. Check distance to trainer
        if (trainer != null)
        {
            float distToTrainer = Vector2.Distance(playerPos, trainer.transform.position);
            if (distToTrainer <= talkDistance)
            {
                if (statusText != null)
                {
                    statusText.text = "Trainer: 'Welcome, young Samurai! Go up the stairs to enter the Dojo and test your reflexes!'";
                }
                spokeToTrainer = true;
            }
            else if (spokeToTrainer)
            {
                // Reset to help message once player walks away
                if (statusText != null)
                {
                    statusText.text = "Go up the stairs and enter the Dojo doors at the top.";
                }
                spokeToTrainer = false;
            }
        }

        // 2. Check distance to Dojo Door
        float distToDoor = Vector2.Distance(playerPos, dojoDoorPosition);
        if (distToDoor <= dojoDoorRadius)
        {
            Debug.Log("[HubManager] Player inside dojo door radius! Distance: " + distToDoor + ", Triggering EnterDojo().");
            StartCoroutine(EnterDojo());
        }
    }

    IEnumerator EnterDojo()
    {
        isTransitioning = true;
        Debug.Log("[HubManager] EnterDojo coroutine started!");
        if (statusText != null)
        {
            statusText.text = "Entering the Dojo for Training...";
        }

        // Stop player movement
        if (player != null)
        {
            var move = player.GetComponent<PlayerMovement>();
            if (move != null) 
            {
                Debug.Log("[HubManager] Disabling PlayerMovement component.");
                move.enabled = false;
            }
        }

        // Play Gong sound
        if (sfxSource != null && gongSound != null)
        {
            Debug.Log("[HubManager] Playing gong sound: " + gongSound.name);
            sfxSource.PlayOneShot(gongSound);
        }
        else
        {
            Debug.LogWarning("[HubManager] Cannot play gong: sfxSource=" + (sfxSource != null) + ", gongSound=" + (gongSound != null));
        }

        Debug.Log("[HubManager] Yielding for 1.5 seconds...");
        yield return new WaitForSeconds(1.5f);

        Debug.Log("[HubManager] Loading Scene 2 (TrainingRoom) now!");
        SceneManager.LoadScene(2);
    }

    void OnDrawGizmosSelected()
    {
        // Visualize dojo door trigger area in Editor
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(new Vector3(dojoDoorPosition.x, dojoDoorPosition.y, 0f), dojoDoorRadius);
    }
}