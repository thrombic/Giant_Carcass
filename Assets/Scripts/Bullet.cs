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
            transform.localScale = new Vector3(-1, 1, 1);
    }

    void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime);
        lifetime -= Time.deltaTime;
        if (lifetime <= 0f) Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Try to hit a door
        Door door = other.GetComponent<Door>();
        if (door != null)
        {
            door.TakeHit();
            //AudioManager.Instance.PlayHit();
            Destroy(gameObject);
            return;
        }

        // Destroy on anything else (walls, ground)
        if (!other.isTrigger)
            Destroy(gameObject);
    }
}