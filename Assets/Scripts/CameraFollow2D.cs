using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    public Vector2 offset = new Vector2(0f, 0f);

    [Header("Suavizado")]
    public float smoothTimeX = 0.18f;
    public float smoothTimeY = 0.25f;

    [Header("Zona muerta vertical")]
    [Tooltip("Altura (en unidades) alrededor del centro de cámara en la que NO sigue al jugador verticalmente.")]
    public float verticalDeadZone = 3.0f;

    [Header("Límites (opcional)")]
    public bool clampY = false;
    public float minY = -10f;
    public float maxY = 10f;

    [Header("Look-ahead (opcional)")]
    public float lookAheadX = 1.2f;   // cuánto se adelanta en X según dirección
    public float lookAheadSmoothing = 8f;

    [Header("Estilo Mario")]
    [Tooltip("Si está activado, la cámara NO sigue en Y (queda fija a su altura inicial).")]
    public bool lockYToStart = true;

    Vector3 vel;          // para SmoothDamp
    float currentLookAhead;
    float lockedY;        // altura fija cuando lockYToStart = true

    void Start()
    {
        // Guardamos la Y inicial para el modo bloqueado
        lockedY = transform.position.y;
    }

    void LateUpdate()
    {
        if (!target) return;

        // ---- X con look-ahead suave ----
        float dir = Mathf.Sign(Input.GetAxisRaw("Horizontal"));
        float targetLook = (Mathf.Abs(dir) > 0.01f) ? dir * lookAheadX : 0f;
        currentLookAhead = Mathf.Lerp(currentLookAhead, targetLook, Time.deltaTime * lookAheadSmoothing);

        float desiredX = target.position.x + offset.x + currentLookAhead;
        float newX = Mathf.SmoothDamp(transform.position.x, desiredX, ref vel.x, smoothTimeX);

        // ---- Y: bloqueada o con zona muerta ----
        float newY;

        if (lockYToStart)
        {
            // Cámara fija en Y (estilo Mario)
            newY = lockedY;
        }
        else
        {
            // Seguir en Y con dead-zone
            float camY = transform.position.y;
            float targetY = target.position.y + offset.y;

            newY = camY;
            float deltaY = targetY - camY;

            if (Mathf.Abs(deltaY) > verticalDeadZone)
            {
                float edgeY = camY + Mathf.Sign(deltaY) * verticalDeadZone;
                float followTo = Mathf.Lerp(edgeY, targetY, 0.6f);
                newY = Mathf.SmoothDamp(camY, followTo, ref vel.y, smoothTimeY);
            }

            if (clampY) newY = Mathf.Clamp(newY, minY, maxY);
        }

        transform.position = new Vector3(newX, newY, -10f);
    }
}
