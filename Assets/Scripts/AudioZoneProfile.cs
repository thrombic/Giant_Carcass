using UnityEngine;

[CreateAssetMenu(menuName = "Giant Carcass/Audio Zone")]
public class AudioZoneProfile : ScriptableObject
{
    public AudioClip music;
    [Range(0f, 1f)] public float musicVolume = 1f;
    public AudioClip ambience;
    [Range(0f, 1f)] public float ambienceVolume = 0.25f;
}
