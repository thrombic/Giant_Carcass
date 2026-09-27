using UnityEngine;

public class StarfleaAmbientAudio : MonoBehaviour
{
    public AudioClip loopClip;
    [Range(0f, 1f)] public float quietVolume = 0.12f;
    [Range(0f, 1f)] public float closeVolume = 1f;
    public bool proximityBoostEnabled = true;
    [Min(0f)] public float fullVolumeDistanceInPlayerRadii = 5f;
    [Min(0f)] public float fadeDistanceInPlayerRadii = 3f;
    [Min(0.01f)] public float volumeChangePerSecond = 3f;

    private AudioSource source;
    private PlayerController player;
    private Collider2D playerCollider;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        if (source == null)
            source = gameObject.AddComponent<AudioSource>();

        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.clip = loopClip;
        source.volume = quietVolume;
    }

    void OnEnable()
    {
        if (source != null && loopClip != null)
            source.Play();
    }

    void OnDisable()
    {
        if (source != null)
            source.Stop();
    }

    void Update()
    {
        if (source == null)
            return;

        if (source.clip != loopClip)
        {
            source.Stop();
            source.clip = loopClip;
            if (loopClip != null)
                source.Play();
        }

        if (player == null)
        {
            player = FindFirstObjectByType<PlayerController>();
            playerCollider = player != null ? player.GetComponent<Collider2D>() : null;
        }

        float targetVolume = quietVolume;
        if (proximityBoostEnabled && player != null)
        {
            float playerRadius = playerCollider != null ? playerCollider.bounds.extents.x : 0.5f;
            playerRadius = Mathf.Max(0.1f, playerRadius);
            float closeDistance = playerRadius * fullVolumeDistanceInPlayerRadii;
            float fadeDistance = playerRadius * fadeDistanceInPlayerRadii;
            float distance = Vector2.Distance(transform.position, player.transform.position);
            float closeness = fadeDistance > 0f
                ? 1f - Mathf.InverseLerp(closeDistance, closeDistance + fadeDistance, distance)
                : (distance <= closeDistance ? 1f : 0f);
            targetVolume = Mathf.Lerp(quietVolume, closeVolume, closeness);
        }

        source.volume = Mathf.MoveTowards(source.volume, targetVolume,
            Mathf.Max(0.01f, volumeChangePerSecond) * Time.deltaTime);
    }
}
