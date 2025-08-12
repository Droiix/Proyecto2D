using System.Collections;
using UnityEngine;

public class ItemSpawner2D : MonoBehaviour
{
    [Header("Prefabs (1 o más)")]
    public GameObject[] itemPrefabs;

    [Header("Ritmo")]
    public float minTime = 1.0f;
    public float maxTime = 2.0f;

    [Header("Marco de cámara")]
    public bool confineToCamera = true;
    public float cameraXInset = 1.0f;     // margen lateral dentro del frame

    [Header("Ajuste a suelo")]
    public LayerMask groundLayer;
    public float maxRaycastDown = 20f;

    [Header("Validación")]
    public bool avoidOverlap = true;
    public float overlapRadius = 0.25f;

    [Header("Evitar al jugador")]
    public Transform player;
    public float minDistanceFromPlayerX = 1.5f;

    [Header("Debug")]
    public bool logReasons = false;

    Camera cam;
    Coroutine loop;

    void Awake()
    {
        cam = Camera.main;
        if (!player && GameManager.Instance) player = GameManager.Instance.GetPlayer();
    }

    void OnEnable()
    {
        if (loop == null) loop = StartCoroutine(SpawnLoop(0f));
    }

    void OnDisable()
    {
        if (loop != null) StopCoroutine(loop);
        loop = null;
    }

    IEnumerator SpawnLoop(float initialDelay)
    {
        if (initialDelay > 0f) yield return new WaitForSeconds(initialDelay);

        while (true)
        {
            float wait = Random.Range(minTime, maxTime);
            yield return new WaitForSeconds(wait);
            TrySpawnOne();
        }
    }

    void TrySpawnOne()
    {
        if (itemPrefabs == null || itemPrefabs.Length == 0) { if (logReasons) Debug.LogWarning("[ItemSpawner2D] Sin prefabs."); return; }

        float x, rayStartY;

        if (confineToCamera && cam)
        {
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;

            float left = cam.transform.position.x - halfW + cameraXInset;
            float right = cam.transform.position.x + halfW - cameraXInset;

            x = Random.Range(left, right);

            if (player && minDistanceFromPlayerX > 0f)
            {
                int guard = 0;
                while (Mathf.Abs(x - player.position.x) < minDistanceFromPlayerX && guard++ < 10)
                    x = Random.Range(left, right);
                if (guard >= 10 && logReasons) Debug.Log("[ItemSpawner2D] Reubicado por cercanía al player.");
            }

            rayStartY = cam.transform.position.y + halfH + 2f;
        }
        else
        {
            x = transform.position.x + Random.Range(-2f, 2f);
            rayStartY = transform.position.y + 10f;
        }

        // Raycast hacia abajo para apoyar en plataforma (groundLayer debe ser Ground)
        RaycastHit2D hit = Physics2D.Raycast(new Vector2(x, rayStartY), Vector2.down, maxRaycastDown, groundLayer);
        if (!hit) { if (logReasons) Debug.Log("[ItemSpawner2D] Sin suelo debajo en groundLayer."); return; }

        // Elegir prefab
        GameObject pick = itemPrefabs[Random.Range(0, itemPrefabs.Length)];
        if (!pick) { if (logReasons) Debug.Log("[ItemSpawner2D] Prefab nulo."); return; }

        // Altura automática usando el collider del prefab
        float autoYOffset = 0.1f;
        var col = pick.GetComponentInChildren<Collider2D>();
        if (col != null) autoYOffset = Mathf.Max(0.05f, col.bounds.extents.y);

        Vector3 spawnPos = new Vector3(x, hit.point.y + autoYOffset, 0f);

        if (avoidOverlap && Physics2D.OverlapCircle(spawnPos, overlapRadius))
        {
            if (logReasons) Debug.Log("[ItemSpawner2D] Cancelado por solape (OverlapCircle).");
            return;
        }

        Instantiate(pick, spawnPos, Quaternion.identity);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!confineToCamera || Camera.main == null) return;
        var c = Camera.main;
        float halfH = c.orthographicSize;
        float halfW = halfH * c.aspect;

        Vector3 bl = new Vector3(c.transform.position.x - halfW + cameraXInset,
                                 c.transform.position.y - halfH, 0f);
        Vector3 tr = new Vector3(c.transform.position.x + halfW - cameraXInset,
                                 c.transform.position.y + halfH, 0f);

        Gizmos.color = new Color(1f, 1f, 0f, 0.25f);
        Gizmos.DrawWireCube((bl + tr) * 0.5f, new Vector3(tr.x - bl.x, tr.y - bl.y, 0f));
    }
#endif
}
