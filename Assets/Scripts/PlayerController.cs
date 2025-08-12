using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    public float baseSpeed = 5f;
    public float jumpForce = 12f;

    [Header("Suelo")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.15f;
    public LayerMask groundLayer;

    [Header("Caída")]
    public bool useYKill = true;
    public float yKillHeight = -12f;

    [Header("Anim/Refs")]
    public Animator animator;
    public SpriteRenderer spriteRenderer;

    [Header("Disparo")]
    public Transform firePoint;
    public GameObject Bullet;
    public float fireRate = 6f;
    float nextFireTime;

    [Header("Límites dinámicos por cámara")]
    public bool limitByCamera = true;
    public float topMargin = 0.12f;
    public float bottomMargin = 0.3f;


    Rigidbody2D rb;
    bool isGrounded, isDead;
    float targetSpeed;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (!animator) animator = GetComponent<Animator>();
        if (!spriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        targetSpeed = GameManager.Instance ? GameManager.Instance.GetPlayerSpeed(baseSpeed) : baseSpeed;
    }

    void Update()
    {
        if (isDead) return;

        // Detección de suelo
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        animator.SetBool("isJumping", !isGrounded);

        // Input horizontal
        float x = Input.GetAxisRaw("Horizontal");
        rb.linearVelocityX = x * targetSpeed;             // <-- Unity 6
        animator.SetFloat("Speed", Mathf.Abs(rb.linearVelocityX));

        // Flip visual
        if (x != 0) spriteRenderer.flipX = x < 0;

        // Salto
        if (isGrounded && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)))
        {
            rb.linearVelocityY = 0f;                      // reset vertical antes del impulso
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        }

        // Disparo (J o click izq)
        if ((Input.GetKey(KeyCode.J) || Input.GetMouseButton(0)) && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + (1f / fireRate);
            Shoot();
        }

        // Muerte por caída
        if (useYKill && transform.position.y < yKillHeight)
            Die();

        if (limitByCamera)
        {
            var cam = Camera.main;
            if (cam)
            {
                float halfH = cam.orthographicSize;
                float topY = cam.transform.position.y + halfH - topMargin;
                float bottomY = cam.transform.position.y - halfH + bottomMargin;

                // techo suave: si se pasa, lo clavamos al borde y frenamos su velocidad vertical ascendente
                if (transform.position.y > topY)
                {
                    transform.position = new Vector3(transform.position.x, topY, transform.position.z);
                    if (rb.linearVelocityY > 0f) rb.linearVelocityY = 0f;
                }

                // (opcional) suelo visible: evita que salga de cámara por abajo sin caer a una FallZone
                if (transform.position.y < bottomY)
                {
                    transform.position = new Vector3(transform.position.x, bottomY, transform.position.z);
                    if (rb.linearVelocityY < 0f) rb.linearVelocityY = 0f;
                }
            }
        }
    }

    void Shoot()
    {
        if (!Bullet || !firePoint) return;

        Vector2 dir = (spriteRenderer && spriteRenderer.flipX) ? Vector2.left : Vector2.right;

        GameObject b = Instantiate(Bullet, firePoint.position, Quaternion.identity);
        var bullet = b.GetComponent<Bullet>();
        if (bullet) bullet.SetDirection(dir);

        // Anim de disparo si existe
        if (HasParam("isShooting")) { animator.SetBool("isShooting", true); Invoke(nameof(StopShootAnim), 0.12f); }
        else if (HasParam("Shoot")) { animator.SetTrigger("Shoot"); }

        // Evitar choque con el propio jugador
        var myCol = GetComponent<Collider2D>();
        var bCol = b.GetComponent<Collider2D>();
        if (myCol && bCol) Physics2D.IgnoreCollision(myCol, bCol, true);
    }

    bool HasParam(string p)
    {
        foreach (var prm in animator.parameters) if (prm.name == p) return true;
        return false;
    }

    void StopShootAnim() { if (animator) animator.SetBool("isShooting", false); }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead) return;
        if (other.CompareTag("Coin")) { GameManager.Instance?.AddCoin(1); Destroy(other.gameObject); }
        if (other.CompareTag("FallZone")) Die();
    }

    void OnCollisionEnter2D(Collision2D other)
    {
        if (isDead) return;
        if (other.collider.CompareTag("Enemy")) Die();
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;
        animator.SetBool("isDead", true);
        GameManager.Instance?.OnPlayerDied();
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck) Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}

