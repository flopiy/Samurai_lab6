using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    [Header("Game Mode Config")]
    public static bool vsAIMode = true; // Default to single player vs AI for training

    [Header("Players")]
    public SamuraiController player1;
    public SamuraiController player2;

    [Header("UI")]
    public UIManager uiManager;
    public GameObject statsWindow;
    public Text statsDisplayText;

    [Header("Stats")]
    public StatsManager statsManager;

    [Header("Audio Setup")]
    public AudioSource musicSource;
    public AudioSource sfxSource;
    public AudioClip bgMusic;
    public AudioClip gongSound;
    public AudioClip wooshSound;
    public AudioClip tsingSound;

    [Header("Gameplay Settings")]
    public float catchWindowStart = 0.2f;
    public float catchWindowEnd = 0.3f;
    public int maxScore = 5;

    private int p1Score = 0;
    private int p2Score = 0;
    private float strikeStartTime;
    private bool isStrikeActive = false;
    private bool isRoundOver = false;
    private int currentRound = 0;

    public bool IsRoundOver => isRoundOver;

    void Start()
    {
        p1Score = 0;
        p2Score = 0;
        currentRound = 0;
        isRoundOver = false;

        if (statsWindow != null) statsWindow.SetActive(false);

        // Set up and start Background Music
        if (musicSource != null && bgMusic != null)
        {
            musicSource.clip = bgMusic;
            musicSource.loop = true;
            musicSource.Play();
        }

        // Play initial Gong sound
        PlaySFX(gongSound);

        // Round 1 Setup
        player1.currentRole = SamuraiController.PlayerRole.Attacker;
        player2.currentRole = SamuraiController.PlayerRole.Defender;

        UpdateUI();
        StartCoroutine(StartNewRound("READY..."));
    }

    public void OnAttackInitiated()
    {
        if (isRoundOver) return;
        strikeStartTime = Time.time;
        isStrikeActive = true;

        PlaySFX(wooshSound);

        // Notify AI if active and Player 2 is AI controlled
        if (vsAIMode && player2 != null && player2.isAIControlled)
        {
            var ai = player2.GetComponent<SamuraiAI>();
            if (ai != null)
            {
                ai.OnPlayerStrikeInitiated();
            }
        }

        // If defender does not react within the window, it's an auto hit
        Invoke("CheckAutoHit", catchWindowEnd + 0.05f);
    }

    public void OnCatchAttempt(SamuraiController defender)
    {
        if (isRoundOver) return;
        CancelInvoke("CheckAutoHit");

        if (isStrikeActive)
        {
            float reactionTime = Time.time - strikeStartTime;
            if (reactionTime >= catchWindowStart && reactionTime <= catchWindowEnd)
            {
                // Successful Catch!
                PlaySFX(tsingSound);
                CreateSparks();
                EndRound(defender, "BLADE CAUGHT!", true);
            }
            else
            {
                // Too early or too late
                EndRound(GetOpponent(defender), "SLASHED!", false);
            }
        }
        else
        {
            // Too early (premature Catch attempt)
            EndRound(GetOpponent(defender), "PREMATURE CATCH!", false);
        }
    }

    void CheckAutoHit()
    {
        if (isStrikeActive && !isRoundOver)
        {
            SamuraiController attacker = (player1.currentRole == SamuraiController.PlayerRole.Attacker) ? player1 : player2;
            EndRound(attacker, "TOO LATE! SLASHED!", false);
        }
    }

    void EndRound(SamuraiController winner, string message, bool isCatch)
    {
        isRoundOver = true;
        isStrikeActive = false;
        CancelInvoke("CheckAutoHit");

        if (winner == player1) p1Score++;
        else p2Score++;

        uiManager.ShowStatus(message);
        UpdateUI();

        // Visual effects for end of round
        if (isCatch)
        {
            // Freeze both characters in epic pose
            if (player1.anim != null) player1.anim.speed = 0f;
            if (player2.anim != null) player2.anim.speed = 0f;
        }
        else
        {
            // Defender collapses
            SamuraiController loser = (winner == player1) ? player2 : player1;
            StartCoroutine(DefenderCollapse(loser));
        }

        if (p1Score >= maxScore || p2Score >= maxScore)
        {
            PlaySFX(gongSound);
            string winnerName = (p1Score >= maxScore) ? "PLAYER 1" : "PLAYER 2";
            uiManager.ShowStatus(winnerName + " WINS THE MATCH!");
            Invoke("ShowEndGameUI", 3f);
        }
        else
        {
            StartCoroutine(StartNewRound("READY..."));
        }
    }

    IEnumerator DefenderCollapse(SamuraiController defender)
    {
        float duration = 0.4f;
        float elapsed = 0f;
        Quaternion startRot = defender.transform.rotation;
        
        // Collapse: Rotate by 90 degrees on Z axis
        float angle = (defender.transform.position.x < 46f) ? 90f : -90f;
        Quaternion endRot = Quaternion.Euler(0, 0, angle);
        
        SpriteRenderer sr = defender.GetComponent<SpriteRenderer>();
        Color startColor = sr != null ? sr.color : Color.white;
        Color endColor = Color.gray;

        while (elapsed < duration)
        { 
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            defender.transform.rotation = Quaternion.Slerp(startRot, endRot, t);
            if (sr != null) sr.color = Color.Lerp(startColor, endColor, t);
            yield return null;
        }
    }

    void CreateSparks()
    {
        Vector3 pos = (player1.transform.position + player2.transform.position) / 2f;
        pos.y += 0.5f;
        GameObject sparksGo = new GameObject("Sparks", typeof(ParticleSystem));
        sparksGo.transform.position = pos;
        ParticleSystem ps = sparksGo.GetComponent<ParticleSystem>();
        
        // Stop it first so we can safely edit duration and main properties without triggering Unity warnings/errors
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        
        var main = ps.main;
        main.startColor = new ParticleSystem.MinMaxGradient(Color.yellow, Color.white);
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 6f);
        main.gravityModifier = 1.5f;
        main.stopAction = ParticleSystemStopAction.Destroy;
        main.duration = 0.5f;
        main.loop = false;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.burstCount = 1;
        var burst = new ParticleSystem.Burst(0f, 35);
        emission.SetBursts(new ParticleSystem.Burst[] { burst });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.25f;

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        
        ps.Play();
    }

    void PlaySFX(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    void ShowEndGameUI()
    {
        if (statsManager != null) statsManager.AddWin();
        if (statsWindow != null) statsWindow.SetActive(true);
        
        if (statsDisplayText != null && statsManager != null)
        { 
            statsDisplayText.text = "LEVEL: " + statsManager.level + "\n" +
                                    "WINS: " + statsManager.wins + "\n" +
                                    "EXP: " + statsManager.experience + "/100";
        }
    }

    public void LoadNextLocation()
    {
        // Go back to the main hub scene (0.unity which is buildIndex 1)
        SceneManager.LoadScene(1);
    }

    public void PlayAgain()
    {
        // Play fresh Gong Sound
        PlaySFX(gongSound);

        // Reset match scores and variables
        p1Score = 0;
        p2Score = 0;
        currentRound = 0;
        isRoundOver = false;
        isStrikeActive = false;
        CancelInvoke("CheckAutoHit");

        if (statsWindow != null) statsWindow.SetActive(false);

        // Reset player positions, rotations, colors, animators, and scripts
        player1.ResetState();
        player2.ResetState();

        player1.transform.rotation = Quaternion.identity;
        player2.transform.rotation = Quaternion.identity;
        player1.anim.speed = 1f;
        player2.anim.speed = 1f;

        SpriteRenderer sr1 = player1.GetComponent<SpriteRenderer>();
        SpriteRenderer sr2 = player2.GetComponent<SpriteRenderer>();
        if (sr1 != null) sr1.color = Color.white;
        if (sr2 != null) sr2.color = Color.white;

        // Reset standard roles for Round 1
        player1.currentRole = SamuraiController.PlayerRole.Attacker;
        player2.currentRole = SamuraiController.PlayerRole.Defender;

        UpdateUI();
        StopAllCoroutines();
        StartCoroutine(StartNewRound("READY..."));
    }

    IEnumerator StartNewRound(string msg)
    {
        isRoundOver = true;
        yield return new WaitForSeconds(2f);

        currentRound++;
        if (currentRound > 1)
        {
            var p1OldRole = player1.currentRole;
            player1.currentRole = (p1OldRole == SamuraiController.PlayerRole.Attacker) ? SamuraiController.PlayerRole.Defender : SamuraiController.PlayerRole.Attacker;
            player2.currentRole = (player1.currentRole == SamuraiController.PlayerRole.Attacker) ? SamuraiController.PlayerRole.Defender : SamuraiController.PlayerRole.Attacker;
        }

        player1.ResetState();
        player2.ResetState();

        player1.transform.rotation = Quaternion.identity;
        player2.transform.rotation = Quaternion.identity;
        player1.anim.speed = 1f;
        player2.anim.speed = 1f;

        SpriteRenderer sr1 = player1.GetComponent<SpriteRenderer>();
        SpriteRenderer sr2 = player2.GetComponent<SpriteRenderer>();
        if (sr1 != null) sr1.color = Color.white;
        if (sr2 != null) sr2.color = Color.white;

        uiManager.ShowStatus(msg);
        yield return new WaitForSeconds(1.2f);

        string p1RoleStr = player1.currentRole == SamuraiController.PlayerRole.Attacker ? "ATTACKER" : "DEFENDER";
        string p2RoleStr = player2.currentRole == SamuraiController.PlayerRole.Attacker ? "ATTACKER" : "DEFENDER";
        uiManager.ShowStatus("P1: " + p1RoleStr + "   P2: " + p2RoleStr);
        yield return new WaitForSeconds(1.5f);

        uiManager.ShowStatus("FIGHT!");
        yield return new WaitForSeconds(0.8f);

        uiManager.ShowStatus("");
        isRoundOver = false;
    }

    SamuraiController GetOpponent(SamuraiController p) => (p == player1) ? player2 : player1;
    void UpdateUI() { if (uiManager != null) uiManager.UpdateScore(p1Score, p2Score); }
}
