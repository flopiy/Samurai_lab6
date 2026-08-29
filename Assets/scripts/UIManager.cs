using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public Image[] p1Lanterns;
    public Image[] p2Lanterns;
    public Text statusText;
    public Sprite lanternOff;
    public Sprite lanternOn;

    public void UpdateScore(int p1Score, int p2Score)
    {
        for (int i = 0; i < p1Lanterns.Length; i++)
        {
            if (i < p1Score) p1Lanterns[i].sprite = lanternOn;
            else p1Lanterns[i].sprite = lanternOff;
        }

        for (int i = 0; i < p2Lanterns.Length; i++)
        {
            if (i < p2Score) p2Lanterns[i].sprite = lanternOn;
            else p2Lanterns[i].sprite = lanternOff;
        }
    }

    public void ShowStatus(string message)
    {
        if (statusText != null) statusText.text = message;
    }
}   