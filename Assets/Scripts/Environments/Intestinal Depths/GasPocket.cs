using System.Collections;
using UnityEngine;

public class GasPocket : MonoBehaviour
{
    private GameObject bubble;
    private bool spawningBubble = false;

    [SerializeField] private GameObject bubblePrefab;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        bubble = null;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (bubble == null && !spawningBubble)
        {
            spawningBubble = true;
            StartCoroutine(SpawnBubble());
        }
    }

    // if bubble is destroyed, wait a second then spawn a new one
    IEnumerator SpawnBubble()
    {
        // TODO: play animation
        yield return new WaitForSeconds(1);
        bubble = Instantiate(bubblePrefab, transform.position, Quaternion.identity);
        spawningBubble = false;
        yield return null;
    }
}
