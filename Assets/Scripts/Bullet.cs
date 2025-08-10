using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Bullet : MonoBehaviour
{
    public float speed = 12f;
    public float lifetime = 2f;
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

    void FixedUpdate()
    {
        rb.linearVelocity = direction * speed; // <-- Unity 6
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) return;

        if (other.CompareTag("Enemy"))
        {
            var e = other.GetComponent<Enemy>();
            if (e) e.TakeDamage(damage);
            Despawn();
            return;
        }

        if (other.gameObject.layer == LayerMask.NameToLayer("Ground") || other.CompareTag("Ground"))
            Despawn();
    }

    void Despawn()
    {
        CancelInvoke();
        Destroy(gameObject);
    }
}

