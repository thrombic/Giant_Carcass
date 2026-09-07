using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SpikeHazard : MonoBehaviour
{
    [Min(0)] public int damage = 1;
    [Min(0f)] public float knockbackSpeed = 8f;

    private Collider2D hazardCollider;

    void Awake()
    {
        hazardCollider = GetComponent<Collider2D>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        TryDamage(other.gameObject);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        TryDamage(other.gameObject);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        TryDamage(collision.gameObject);
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        TryDamage(collision.gameObject);
    }

    void TryDamage(GameObject target)
    {
        PlayerDamageReceiver receiver = target.GetComponentInParent<PlayerDamageReceiver>();
        if (receiver == null)
            return;

        Vector2 playerPosition = receiver.transform.position;
        Vector2 nearestPoint = hazardCollider.ClosestPoint(playerPosition);
        Vector2 knockbackDirection = playerPosition - nearestPoint;

        if (knockbackDirection.sqrMagnitude < 0.0001f)
            knockbackDirection = transform.up;

        receiver.TryTakeContactDamage(damage, knockbackDirection, knockbackSpeed);
    }
}
