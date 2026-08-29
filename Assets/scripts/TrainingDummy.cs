using UnityEngine;
using System.Collections;

public class TrainingDummy : MonoBehaviour
{
    public Transform player; // ���� ��������� ������ � ���������
    public Color hitColor = Color.red; // ���� ��� ����
    private Color originalColor;
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    void Update()
    {
        if (player != null)
        {
            LookAtPlayer();
        }
    }

    void LookAtPlayer()
    {
        // ���� ������� �������� � ���������� ������� ��������, � �������
        if (player.position.x > transform.position.x)
        {
            transform.localScale = new Vector3(4, 4, 4); // �������� ������
        }
        else
        {
            transform.localScale = new Vector3(-4, 4, 4); // �������� ���� (�������������)
        }
    }

    // �����, ���� �� ������������� � ���� ������, ���� �� �'�
    public void TakeDamage()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        StopAllCoroutines();
        StartCoroutine(FlashEffect());
        Debug.Log("������� ������� ����!");
    }

    IEnumerator FlashEffect()
    {
        spriteRenderer.color = hitColor;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = originalColor;
    }
}