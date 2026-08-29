using UnityEngine;

public class StatsManager : MonoBehaviour
{
    public int level = 1;
    public int experience = 0;
    public int wins = 0;

    public float reactionBonus = 0f;
    public float bluffDuration = 1f;

    private void Awake()
    {
        // Це дозволяє об'єкту вижити при зміні сцени
        DontDestroyOnLoad(this.gameObject);
    }

    public void AddWin()
    {
        wins++;
        experience += 50;
        if (experience >= level * 100) LevelUp();
    }

    void LevelUp()
    {
        level++;
        experience = 0;
        reactionBonus += 0.01f;
        Debug.Log("Новий рівень майстерності: " + level);
    }
}