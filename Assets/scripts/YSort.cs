using UnityEngine;

public class YSort : MonoBehaviour
{
    private SpriteRenderer sr;
    public int baseOrder = 100;
    public float offset = 0f;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void LateUpdate()
    {
        if (sr != null)
        {
            // Lower Y positions are drawn in front (higher sorting order)
            // Multiply by 100 to get fine-grained sorting
            sr.sortingOrder = baseOrder - Mathf.RoundToInt((transform.position.y + offset) * 10f);
        }
    }
}