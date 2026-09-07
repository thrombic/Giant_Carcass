using UnityEngine;

public class CameraRoomBounds : MonoBehaviour
{
    public Vector2 roomSize = new Vector2(32f, 18f);
    public Vector2 roomOffset;
    public Transform respawnPoint;
    public Color gizmoColor = new Color(0.25f, 0.8f, 1f, 0.35f);

    public Bounds WorldBounds
    {
        get
        {
            Vector3 center = transform.position + (Vector3)roomOffset;
            Vector3 size = new Vector3(Mathf.Max(0.01f, roomSize.x), Mathf.Max(0.01f, roomSize.y), 0f);
            return new Bounds(center, size);
        }
    }

    public bool Contains(Vector2 worldPosition)
    {
        return WorldBounds.Contains(new Vector3(worldPosition.x, worldPosition.y, WorldBounds.center.z));
    }

    public Vector3 GetRespawnPosition(Vector3 fallbackPosition)
    {
        return respawnPoint != null ? respawnPoint.position : fallbackPosition;
    }

    public Vector3 ClampCameraPosition(Vector3 desiredPosition, Camera cameraToClamp)
    {
        if (cameraToClamp == null)
            return desiredPosition;

        Bounds bounds = WorldBounds;
        float verticalExtent = cameraToClamp.orthographic ? cameraToClamp.orthographicSize : 0f;
        float horizontalExtent = verticalExtent * cameraToClamp.aspect;

        desiredPosition.x = ClampAxis(desiredPosition.x, bounds.min.x + horizontalExtent, bounds.max.x - horizontalExtent, bounds.center.x);
        desiredPosition.y = ClampAxis(desiredPosition.y, bounds.min.y + verticalExtent, bounds.max.y - verticalExtent, bounds.center.y);
        return desiredPosition;
    }

    public static CameraRoomBounds FindRoomForPosition(Vector2 worldPosition, CameraRoomBounds currentRoom)
    {
        if (currentRoom != null && currentRoom.isActiveAndEnabled && currentRoom.Contains(worldPosition))
            return currentRoom;

        CameraRoomBounds[] rooms = FindObjectsByType<CameraRoomBounds>(FindObjectsSortMode.None);
        foreach (CameraRoomBounds room in rooms)
        {
            if (room != null && room.isActiveAndEnabled && room.Contains(worldPosition))
                return room;
        }

        return currentRoom;
    }

    static float ClampAxis(float value, float min, float max, float fallback)
    {
        if (min > max)
            return fallback;

        return Mathf.Clamp(value, min, max);
    }

    void OnDrawGizmos()
    {
        Bounds bounds = WorldBounds;
        Gizmos.color = gizmoColor;
        Gizmos.DrawCube(bounds.center, bounds.size);
        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }
}
