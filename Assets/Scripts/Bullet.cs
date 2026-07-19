using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 14f;
    public float lifetime = 3f;

    private Vector2 direction;

    public void SetDirection(Vector2 dir)
    {
        direction = dir.normalized;
        // Flip sprite if going left
        if (dir.x < 0)
            transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
    }

    void FixedUpdate()
    {
        transform.Translate(direction * speed * Time.deltaTime);
        lifetime -= Time.deltaTime;
        if (lifetime <= 0f) Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Try to hit an enemy
        EnemyBase enemy = other.GetComponent<EnemyBase>();
        if (enemy != null)
        {
            enemy.TakeDamage(1);
            Destroy(gameObject);
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
}
