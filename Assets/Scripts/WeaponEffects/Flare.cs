using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Flare : MonoBehaviour
{
    public float speed = 10f;
    private Vector2 direction;

    private LightningOrb orb;
    private Light2D light;

    private bool collided = false;

    private void Start()
    {
        orb = GetComponent<LightningOrb>();
        light = GetComponent<Light2D>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collided)
            return;

        if (collision != null)
        {
            speed = 0;
            collided = true;
        }
        
        // if hit another object, reparent to that object and stop moving, then start pulsing
        StartCoroutine("StartPulsing");
    }

    IEnumerator StartPulsing()
    {
        // call pulse every 5 seconds for 20 seconds, then destroy the flare
        for (int pulseCount = 0; pulseCount < 4; pulseCount++)
        {
            yield return new WaitForSeconds(2f);
            orb.Activate();
            light.enabled = true;
            yield return new WaitForSeconds(1f);
            orb.Deactivate();
            light.enabled = false;
            yield return new WaitForSeconds(2f);
        }

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
        transform.Translate(direction * speed * Time.deltaTime);
    }
}
