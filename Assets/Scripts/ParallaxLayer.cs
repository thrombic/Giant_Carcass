using UnityEngine;

// Attach to a unit quad under the orthographic gameplay camera.
[ExecuteAlways, DefaultExecutionOrder(100), DisallowMultipleComponent]
[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
public sealed class ParallaxLayer : MonoBehaviour
{
    [Tooltip("On-screen travel per unit of camera movement. 0 pins the art to the screen, 1 matches the world.")]
    [SerializeField] private Vector2 scrollFactor = Vector2.one;
    [Tooltip("Width of one copy of the artwork in world units. Height follows the texture aspect ratio.")]
    [SerializeField, Min(0.01f)] private float tileWidth = 48f;
    [Tooltip("Shift the artwork in fractions of one tile.")]
    [SerializeField] private Vector2 tileOffset;
    [SerializeField] private Color tint = Color.white;

    private static readonly int BaseMapST = Shader.PropertyToID("_BaseMap_ST");
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    private Camera view;
    private MeshRenderer quad;
    private MaterialPropertyBlock properties;

    private void OnEnable()
    {
        view = GetComponentInParent<Camera>();
        quad = GetComponent<MeshRenderer>();
        properties = new MaterialPropertyBlock();
        UpdateLayer();
    }

    private void LateUpdate() => UpdateLayer();

    private void UpdateLayer()
    {
        if (view == null || !view.orthographic || quad.sharedMaterial == null)
            return;

        Texture texture = quad.sharedMaterial.mainTexture;
        if (texture == null)
            return;

        float height = 2f * view.orthographicSize;
        float width = height * view.aspect;
        transform.localScale = new Vector3(width, height, 1f);

        float tileHeight = tileWidth * texture.height / texture.width;
        Vector3 cameraPosition = view.transform.position;
        float tilesX = width / tileWidth;
        float tilesY = height / tileHeight;
        // Anchor the sampled region at the camera's absolute position so room snaps
        // and respawns land on the same pattern as scrolling there continuously.
        float u = 0.5f + tileOffset.x + cameraPosition.x * scrollFactor.x / tileWidth;
        float v = 0.5f + tileOffset.y + cameraPosition.y * scrollFactor.y / tileHeight;
        properties.SetVector(BaseMapST, new Vector4(tilesX, tilesY, u - tilesX * 0.5f, v - tilesY * 0.5f));
        properties.SetColor(BaseColor, tint);
        quad.SetPropertyBlock(properties);
    }
}
