using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ZoneAudioController : MonoBehaviour
{
    public AudioZoneProfile defaultZone;
    [Min(0f)] public float transitionSeconds = 1.5f;
    public bool musicEnabled = true;
    public bool ambienceEnabled = true;

    private static ZoneAudioController instance;
    private readonly LoopPair musicLoop = new LoopPair();
    private readonly LoopPair ambienceLoop = new LoopPair();
    private PlayerController player;
    private CameraRoomBounds currentRoom;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            instance.defaultZone = defaultZone;
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        musicLoop.Initialize(this, 0);
        ambienceLoop.Initialize(this, 128);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        RefreshScene();
        UpdateZone(true);
    }

    void Update()
    {
        if (player == null)
            player = FindFirstObjectByType<PlayerController>();

        UpdateZone(false);
    }

    void OnDestroy()
    {
        if (instance != this)
            return;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        instance = null;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshScene();
        UpdateZone(false);
    }

    void RefreshScene()
    {
        player = FindFirstObjectByType<PlayerController>();
        currentRoom = null;
    }

    void UpdateZone(bool immediate)
    {
        if (player == null)
            return;

        Vector2 playerPosition = player.transform.position;
        if (currentRoom == null || !currentRoom.isActiveAndEnabled || !currentRoom.Contains(playerPosition))
            currentRoom = CameraRoomBounds.FindRoomForPosition(playerPosition, null);

        AudioZoneProfile zone = currentRoom != null && currentRoom.audioZone != null
            ? currentRoom.audioZone
            : defaultZone;

        float fade = immediate ? 0f : transitionSeconds;
        musicLoop.Set(zone != null && musicEnabled ? zone.music : null,
            zone != null ? zone.musicVolume : 0f, fade);
        ambienceLoop.Set(zone != null && ambienceEnabled ? zone.ambience : null,
            zone != null ? zone.ambienceVolume : 0f, fade);
    }

    private sealed class LoopPair
    {
        private ZoneAudioController owner;
        private AudioSource first;
        private AudioSource second;
        private AudioClip targetClip;
        private float targetVolume;
        private Coroutine fadeRoutine;

        public void Initialize(ZoneAudioController controller, int priority)
        {
            owner = controller;
            first = CreateSource(priority);
            second = CreateSource(priority);
        }

        AudioSource CreateSource(int priority)
        {
            AudioSource source = owner.gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.priority = priority;
            source.volume = 0f;
            return source;
        }

        public void Set(AudioClip clip, float volume, float fadeSeconds)
        {
            volume = Mathf.Clamp01(volume);
            if (clip == targetClip && Mathf.Approximately(volume, targetVolume))
                return;

            targetClip = clip;
            targetVolume = volume;

            if (fadeRoutine != null)
                owner.StopCoroutine(fadeRoutine);

            AudioSource incoming = null;
            if (clip != null)
            {
                if (first.isPlaying && first.clip == clip)
                    incoming = first;
                else if (second.isPlaying && second.clip == clip)
                    incoming = second;
                else
                {
                    incoming = first.volume <= second.volume ? first : second;
                    incoming.Stop();
                    incoming.clip = clip;
                    incoming.volume = 0f;
                    incoming.Play();
                }
            }

            AudioSource outgoing = incoming == first ? second : first;
            if (incoming == null)
                outgoing = second;

            if (fadeSeconds <= 0f)
            {
                Finish(incoming, outgoing, volume);
                return;
            }

            fadeRoutine = owner.StartCoroutine(Fade(incoming, outgoing, volume, fadeSeconds));
        }

        IEnumerator Fade(AudioSource incoming, AudioSource outgoing, float volume, float seconds)
        {
            float incomingStart = incoming != null ? incoming.volume : 0f;
            float outgoingStart = outgoing.volume;
            float otherStart = incoming == null ? first.volume : 0f;
            float elapsed = 0f;

            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / seconds);
                if (incoming != null)
                    incoming.volume = Mathf.Lerp(incomingStart, volume, progress);
                else
                    first.volume = Mathf.Lerp(otherStart, 0f, progress);
                outgoing.volume = Mathf.Lerp(outgoingStart, 0f, progress);
                yield return null;
            }

            Finish(incoming, outgoing, volume);
            fadeRoutine = null;
        }

        void Finish(AudioSource incoming, AudioSource outgoing, float volume)
        {
            if (incoming != null)
                incoming.volume = volume;

            outgoing.Stop();
            outgoing.clip = null;
            outgoing.volume = 0f;

            if (incoming == null)
            {
                first.Stop();
                first.clip = null;
                first.volume = 0f;
            }
        }
    }
}
