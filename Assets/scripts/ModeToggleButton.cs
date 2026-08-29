using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ModeToggleButton : MonoBehaviour
{
    private Button button;
    private Text text;

    void Awake()
    {
        button = GetComponent<Button>();
        text = GetComponentInChildren<Text>();
    }

    void Start()
    {
        if (button != null)
        {
            button.onClick.AddListener(OnToggleMode);
        }
        UpdateVisuals();
    }

    void OnEnable()
    {
        UpdateVisuals();
    }

    void OnToggleMode()
    {
        GameManager.vsAIMode = !GameManager.vsAIMode;
        UpdateVisuals();
        Debug.Log("Game Mode switched! vsAIMode = " + GameManager.vsAIMode);
    }

    public void UpdateVisuals()
    {
        if (text == null || button == null) return;

        var img = button.GetComponent<Image>();

        if (GameManager.vsAIMode)
        {
            text.text = "⚔️ MODE: PLAYER VS AI";
            if (img != null) img.color = new Color(0.12f, 0.35f, 0.12f, 0.9f); // Dark Emerald Green
        }
        else
        {
            text.text = "👥 MODE: LOCAL 2 PLAYERS";
            if (img != null) img.color = new Color(0.45f, 0.12f, 0.12f, 0.9f); // Dark Crimson Red
        }
    }
}