using UnityEngine;

public class Door : MonoBehaviour
{
    [Header("Door Settings")]
    public int hitsRequired = 10000;
    public float openDistance = 3f;       // how far it slides up
    public float openSpeed = 4f;

    private int hitCount = 0;
    private bool isOpen = false;
    private Vector3 closedPos;
    private Vector3 openPos;

    // Optional: visual flash on hit
    private SpriteRenderer sr;
    private Color originalColor;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr) originalColor = sr.color;

        closedPos = transform.position;
        openPos = closedPos + Vector3.up * openDistance;
    }

    void Update()
    {
        if (isOpen)
            transform.position = Vector3.MoveTowards(transform.position, openPos, openSpeed * Time.deltaTime);
    }

    public void TakeHit()
    {
        if (isOpen) return;

        hitCount++;
        Debug.Log($"Door hit {hitCount}/{hitsRequired}");

        if (sr) StartCoroutine(FlashRed());

        if (hitCount >= hitsRequired)
            Open();
    }

    void Open()
    {
        isOpen = true;
        // Disable the collider so the player can walk through
        GetComponent<Collider2D>().enabled = false;
        //AudioManager.Instance.PlayDoorOpen();
    }

    System.Collections.IEnumerator FlashRed()
    {
        if (sr) sr.color = Color.red;
        yield return new WaitForSeconds(0.08f);
        if (sr) sr.color = originalColor;
    }
}