using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public sealed class HealthPickup : MonoBehaviour
{
    [Min(1)] public int healAmount = 1;
    [Min(0f)] public float pulseSpeed = 2.5f;
    [Range(0f, 0.3f)] public float pulseScale = 0.08f;
    [Range(0f, 0.5f)] public float pulseBrightness = 0.15f;

    [Header("Pickup Audio")]
    public AudioClip pickupSound;
    [Range(0f, 1f)] public float pickupVolume = 0.75f;

    private static AudioSource pickupAudioSource;

    private SpriteRenderer spriteRenderer;
    private Vector3 baseScale;
    private Color baseColor;
    private float phase;
    private bool collected;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale;
        baseColor = spriteRenderer.color;
        phase = Random.value * Mathf.PI * 2f;
    }

    void Update()
    {
        float pulse = (Mathf.Sin(Time.time * pulseSpeed + phase) + 1f) * 0.5f;
        transform.localScale = baseScale * (1f + pulse * pulseScale);

        float brightness = 1f - pulse * pulseBrightness;
        spriteRenderer.color = new Color(
            Mathf.Min(1f, baseColor.r * brightness),
            Mathf.Min(1f, baseColor.g * brightness),
            Mathf.Min(1f, baseColor.b * brightness),
            baseColor.a);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        TryCollect(other);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        TryCollect(other);
    }

    void TryCollect(Collider2D other)
    {
        if (collected)
            return;

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null)
            return;

        HealthSystem health = player.GetComponent<HealthSystem>();
        if (health != null && health.Heal(healAmount))
        {
            collected = true;
            PlayPickupSound();
            Destroy(gameObject);
        }
    }

    void PlayPickupSound()
    {
        if (pickupSound == null || pickupVolume <= 0f)
            return;

        // A shared 2D source lets each sound finish after its pickup is destroyed.
        if (pickupAudioSource == null)
        {
            GameObject audioObject = new GameObject("Health Pickup Audio");
            DontDestroyOnLoad(audioObject);
            pickupAudioSource = audioObject.AddComponent<AudioSource>();
            pickupAudioSource.playOnAwake = false;
            pickupAudioSource.spatialBlend = 0f;
            pickupAudioSource.loop = false;
        }

        pickupAudioSource.PlayOneShot(pickupSound, pickupVolume);
    }
}
