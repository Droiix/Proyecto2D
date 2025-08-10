using UnityEngine;
using System.Collections.Generic;

public class AutoGenerator2D : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform player;              // opcional: si está vacío lo toma del GameManager
    [SerializeField] private Transform initialPlatform;     // TU plataforma inicial en la escena (OBLIGATORIO)
    [SerializeField] private GameObject plataformaPrefab;
    [SerializeField] private GameObject monedaPrefab;
    [SerializeField] private GameObject enemigoPrefab;
    [SerializeField] private GameObject spikePrefab;

    [Header("Rango de generación")]
    [SerializeField] private float spawnAheadDistance = 30f;
    [SerializeField] private float cleanupBehindDistance = 35f;

    [Header("Plataformas")]
    [SerializeField] private float startBuffer = 1.0f;  // distancia extra después del borde derecho de la plataforma inicial
    [SerializeField] private float minGap = 2.0f;
    [SerializeField] private float maxGap = 4.5f;
    [SerializeField] private float minY = -2f;
    [SerializeField] private float maxY = 2.5f;
    [SerializeField] private bool alignToIntY = true;

    [Header("Monedas y enemigos")]
    [Range(0f, 1f)][SerializeField] private float coinChance = 0.65f;
    [Range(0f, 1f)][SerializeField] private float enemyChance = 0.35f;
    [SerializeField] private float coinHeightOffset = 1.2f;
    [SerializeField] private float enemyHeightOffset = 0.6f;

    private float lastSpawnX;
    private readonly List<GameObject> spawned = new();

    private void Start()
    {
        if (player == null && GameManager.Instance != null)
            player = GameManager.Instance.GetPlayer();

        // calcular el borde derecho de la plataforma inicial
        if (initialPlatform == null)
        {
            Debug.LogError("[AutoGenerator2D] Falta 'initialPlatform'. Asigna tu plataforma inicial.");
            enabled = false;
            return;
        }

        float rightEdge = CalcRightEdge(initialPlatform);
        lastSpawnX = rightEdge + startBuffer - 0.5f; // pequeño margen para que el primer gap funcione

        // Generar un tramo inicial por si el jugador ya está cerca
        GenerateUntil((player ? player.position.x : rightEdge) + spawnAheadDistance);
    }

    private float CalcRightEdge(Transform t)
    {
        // Toma cualquier Renderer para calcular bounds (SpriteRenderer recomendado)
        var rend = t.GetComponentInChildren<Renderer>();
        if (rend != null) return rend.bounds.max.x;
        // fallback: posición x si no hay renderer (no debería pasar)
        return t.position.x;
    }

    private void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying()) return;
        if (player == null) return;

        float targetX = player.position.x + spawnAheadDistance;
        if (lastSpawnX < targetX)
            GenerateUntil(targetX);

        CleanupBehind(player.position.x - cleanupBehindDistance);
    }

    private void GenerateUntil(float targetX)
    {
        while (lastSpawnX < targetX)
        {
            float gap = Random.Range(minGap, maxGap);
            float nextX = lastSpawnX + gap;

            float y = Random.Range(minY, maxY);
            if (alignToIntY) y = Mathf.Round(y);

            // plataforma
            Vector3 platPos = new Vector3(nextX, y, plataformaPrefab ? plataformaPrefab.transform.position.z : 0f);
            GameObject plat = Instantiate(plataformaPrefab, platPos, Quaternion.identity);
            spawned.Add(plat);

            // moneda
            if (monedaPrefab && Random.value < coinChance)
            {
                Vector3 coinPos = platPos + Vector3.up * coinHeightOffset;
                GameObject coin = Instantiate(monedaPrefab, coinPos, Quaternion.identity);
                spawned.Add(coin);
            }

            // enemigo/spike
            if (Random.value < enemyChance)
            {
                GameObject pick = (Random.value < 0.5f && spikePrefab) ? spikePrefab : enemigoPrefab;
                if (pick)
                {
                    Vector3 ePos = platPos + Vector3.up * enemyHeightOffset;
                    GameObject e = Instantiate(pick, ePos, Quaternion.identity);
                    spawned.Add(e);
                }
            }

            lastSpawnX = nextX;
        }
    }

    private void CleanupBehind(float limitX)
    {
        // SOLO limpia lo que este generador creó (spawned)
        for (int i = spawned.Count - 1; i >= 0; i--)
        {
            var go = spawned[i];
            if (go == null) { spawned.RemoveAt(i); continue; }

            if (go.transform.position.x < limitX)
            {
                Destroy(go);
                spawned.RemoveAt(i);
            }
        }
    }

    public void ClearAllGenerated()
    {
        foreach (var go in spawned) if (go) Destroy(go);
        spawned.Clear();
    }
}

