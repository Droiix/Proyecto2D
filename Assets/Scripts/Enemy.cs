using UnityEngine;

public class Enemy : MonoBehaviour
{
    public int maxHealth = 1;
    int currentHealth;

    void Awake() { currentHealth = maxHealth; }

    public void TakeDamage(int dmg)
    {
        currentHealth -= dmg;
        if (currentHealth <= 0) Destroy(gameObject);
    }

    void OnCollisionEnter2D(Collision2D other)
    {
        if (other.collider.CompareTag("Player"))
        {
            var p = other.collider.GetComponent<PlayerController>();
            if (p) p.Die();
        }
    }
}

