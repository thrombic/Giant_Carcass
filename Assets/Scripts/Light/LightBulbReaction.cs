using UnityEngine;
using UnityEngine.Rendering.Universal;
using static UnityEngine.Rendering.DebugUI.Table;

public class LightBulbReaction : MonoBehaviour
{

    private SpriteRenderer sr;
    [SerializeField] private LightDetector detector;
    Light2D selfLight;
    private float timeToFade = 5;

    void SetBrightness(float brightness)
    {
        sr.color = new Color(brightness, brightness, brightness, sr.color.a);
    }

    void IncreaseBrightness(float amount)
    {
        sr.color += new Color(amount, amount, amount, 0);
    }

    void DecreaseBrightness(float amount)
    {
        sr.color -= new Color(amount, amount, amount, 0);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        detector = GetComponent<LightDetector>();
        selfLight = GetComponent<Light2D>();
        SetBrightness(0);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        float exposure = detector.CurrentExposure;
        if (exposure > 0)
            IncreaseBrightness(exposure * Time.deltaTime);
        else
            DecreaseBrightness(Time.deltaTime / timeToFade);

        sr.color = new Color(
            Mathf.Clamp01(sr.color.r),
            Mathf.Clamp01(sr.color.g),
            Mathf.Clamp01(sr.color.b),
            Mathf.Clamp01(sr.color.a)
        );
        selfLight.intensity = sr.color.r;
    }
}
