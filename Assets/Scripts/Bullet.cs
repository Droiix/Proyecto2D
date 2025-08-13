using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
//Scripts para las balas del jugador
// y enemigos, que se encargan de moverse, detectar colisiones y aplicar daño.
// Las balas se destruyen después de un tiempo o al colisionar con un enemigo
public class Bullet : MonoBehaviour
{
    // Velocidad de la bala, tiempo de vida y daño que inflige
    public float speed = 12f;
    // Tiempo de vida de la bala antes de ser destruida
    public float lifetime = 2f;
    // Daño que inflige la bala al enemigo
    public int damage = 1;

    Vector2 direction = Vector2.right;
    Rigidbody2D rb;

    void Awake() { rb = GetComponent<Rigidbody2D>(); }
    void OnEnable() { Invoke(nameof(Despawn), lifetime); }

    public void SetDirection(Vector2 dir)
    {
        direction = dir.normalized;
        if (direction.x < 0) transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
    }

    void FixedUpdate() { rb.linearVelocity = direction * speed; }

    void OnTriggerEnter2D(Collider2D other) { HandleHit(other); }
    void OnCollisionEnter2D(Collision2D col) { HandleHit(col.collider); }

    void HandleHit(Collider2D other)
    {
        if (other == null) return;
        if (other.CompareTag("Player")) return;

        var enemy = other.GetComponentInParent<Enemy>();
        if (enemy)
        {
            enemy.TakeDamage(damage);
            Despawn();
            return;
        }

        if (other.gameObject.layer == LayerMask.NameToLayer("Ground") || other.CompareTag("Ground"))
        {
            Despawn();
            return;
        }
    }

    void Despawn()
    {
        CancelInvoke();
        Destroy(gameObject);
    }
}
