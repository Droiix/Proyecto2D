using UnityEngine;
using System.Collections;
// enemigo que el jugador puede matar
// se destruye al recibir daño y puede hacer daño al jugador
// se desvanece y reduce de tamaño al morir
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
    // Inicializa el enemigo, asigna componentes y escala inicial
    // Configura la salud inicial y los componentes necesarios
    void Awake()
    {
        currentHealth = maxHealth;
        cols = GetComponentsInChildren<Collider2D>(true);
        rb = GetComponent<Rigidbody2D>();
        rends = GetComponentsInChildren<SpriteRenderer>(true);
        startScale = transform.localScale;
    }
    // Aplica daño al enemigo, si la salud llega a 0, inicia el proceso de muerte
    // Desactiva colisionadores y Rigidbody, desvanece el sprite y reduce el
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
    // Detecta colisiones con balas o al jugador
    // Si colisiona con una bala, aplica daño y destruye la bala
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
    // Detecta colisiones con el jugador o balas
    // Si colisiona con el jugador, lo mata
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
