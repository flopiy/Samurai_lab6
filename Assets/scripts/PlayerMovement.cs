using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;
    public float attackRange = 1.5f; // Радіус атаки
    private Vector2 targetPosition;
    private bool isMoving = false;
    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            // Set up rigidbody for clean 2D top-down collisions
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
    }

    void Update()
    {
        // Рух (мишка)
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            SetTargetPosition();
        }

        // АТАКА (клавіша Space / Пробіл)
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            PerformAttack();
        }
    }

    void FixedUpdate()
    {
        if (isMoving)
        {
            Move();
        }
        else if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    void PerformAttack()
    {
        Debug.Log("Гравець атакує!");

        // 1. Знаходимо всі об'єкти в радіусі атаки
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, attackRange);

        // 2. Перевіряємо кожен об'єкт
        foreach (Collider2D enemy in hitEnemies)
        {
            // Шукаємо скрипт TrainingDummy на об'єкті
            TrainingDummy dummy = enemy.GetComponent<TrainingDummy>();
            
            if (dummy != null)
            {
                dummy.TakeDamage(); // Викликаємо метод отримання шкоди
            }
        }
    }

    // Для візуалізації радіусу атаки в редакторі
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }

    void SetTargetPosition()
    {
        if (Camera.main == null || Mouse.current == null)
        {
            return;
        }

        // Отримуємо координати миші в ігровому світі
        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        
        targetPosition = new Vector2(mouseWorldPosition.x, mouseWorldPosition.y);
        isMoving = true;
    }

    void Move()
    {
        Vector2 currentPos = transform.position;
        Vector2 nextPos = Vector2.MoveTowards(currentPos, targetPosition, speed * Time.fixedDeltaTime);

        if (rb != null)
        {
            rb.MovePosition(nextPos);
        }
        else
        {
            transform.position = nextPos;
        }

        // Зупиняємося, якщо майже дійшли
        if (Vector2.Distance(currentPos, targetPosition) < 0.08f)
        {
            isMoving = false;
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }
    }
}
