using System.Collections.Generic;
using UnityEngine;

public sealed class StarfleaSpawner : MonoBehaviour
{
    public GameObject[] starfleaPrefabs;
    [Tooltip("Choose left or right with equal probability for each spawned Starflea.")]
    public bool randomizeStartingDirection = true;
    public Transform spawnPoint;
    [Tooltip("The solid, crawlable rectangle used to find the pipe's top edge.")]
    public BoxCollider2D pipeCollider;
    [Min(0f)] public float spawnClearance = 0.05f;
    [Min(0f)] public float firstSpawnDelay = 20f;
    [Min(0.1f)] public float spawnInterval = 20f;
    [Min(1)] public int maxSpawnedAlive = 6;

    private readonly List<GameObject> spawnedStarfleas = new List<GameObject>();
    private float spawnTimer;

    void OnEnable()
    {
        if (pipeCollider == null)
            pipeCollider = GetComponent<BoxCollider2D>();
        spawnTimer = firstSpawnDelay;
    }

    void Update()
    {
        spawnedStarfleas.RemoveAll(starflea => starflea == null);
        if (spawnedStarfleas.Count >= maxSpawnedAlive)
            return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0f)
            return;

        SpawnRandomStarflea();
        spawnTimer = spawnInterval;
    }

    void SpawnRandomStarflea()
    {
        if (starfleaPrefabs == null || starfleaPrefabs.Length == 0)
            return;

        int startIndex = Random.Range(0, starfleaPrefabs.Length);
        for (int offset = 0; offset < starfleaPrefabs.Length; offset++)
        {
            GameObject prefab = starfleaPrefabs[(startIndex + offset) % starfleaPrefabs.Length];
            if (prefab == null)
                continue;

            Vector3 position = spawnPoint != null ? spawnPoint.position : transform.position;
            GameObject spawned = Instantiate(prefab, position, Quaternion.identity);
            StarfleaBase starflea = spawned.GetComponent<StarfleaBase>();
            if (starflea != null && randomizeStartingDirection)
                starflea.startMovingLeft = Random.value < 0.5f;
            PlaceAbovePipe(spawned);
            spawnedStarfleas.Add(spawned);
            return;
        }
    }

    void PlaceAbovePipe(GameObject spawned)
    {
        if (pipeCollider == null)
            return;

        // Resolve the top in pipe-local space so rotated/scaled pipes work too.
        Transform pipeTransform = pipeCollider.transform;
        Vector2 localPoint = spawnPoint != null
            ? (Vector2)pipeTransform.InverseTransformPoint(spawnPoint.position)
            : pipeCollider.offset;
        float halfWidth = pipeCollider.size.x * 0.5f;
        localPoint.x = Mathf.Clamp(localPoint.x,
            pipeCollider.offset.x - halfWidth, pipeCollider.offset.x + halfWidth);
        localPoint.y = pipeCollider.offset.y + pipeCollider.size.y * 0.5f;

        StarfleaBase starflea = spawned.GetComponent<StarfleaBase>();
        float radius = starflea != null ? starflea.bodyRadius : 0f;
        CircleCollider2D body = spawned.GetComponent<CircleCollider2D>();
        if (body != null)
        {
            Vector3 scale = body.transform.lossyScale;
            radius = Mathf.Max(radius, body.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y)));
        }

        Vector3 position = pipeTransform.TransformPoint(localPoint)
            + pipeTransform.up * (radius + spawnClearance);
        position.z = transform.position.z;
        spawned.transform.position = position;
        Rigidbody2D bodyPhysics = spawned.GetComponent<Rigidbody2D>();
        if (bodyPhysics != null)
            bodyPhysics.position = position;
    }
}
