using UnityEngine;
using System.Collections;

public class Enemy : MonoBehaviour
{
    public int maxHealth = 1;
    public float fadeDuration = 0.35f;
    public float shrinkFactor = 0.1f;

    int currentHealth;
    bool dying;
    Collider2D[] cols;
    Rigidbody2D rb;
    SpriteRenderer[] rends;
    Vector3 startScale;

    void Awake()
    {
        currentHealth = maxHealth;
        cols = GetComponentsInChildren<Collider2D>(true);
        rb = GetComponent<Rigidbody2D>();
        rends = GetComponentsInChildren<SpriteRenderer>(true);
        startScale = transform.localScale;
    }

    public void TakeDamage(int dmg)
    {
        if (dying) return;
        currentHealth -= dmg;
        if (currentHealth <= 0) StartCoroutine(FadeAndDie());
    }

    IEnumerator FadeAndDie()
    {
        dying = true;
        foreach (var c in cols) if (c) c.enabled = false;
        if (rb) { rb.linearVelocity = Vector2.zero; rb.bodyType = RigidbodyType2D.Kinematic; rb.simulated = false; }


        Color[] start = new Color[rends.Length];
        for (int i = 0; i < rends.Length; i++) if (rends[i]) start[i] = rends[i].color;

        float t = 0f;
        while (t < fadeDuration)
        {
            float k = t / fadeDuration;
            float a = Mathf.Lerp(1f, 0f, k);
            float s = Mathf.Lerp(1f, shrinkFactor, k);

            for (int i = 0; i < rends.Length; i++)
            {
                if (!rends[i]) continue;
                var c = start[i]; c.a = a; rends[i].color = c;
            }
            transform.localScale = startScale * s;

            t += Time.deltaTime;
            yield return null;
        }
        Destroy(gameObject);
    }

    void OnCollisionEnter2D(Collision2D other)
    {
        if (dying) return;

        var bullet = other.collider.GetComponent<Bullet>();
        if (bullet) { TakeDamage(bullet.damage); Destroy(bullet.gameObject); return; }

        if (other.collider.CompareTag("Player"))
        {
            var p = other.collider.GetComponent<PlayerController>();
            if (p) p.Die();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (dying) return;

        var bullet = other.GetComponent<Bullet>();
        if (bullet) { TakeDamage(bullet.damage); Destroy(bullet.gameObject); return; }

        if (other.CompareTag("Player"))
        {
            var p = other.GetComponent<PlayerController>();
            if (p) p.Die();
        }
    }
}
