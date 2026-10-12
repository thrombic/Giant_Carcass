using UnityEngine;

public class GasBubble : MonoBehaviour
{
    private float lifetime = 10;
    private Rigidbody2D rb;

    [SerializeField] private float rayDistance = .01f;
    [SerializeField] private LayerMask detectionLayers;
    private Collider2D[] playerColliders;
    private Collider2D feetCollider;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        playerColliders = GameObject.FindGameObjectWithTag("Player").GetComponents<Collider2D>();
        foreach (Collider2D col in playerColliders)
        {
            if (col.isTrigger)
            {
                feetCollider = col;
                break;
            }
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        // Start slightly above the collider so the ray doesn't hit this object itself
        Collider2D col = GetComponent<Collider2D>();
        Vector2 origin = new Vector2(col.bounds.center.x, col.bounds.max.y + 0.01f);

        ContactPoint2D[] contacts = new ContactPoint2D[8];
        int count = col.GetContacts(contacts);

        if (count > 0 /* && check if contactpoint is above the correct y-position */)
            rb.linearVelocityY = -1;

        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.up, rayDistance, detectionLayers); // check if player is standing on the bubble
        // TODO: change raycast to use angle of platform effector to check for player

        // TODO: when player lands on bubble, wobble slightly (descend, ascend, descend)
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