using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Attach this to object. Continuously measures how much light is
/// falling on it from ALL relevant lights in the scene (flashlight, lamps,
/// torches, moonlight, etc), not just one designated source. Triggers
/// aggressive behavior once total exposure crosses a threshold.
/// </summary>
public class LightDetector : MonoBehaviour
{
    [Header("Detection Settings")]
    [Tooltip("Layers that block light (walls, props, etc).")]
    public LayerMask occlusionMask;

    [Tooltip("Total combined light exposure needed to trigger aggro.")]
    public float exposureThreshold = 1.0f;

    [Tooltip("Exposure must drop below this (with hysteresis) to calm down.")]
    public float calmThreshold = 0.3f;

    [Tooltip("How long exposure must stay above threshold before triggering.")]
    public float detectionDelay = 0.25f;

    [Tooltip("How often (seconds) to rescan for lights in the scene.")]
    public float rescanInterval = .25f;

    private List<Light2D> sceneLights = new List<Light2D>();
    private Light2D selfLight;
    private float rescanTimer = 0f;

    public float CurrentExposure { get; private set; }


    void Start()
    {
        selfLight = GetComponent<Light2D>();
        RescanLights();
    }

    void FixedUpdate()
    {
        rescanTimer += Time.deltaTime;
        if (rescanTimer >= rescanInterval)
        {
            RescanLights();
            rescanTimer = 0f;
        }

        CurrentExposure = CalculateTotalExposure();
    }

    /// <summary>
    /// Refresh the list of lights currently active in the scene.
    /// Cheaper than calling FindObjectsOfType every frame.
    /// </summary>
    private void RescanLights()
    {
        sceneLights.Clear();
        Light2D[] found = FindObjectsByType<Light2D>(FindObjectsSortMode.None);
        sceneLights.AddRange(found);
    }

    private float CalculateTotalExposure()
    {
        float total = 0f;

        foreach (var light in sceneLights)
        {
            if (light == null || !light.isActiveAndEnabled || light == selfLight) continue;
            total += GetContribution(light);
        }

        return total;
    }

    private float GetContribution(Light2D light)
    {
        switch (light.lightType)
        {
            case Light2D.LightType.Global:
                // Ambient/global light affects everything uniformly,
                // no position, range, or occlusion to check.
                return light.intensity;

            case Light2D.LightType.Point:
                return PointContribution(light);

            // Freeform, Sprite, and Parametric lights don't expose a simple
            // radius/angle API. Approximate them as an omnidirectional point
            // light using their transform position and intensity.
            case Light2D.LightType.Freeform:
            case Light2D.LightType.Sprite:
            case Light2D.LightType.Parametric:
                return ApproximateShapeContribution(light);

            default:
                return 0f;
        }
    }

    private float PointContribution(Light2D light)
    {
        Vector2 toObj = (Vector2)transform.position - (Vector2)light.transform.position;
        float distance = toObj.magnitude;

        if (distance > light.pointLightOuterRadius) return 0f;

        // Angle check (Point lights support a cone via inner/outer angle;
        // default 360 behaves like an omnidirectional point light)
        if (light.pointLightOuterAngle < 360f)
        {
            float angle = Vector2.Angle(light.transform.up, toObj);
            if (angle > light.pointLightOuterAngle * 0.5f) return 0f;
        }

        if (IsOccluded2D(light.transform.position, toObj, distance)) return 0f;

        return light.intensity * RadialFalloff(distance, light.pointLightInnerRadius,
                                                 light.pointLightOuterRadius, light.falloffIntensity);
    }

    private float ApproximateShapeContribution(Light2D light)
    {
        Vector2 toBat = (Vector2)transform.position - (Vector2)light.transform.position;
        float distance = toBat.magnitude;

        // No exact radius exists on these light types, so use a flat
        // effective radius as an approximation. Tune per-light, or expose
        // this as a serialized field if you need per-instance control.
        float approxRadius = 5f;
        if (distance > approxRadius) return 0f;

        if (IsOccluded2D(light.transform.position, toBat, distance)) return 0f;

        return light.intensity * Mathf.Clamp01(1f - (distance / approxRadius));
    }

    private bool IsOccluded2D(Vector2 lightPos, Vector2 toBat, float distance)
    {
        RaycastHit2D hit = Physics2D.Raycast(lightPos, toBat.normalized, distance, occlusionMask);
        if (hit.collider != null)
        {
            return hit.collider.gameObject != gameObject;
        }
        return false;
    }

    private float RadialFalloff(float distance, float innerRadius, float outerRadius, float falloffIntensity)
    {
        if (distance <= innerRadius) return 1f;
        float t = Mathf.InverseLerp(outerRadius, innerRadius, distance);
        // falloffIntensity (0-1) controls how soft the edge is
        return Mathf.Pow(Mathf.Clamp01(t), Mathf.Max(0.01f, 1f - falloffIntensity));
    }

}