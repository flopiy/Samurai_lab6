using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class SamuraiController : MonoBehaviour
{
    public enum PlayerRole { Attacker, Defender }
    public PlayerRole currentRole;

    [Header("Input Setup")]
    public string[] actionKeys = new string[] { "f", "space" };

    [Header("AI Config")]
    public bool isAIControlled = false;

    [Header("Components")]
    public Animator anim;
    public GameManager gameManager;

    private bool hasActed = false;
    private bool isWindingUp = false;
    private Vector3 originalPosition;
    private SpriteRenderer spriteRenderer;

    public bool IsWindingUp => isWindingUp;
    public bool HasActed => hasActed;

    void Awake()
    { 
        originalPosition = transform.position;
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void OnEnable()
    {
        ResetState();
    }

    void Update()
    {
        // If AI controlled, skip keyboard reading completely
        if (isAIControlled)
        {
            ApplyWindUpVibration();
            return;
        }

        if (Keyboard.current == null) return;
        if (gameManager != null && gameManager.IsRoundOver)
        {
            isWindingUp = false;
            return;
        }

        bool wasPressed = false;
        bool wasReleased = false;

        foreach (var keyName in actionKeys)
        {
            var key = Keyboard.current[keyName] as KeyControl;
            if (key != null)
            {
                if (key.wasPressedThisFrame) wasPressed = true;
                if (key.wasReleasedThisFrame) wasReleased = true;
            }
        }

        if (currentRole == PlayerRole.Attacker)
        {
            if (wasPressed && !isWindingUp)
            {
                TriggerWindUp();
            }

            if (wasReleased && isWindingUp)
            {
                TriggerStrike();
            }
        }
        else if (currentRole == PlayerRole.Defender && !hasActed)
        {
            if (wasPressed)
            {
                TriggerCatch();
            }
        }

        ApplyWindUpVibration();
    }

    public void TriggerWindUp()
    {
        if (currentRole != PlayerRole.Attacker || isWindingUp) return;
        isWindingUp = true;
        if (anim != null) anim.SetTrigger("WindUp");
        Debug.Log(gameObject.name + " Attacker: Wind-up started (holding)...");
    }

    public void TriggerStrike()
    {
        if (currentRole != PlayerRole.Attacker || !isWindingUp) return;
        isWindingUp = false;
        transform.position = originalPosition; // Reset vibration offset
        StartAttack();
    }

    public void TriggerCatch()
    {
        if (currentRole != PlayerRole.Defender || hasActed) return;
        hasActed = true;
        if (anim != null) anim.SetTrigger("Catch");
        if (gameManager != null) gameManager.OnCatchAttempt(this);
    }

    private void ApplyWindUpVibration()
    {
        // Apply slight vibration tension during wind up
        if (isWindingUp && currentRole == PlayerRole.Attacker)
        {
            float shakeMagnitude = 0.04f;
            transform.position = originalPosition + (Vector3)Random.insideUnitCircle * shakeMagnitude;
        }
    }

    void StartAttack()
    {
        if (anim != null) anim.SetTrigger("Strike");
        if (gameManager != null) gameManager.OnAttackInitiated();
    }

    public void ResetState()
    {
        hasActed = false;
        isWindingUp = false;
        transform.position = originalPosition;
        if (anim != null)
        {
            anim.ResetTrigger("WindUp");
            anim.ResetTrigger("Strike");
            anim.ResetTrigger("Catch");
            anim.Play("Idle", 0, 0f);
        }
    }
}
