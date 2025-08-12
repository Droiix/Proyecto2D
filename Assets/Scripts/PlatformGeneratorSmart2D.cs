using UnityEngine;
using System.Collections.Generic;

public class PlatformGeneratorSmart2D : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Transform initialPlatform;
    [SerializeField] private GameObject plataforma;

    [SerializeField] private LayerMask groundLayer = 1 << 0;

    [SerializeField] private bool spawnCoins = true;
    [SerializeField] private GameObject moneda;
    [Range(0f, 1f)][SerializeField] private float coinChance = 0.6f;
    [SerializeField] private float coinExtraLift = 0.02f;

    [SerializeField] private bool spawnEnemies = true;
    [SerializeField] private GameObject enemigo;
    [SerializeField] private GameObject spike;
    [Range(0f, 1f)][SerializeField] private float enemyChance = 0.35f;
    [SerializeField] private float enemyExtraLift = 0.02f;

    [SerializeField] private float spawnAheadDistance = 30f;
    [SerializeField] private float cleanupBehindDistance = 35f;
    [SerializeField] private float startBuffer = 1.2f;

    [SerializeField] private bool autoTuneFromPlayer = true;

    [SerializeField] private float minGap = 3.0f;
    [SerializeField] private float maxGap = 6.0f;
    [SerializeField] private float maxStepUp = 1.2f;
    [SerializeField] private float maxStepDown = 2.0f;

    [SerializeField] private float bandBelow = 0.4f;
    [SerializeField] private float bandAbove = 1.6f;
    [SerializeField] private float verticalBias = 0.6f;
    [SerializeField] private bool alignToIntY = true;

    [SerializeField] private bool confineToCameraY = true;
    [SerializeField] private float cameraTopMargin = 1.0f;
    [SerializeField] private float cameraBottomMargin = 0.4f;

    float tunedMinGap, tunedMaxGap, tunedStepUp, tunedStepDown;
    float lastSpawnX, lastY;
    readonly List<GameObject> spawned = new();
    Camera cam;

    [HideInInspector] public float difficultyGapScale = 1f;
    [HideInInspector] public float difficultyEnemyBonus = 0f;

    void Awake()
    {
        if (!player && GameManager.Instance) player = GameManager.Instance.GetPlayer();
        cam = Camera.main;
    }

    void Start()
    {
        if (!initialPlatform || !plataforma) { enabled = false; return; }

        if (autoTuneFromPlayer) AutoTune();
        else { tunedMinGap = minGap; tunedMaxGap = maxGap; tunedStepUp = maxStepUp; tunedStepDown = maxStepDown; }

        float right = CalcRightEdge(initialPlatform);
        lastSpawnX = right + startBuffer - 0.5f;
        lastY = initialPlatform.position.y + verticalBias;

        GenerateUntil((player ? player.position.x : right) + spawnAheadDistance);
    }

    void Update()
    {
        if (GameManager.Instance && !GameManager.Instance.IsPlaying()) return;
        if (!player) return;

        float targetX = player.position.x + spawnAheadDistance;
        if (lastSpawnX < targetX) GenerateUntil(targetX);

        CleanupBehind(player.position.x - cleanupBehindDistance);
    }

    void GenerateUntil(float targetX)
    {
        float baseY = initialPlatform.position.y + verticalBias;
        float minBandY = baseY - bandBelow;
        float maxBandY = baseY + bandAbove;

        if (confineToCameraY && cam)
        {
            float halfH = cam.orthographicSize;
            float camMin = cam.transform.position.y - halfH + cameraBottomMargin;
            float camMax = cam.transform.position.y + halfH - cameraTopMargin;
            minBandY = Mathf.Max(minBandY, camMin);
            maxBandY = Mathf.Min(maxBandY, camMax);
        }

        while (lastSpawnX < targetX)
        {
            float gap = Random.Range(tunedMinGap, tunedMaxGap) * Mathf.Max(0.5f, difficultyGapScale);
            float nextX = lastSpawnX + gap;

            float rawY = lastY + Random.Range(-tunedStepDown, tunedStepUp);
            float nextY = Mathf.Clamp(rawY, minBandY, maxBandY);
            if (alignToIntY) nextY = Mathf.Round(nextY);

            Vector3 pos = new Vector3(nextX, nextY, plataforma.transform.position.z);
            GameObject plat = Instantiate(plataforma, pos, Quaternion.identity);
            spawned.Add(plat);

            var col = plat.GetComponentInChildren<Collider2D>();
            var rend = plat.GetComponentInChildren<Renderer>();
            Bounds pb = col ? col.bounds : rend.bounds;

            float spawnX = Random.Range(pb.min.x + 0.05f, pb.max.x - 0.05f);
            float topY = pb.max.y;

            if (spawnCoins && moneda && Random.value < coinChance)
            {
                float halfH = GetHalfHeight(moneda);
                Vector3 cpos = new Vector3(spawnX, topY + halfH + coinExtraLift, 0f);
                Instantiate(moneda, cpos, Quaternion.identity);
            }

            float enemyProb = Mathf.Clamp01(enemyChance + difficultyEnemyBonus);
            if (spawnEnemies && Random.value < enemyProb)
            {
                GameObject pick = (spike && Random.value < 0.5f) ? spike : enemigo;
                if (pick)
                {
                    float halfH = GetHalfHeight(pick);
                    Vector3 epos = new Vector3(spawnX, topY + halfH + enemyExtraLift, 0f);
                    Instantiate(pick, epos, Quaternion.identity);
                }
            }

            lastSpawnX = nextX;
            lastY = nextY;
        }
    }

    float ResolveTopYAtX(float x)
    {
        float startY = cam ? cam.transform.position.y + cam.orthographicSize + 2f
                           : (initialPlatform.position.y + verticalBias + bandAbove + 3f);

        RaycastHit2D hit = Physics2D.Raycast(new Vector2(x, startY), Vector2.down, 100f, groundLayer);
        if (hit) return hit.point.y;
        return lastY + 0.01f;
    }

    float GetHalfHeight(GameObject prefab)
    {
        var rend = prefab.GetComponentInChildren<Renderer>();
        if (rend) return rend.bounds.extents.y;
        var col = prefab.GetComponentInChildren<Collider2D>();
        if (col) return col.bounds.extents.y;
        return 0.5f;
    }

    void CleanupBehind(float limitX)
    {
        for (int i = spawned.Count - 1; i >= 0; i--)
        {
            var go = spawned[i];
            if (!go) { spawned.RemoveAt(i); continue; }
            if (go.transform.position.x < limitX) { Destroy(go); spawned.RemoveAt(i); }
        }
    }

    void AutoTune()
    {
        float jumpForce = 12f, speed = 5f, g = Mathf.Abs(Physics2D.gravity.y);
        var pc = player ? player.GetComponent<PlayerController>() : null;
        if (pc) { jumpForce = pc.jumpForce; speed = (GameManager.Instance ? GameManager.Instance.GetPlayerSpeed(pc.baseSpeed) : pc.baseSpeed); }
        var rb = player ? player.GetComponent<Rigidbody2D>() : null;
        if (rb) g *= Mathf.Max(0.1f, rb.gravityScale);

        float v = jumpForce;
        float tTotal = 2f * v / g;
        float maxSameLevelGap = speed * tTotal;

        tunedMinGap = Mathf.Max(2.2f, maxSameLevelGap * 0.35f);
        tunedMaxGap = Mathf.Clamp(maxSameLevelGap * 0.75f, 3.5f, 8.0f);
        tunedStepUp = Mathf.Clamp((v * v) / (2f * g) * 0.55f, 1.0f, 2.2f);
        tunedStepDown = Mathf.Clamp((v * v) / (2f * g) * 0.85f + 0.6f, 1.4f, 3.5f);
    }

    float CalcRightEdge(Transform t)
    {
        var r = t.GetComponentInChildren<Renderer>();
        return r ? r.bounds.max.x : t.position.x;
    }

    public void SetDifficulty(float gapScale, float enemyBonus)
    {
        difficultyGapScale = gapScale;
        difficultyEnemyBonus = enemyBonus;
    }
}
