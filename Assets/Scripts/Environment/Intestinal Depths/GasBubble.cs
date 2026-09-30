using UnityEngine;

public class GasBubble : MonoBehaviour
{
    private float lifetime = 10;
    private Rigidbody2D rb;

    [SerializeField] private float rayDistance = .01f;
    [SerializeField] private LayerMask detectionLayers;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        // Start slightly above the collider so the ray doesn't hit this object itself
        Collider2D col = GetComponent<Collider2D>();
        Vector2 origin = new Vector2(col.bounds.center.x, col.bounds.max.y + 0.01f);

        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.up, rayDistance, detectionLayers); // check if player is standing on the bubble

        // if player is standing on it, descend slowly
        if (hit.collider != null)
            rb.linearVelocityY = -1;
        else
            rb.linearVelocityY = 2;

        lifetime -= Time.deltaTime;
        if (lifetime <= 0)
            Destroy(gameObject);
    }
}