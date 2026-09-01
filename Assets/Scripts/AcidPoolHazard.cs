using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class AcidPoolHazard : MonoBehaviour
{
    [Header("Reset")]
    [Min(0)] public int damage = 1;
    [Min(0f)] public float resetDelay = 0.75f;
    [Min(0f)] public float postResetInvulnerability = 1.25f;

    [Header("Room Override")]
    public CameraRoomBounds roomOverride;
    public Transform respawnPointOverride;

    void OnTriggerEnter2D(Collider2D other)
    {
        TryResetPlayer(other.gameObject);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        TryResetPlayer(other.gameObject);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        TryResetPlayer(collision.gameObject);
    }

    void TryResetPlayer(GameObject target)
    {
        PlayerDamageReceiver receiver = target.GetComponentInParent<PlayerDamageReceiver>();
        if (receiver == null || receiver.IsResetting)
            return;

        CameraRoomBounds room = roomOverride;
        if (room == null)
            room = CameraRoomBounds.FindRoomForPosition(receiver.transform.position, null);

        receiver.BeginHazardReset(
            damage,
            resetDelay,
            postResetInvulnerability,
            room,
            respawnPointOverride);
    }
}
