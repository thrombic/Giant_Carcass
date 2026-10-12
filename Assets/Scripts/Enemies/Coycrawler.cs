using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.U2D;

public class Coycrawler : EnemyBase
{
    private bool isUpright = true;
    private bool facingLeft = true;
    private float moveDirection;
    private LightDetector detector;

    private SpriteRenderer sr;
    private Collider2D col;
    private LayerMask groundLayers;

    [SerializeField] private Sprite uprightSprite;
    [SerializeField] private Sprite downedSprite;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        groundLayers = LayerMask.GetMask("Ground");
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        detector = GetComponentInChildren<LightDetector>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        moveDirection = facingLeft ? -1f : 1f;

        DetectLight();

        if (isUpright)
            Move();

        CheckTurnAround();
    }

    void DetectLight()
    {
        if (detector.CurrentExposure > detector.exposureThreshold) // switch to dormant behavior
        {
            isUpright = false;
            contactDamageEnabled = false;
            sr.sprite = downedSprite;
            // change to dormant sprite
        }
        else
        {
            isUpright = true;
            contactDamageEnabled = true;
            sr.sprite = uprightSprite;
        }
    }

    void Move()
    {
        transform.position += new Vector3(moveDirection * Time.deltaTime, 0, 0);
        // move left/right
    }

    void CheckTurnAround()
    {
        // Check for obstacles or edges to turn around
        Vector3 colEdge = new Vector3(col.bounds.center.x + moveDirection * col.bounds.size.x/2, col.bounds.min.y);
        RaycastHit2D hitDown = Physics2D.Raycast(colEdge, Vector3.down, 0.05f, groundLayers);
        RaycastHit2D hitForward = Physics2D.Raycast(colEdge, Vector3.right * moveDirection, 0.05f, groundLayers);
        if (hitDown.collider == null || hitForward)
        {
            // Turn around if hitting an obstacle
            facingLeft = !facingLeft;

            // sr.flipX = !sr.flipX; // Flip sprite horizontally
        }
    }

}
