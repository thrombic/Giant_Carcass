using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Flare : MonoBehaviour
{
    public float speed = 10f;
    private Vector2 direction;

    [SerializeField] private Collider2D flareCollider;
    [SerializeField] private Collider2D damageCollider;
    private LightningOrb orb;
    private Light2D light;
    private Rigidbody2D rb;

    private bool collided = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        orb = GetComponent<LightningOrb>();
        light = GetComponent<Light2D>();
    }

    void OnTriggerStay2D(Collider2D other)
    {
        // Try to hit an enemy
        EnemyBase enemy = other.GetComponent<EnemyBase>();
        if (enemy != null)
        {
            enemy.TakeDamage(1);
            return;
        }

        // Try to hit a door
        Door door = other.GetComponent<Door>();
        if (door != null)
        {
            door.TakeHit();
            //AudioManager.Instance.PlayHit();
            Destroy(gameObject);
            return;
        }

    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collided)
            return;

        if (collision != null)
        {
            speed = 0;
            collided = true;
            print("Flare collided with " + collision.gameObject.name);
        }

        GameObject newParent = collision.gameObject;

        // when hit another object, reparent to that object and stop moving, then start pulsing
        rb.gravityScale = 0;
        rb.linearVelocity = Vector2.zero;
        flareCollider.enabled = false;
        transform.SetParent(newParent.transform);
        rb.bodyType = RigidbodyType2D.Kinematic;
        transform.localRotation = Quaternion.identity;
        transform.localScale = new Vector3(
            1 / newParent.transform.lossyScale.x,
            1 / newParent.transform.lossyScale.y,
            1 / newParent.transform.lossyScale.z
        );

        StartCoroutine("StartPulsing");
    }

    IEnumerator StartPulsing()
    {
        yield return new WaitForSeconds(1f);
        orb.Activate();
        light.enabled = true;
        damageCollider.enabled = true;
        yield return new WaitForSeconds(15f);

        Destroy(gameObject);
        yield return null;
    }

    void Pulse() // called every 4 seconds after hitting an object
    {
        // TODO: increase the scale of the flare and then decrease it back to normal
        orb.Activate();
        //transform.localScale = new Vector3(3, 3, 3);

        //transform.localScale = new Vector3 (1, 1, 1);
    }

    public void SetDirection(Vector2 dir)
    {
        direction = dir.normalized;
        // Flip sprite if going left
        if (dir.x < 0)
            transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
    }

    void FixedUpdate()
    {
        if (!collided)
            transform.Translate(direction * speed * Time.deltaTime);
        /*else if (hitTransform != null)
            transform.position = hitTransform.position;*/
    }
}
