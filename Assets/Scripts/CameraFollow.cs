using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Camera targetCamera;
    public CameraRoomBounds currentRoom;
    public bool autoFindRoom = true;
    public Vector2 followOffset;
    public float smoothSpeed = 5f;
    public bool snapOnStart = true;

    void Awake()
    {
        if (targetCamera == null)
            targetCamera = GetComponent<Camera>();
    }

    void Start()
    {
        if (snapOnStart)
            MoveToTarget(1f);
    }

    void LateUpdate()
    {
        MoveToTarget(1f - Mathf.Exp(-smoothSpeed * Time.deltaTime));
    }

    public void SnapToTarget()
    {
        MoveToTarget(1f);
    }

    void MoveToTarget(float followAmount)
    {
        if (target == null)
            return;

        if (targetCamera == null)
            targetCamera = GetComponent<Camera>();

        if (autoFindRoom)
            currentRoom = CameraRoomBounds.FindRoomForPosition(target.position, currentRoom);

        Vector3 desiredPosition = new Vector3(
            target.position.x + followOffset.x,
            target.position.y + followOffset.y,
            transform.position.z);

        if (currentRoom != null)
            desiredPosition = currentRoom.ClampCameraPosition(desiredPosition, targetCamera);

        transform.position = Vector3.Lerp(transform.position, desiredPosition, Mathf.Clamp01(followAmount));
    }
}
