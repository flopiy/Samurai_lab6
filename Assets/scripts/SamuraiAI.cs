using UnityEngine;
using System.Collections;

public class SamuraiAI : MonoBehaviour
{
    private SamuraiController controller;
    private SamuraiController opponent;
    private GameManager gameManager;

    private bool isRoundActive = false;
    private Coroutine aiBehaviorCoroutine;
    private Coroutine panicReactionCoroutine;

    void Awake()
    {
        controller = GetComponent<SamuraiController>();
    }

    void Start()
    {
        if (controller != null)
        {
            gameManager = controller.gameManager;
            if (gameManager != null)
            {
                opponent = (gameManager.player1 == controller) ? gameManager.player2 : gameManager.player1;
            }
        }
    }

    void Update()
    {
        if (gameManager == null || controller == null) return;

        // Sync AI controller state with game mode
        bool shouldBeAI = GameManager.vsAIMode && controller.gameObject.name == "Hero Knight";
        if (controller.isAIControlled != shouldBeAI)
        {
            controller.isAIControlled = shouldBeAI;
            if (!shouldBeAI)
            {
                StopAllCoroutines();
                isRoundActive = false;
                return;
            }
        }

        if (!controller.isAIControlled) return;

        // Check for round start transitions
        if (!gameManager.IsRoundOver && !isRoundActive)
        {
            // Fight just started!
            isRoundActive = true;
            if (aiBehaviorCoroutine != null) StopCoroutine(aiBehaviorCoroutine);
            aiBehaviorCoroutine = StartCoroutine(OnRoundStartedBehavior());
        }
        else if (gameManager.IsRoundOver && isRoundActive)
        {
            // Round over
            isRoundActive = false;
            StopAllCoroutines();
        }
    }

    IEnumerator OnRoundStartedBehavior()
    {
        // Wait a small moment for Fight prompt to clear
        yield return new WaitForSeconds(0.5f);

        if (controller.currentRole == SamuraiController.PlayerRole.Attacker)
        {
            // --- ATTACKER ROLE (Bluff & strike) ---
            // 1. Wait a random delay before wind-up
            float delayBeforeWindUp = Random.Range(1.2f, 3.2f);
            yield return new WaitForSeconds(delayBeforeWindUp);

            if (gameManager.IsRoundOver) yield break;

            // 2. Start wind-up (vibrating/bluffing)
            controller.TriggerWindUp();

            // 3. Hold wind-up (bluff) for a random duration to tense up the player
            float bluffDuration = Random.Range(0.6f, 2.0f);
            yield return new WaitForSeconds(bluffDuration);

            if (gameManager.IsRoundOver) yield break;

            // 4. Strike!
            controller.TriggerStrike();
        }
        else
        {
            // --- DEFENDER ROLE (Watch opponent) ---
            // The AI will wait and react to the player's attack.
            // Let's also start a panic timer to simulate the AI getting baited by player's wind-up.
            if (panicReactionCoroutine != null) StopCoroutine(panicReactionCoroutine);
            panicReactionCoroutine = StartCoroutine(WatchPlayerForPanic());
        }
    }

    IEnumerator WatchPlayerForPanic()
    {
        while (!gameManager.IsRoundOver && opponent != null)
        {
            // If the opponent (player) is winding up, there's a chance the AI panics and catches too early!
            if (opponent.IsWindingUp && !controller.HasActed)
            {
                // Panic threshold: random hold time between 0.8f and 2.2f.
                // If the player holds it longer than this, the AI gets baited and catches prematurely!
                float panicDelay = Random.Range(0.8f, 2.2f);
                yield return new WaitForSeconds(panicDelay);

                if (opponent.IsWindingUp && !controller.HasActed && !gameManager.IsRoundOver)
                {
                    Debug.Log("AI got baited by Player's bluff! Premature catch triggered!");
                    controller.TriggerCatch();
                }
                yield break;
            }
            yield return null;
        }
    }

    public void OnPlayerStrikeInitiated()
    {
        if (!controller.isAIControlled || controller.currentRole != SamuraiController.PlayerRole.Defender || controller.HasActed) return;

        // Calculate a human-like reaction time
        float roll = Random.value;
        float reactionDelay = 0.25f;

        if (roll < 0.65f)
        {
            // 65% chance of clean catch (between 0.2s and 0.3s)
            reactionDelay = Random.Range(gameManager.catchWindowStart + 0.01f, gameManager.catchWindowEnd - 0.01f);
        }
        else if (roll < 0.82f)
        {
            // 17% chance of premature/fast reaction (below 0.2s)
            reactionDelay = Random.Range(0.08f, gameManager.catchWindowStart - 0.02f);
        }
        else
        {
            // 18% chance of late reaction (above 0.3s)
            reactionDelay = Random.Range(gameManager.catchWindowEnd + 0.02f, 0.45f);
        }

        // Trigger the catch precisely at the reaction delay!
        StartCoroutine(ExecuteCatchWithDelay(reactionDelay));
    }

    IEnumerator ExecuteCatchWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (!gameManager.IsRoundOver && !controller.HasActed)
        {
            controller.TriggerCatch();
        }
    }
}