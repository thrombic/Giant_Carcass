using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal; // for Light2D — remove if not using URP

/// <summary>
/// LightningOrb — attach to an empty GameObject at the orb's center.
///
/// SETUP:
///   1. Add this script to an empty GameObject ("LightningOrb")
///   2. Optionally assign a soft radial white circle sprite to GlowSprite
///   3. If using URP: add a Light2D component to this GameObject
///   4. Call Activate() / Deactivate() from your spell or character controller
///
/// LAYERS CREATED AUTOMATICALLY:
///   - N internal arc LineRenderers (child GameObjects)
///   - 1 glow SpriteRenderer (child GameObject)
///   - 1 Point Light 2D (if URP — assign in Inspector or auto-found)
///
/// CHARGE MECHANIC:
///   Call SetCharge(0f–1f) every frame to scale the orb up and
///   increase arc count/chaos — good for a hold-to-charge ability.
/// </summary>
public class LightningOrb : MonoBehaviour
{
    // ── Inspector ────────────────────────────────────────────────
    [Header("Orb Shape")]
    public float radius = 0.7f;   // world-space radius
    [Range(2, 20)]
    public int arcCount = 7;      // internal lightning arcs
    [Range(4, 24)]
    public int arcSegments = 12;     // points per arc
    public float chaos = 0.18f;  // perpendicular displacement (fraction of radius)
    public float flickerRate = 0.04f;  // seconds between redraws

    [Header("Outward Bolts")]
    public bool spawnBolts = true;
    [Range(0f, 1f)]
    public float boltChance = 0.25f;  // chance per flicker of a bolt spawning
    public float boltLengthMin = 0.3f;
    public float boltLengthMax = 0.9f;
    public float boltLifetime = 0.12f;

    [Header("Line Widths")]
    public float arcCoreWidth = 0.025f;
    public float arcGlowWidth = 0.08f;
    public float boltCoreWidth = 0.015f;
    public float boltGlowWidth = 0.05f;

    [Header("Color")]
    public Color coreColor = new Color(0.85f, 0.97f, 1.00f, 1.00f);
    public Color glowColor = new Color(0.20f, 0.55f, 1.00f, 0.40f);
    public Color boltColor = new Color(0.70f, 0.88f, 1.00f, 0.80f);

    [Header("Glow Sprite (optional)")]
    public Sprite glowSprite;     // assign a soft white radial circle texture
    public float glowScale = 4f; // size relative to radius

    [Header("URP Light2D (optional)")]
    public Light2D orbLight;      // assign a Point Light 2D, or leave null
    public float lightIntensity = 1.8f;
    public float lightRadius = 2.5f;
    public float lightFlickerAmount = 0.15f;

    [Header("Charge")]
    [Range(0f, 1f)]
    public float charge = 0f;     // set this from your controller (0=idle, 1=full)
    public float maxRadiusScale = 1.4f;  // radius multiplier at full charge
    public int maxExtraArcs = 8;     // extra arcs added at full charge

    // ── Private ──────────────────────────────────────────────────
    LineRenderer[] arcLRs;
    LineRenderer[] arcGlowLRs;

    SpriteRenderer glowSR;
    Material lrMaterial;

    List<BoltInstance> activeBolts = new List<BoltInstance>();
    List<GameObject> boltPool = new List<GameObject>();

    Coroutine flickerRoutine;
    bool isActive;
    float currentRadius;

    // ── Bolt data ─────────────────────────────────────────────────
    struct BoltInstance
    {
        public LineRenderer coreLR;
        public LineRenderer glowLR;
        public float life;
        public float maxLife;
    }

    // ─────────────────────────────────────────────────────────────
    #region Unity Messages

    void Awake()
    {
        lrMaterial = new Material(Shader.Find("Sprites/Default"));
        currentRadius = radius;

        // Internal arc renderers (core + glow per arc)
        int maxArcs = arcCount + maxExtraArcs;
        arcLRs = new LineRenderer[maxArcs];
        arcGlowLRs = new LineRenderer[maxArcs];

        for (int i = 0; i < maxArcs; i++)
        {
            arcLRs[i] = CreateLR($"_Arc{i}", arcCoreWidth, coreColor);
            arcGlowLRs[i] = CreateLR($"_ArcGlow{i}", arcGlowWidth, glowColor);
        }

        // Glow sprite
        if (glowSprite != null)
        {
            var glowGO = new GameObject("_Glow");
            glowGO.transform.SetParent(transform, false);
            glowSR = glowGO.AddComponent<SpriteRenderer>();
            glowSR.sprite = glowSprite;
            glowSR.color = glowColor;
            glowSR.material = new Material(Shader.Find("Sprites/Additive"));
            glowSR.enabled = false;
        }

        // Auto-find Light2D if not assigned
        if (orbLight == null)
            orbLight = GetComponent<Light2D>();

        SetAllVisible(false);
    }

    void Update()
    {
        if (!isActive) return;

        // Scale orb with charge
        currentRadius = radius * Mathf.Lerp(1f, maxRadiusScale, charge);

        // Update glow sprite scale
        if (glowSR != null)
            glowSR.transform.localScale = Vector3.one * (currentRadius / radius) * glowScale;

        // Pulse light with charge and slight random flicker
        if (orbLight != null)
        {
            float flicker = 1f + Random.Range(-lightFlickerAmount, lightFlickerAmount);
            orbLight.intensity = lightIntensity * (0.7f + charge * 0.3f) * flicker;
            orbLight.pointLightOuterRadius = lightRadius * (1f + charge * 0.3f);
        }

        // Tick bolt lifetimes
        for (int i = activeBolts.Count - 1; i >= 0; i--)
        {
            var bolt = activeBolts[i];
            bolt.life -= Time.deltaTime;

            if (bolt.life <= 0f)
            {
                bolt.coreLR.enabled = false;
                bolt.glowLR.enabled = false;
                activeBolts.RemoveAt(i);
            }
            else
            {
                // Fade bolt out
                float a = bolt.life / bolt.maxLife;
                SetLRAlpha(bolt.coreLR, coreColor, a);
                SetLRAlpha(bolt.glowLR, boltColor, a * 0.6f);
                activeBolts[i] = bolt;
            }
        }
    }

    #endregion

    // ─────────────────────────────────────────────────────────────
    #region Public API

    /// <summary>Start the orb flickering.</summary>
    public void Activate()
    {
        if (isActive) return;
        isActive = true;
        SetAllVisible(true);
        flickerRoutine = StartCoroutine(FlickerLoop());
    }

    /// <summary>Stop the orb immediately.</summary>
    public void Deactivate()
    {
        isActive = false;
        if (flickerRoutine != null) StopCoroutine(flickerRoutine);
        SetAllVisible(false);
        ClearBolts();
    }

    /// <summary>
    /// Set charge level 0–1.
    /// Scales radius, adds arcs, and increases bolt spawn rate.
    /// Call this from your character controller every frame.
    /// </summary>
    public void SetCharge(float value)
    {
        charge = Mathf.Clamp01(value);
    }

    /// <summary>Force an immediate redraw (e.g. on ability activation).</summary>
    public void ForceRedraw() => Redraw();

    #endregion

    // ─────────────────────────────────────────────────────────────
    #region Flicker Loop

    IEnumerator FlickerLoop()
    {
        while (isActive)
        {
            Redraw();
            yield return new WaitForSeconds(flickerRate);
        }
    }

    void Redraw()
    {
        // Pulse glow sprite alpha
        if (glowSR != null)
        {
            Color c = glowColor;
            c.a = glowColor.a * Random.Range(0.8f, 1.25f) * (0.6f + charge * 0.4f);
            glowSR.color = c;
            glowSR.enabled = true;
        }

        // How many arcs to draw this frame
        int liveArcs = arcCount + Mathf.RoundToInt(charge * maxExtraArcs);
        liveArcs = Mathf.Min(liveArcs, arcLRs.Length);

        for (int i = 0; i < arcLRs.Length; i++)
        {
            bool visible = i < liveArcs;
            arcLRs[i].enabled = visible;
            arcGlowLRs[i].enabled = visible;

            if (visible) DrawInternalArc(arcLRs[i], arcGlowLRs[i]);
        }

        // Maybe spawn an outward bolt
        if (spawnBolts)
        {
            float spawnChance = boltChance * (0.2f + charge * 0.8f);
            if (Random.value < spawnChance)
                SpawnBolt();
        }
    }

    #endregion

    // ─────────────────────────────────────────────────────────────
    #region Internal Arcs

    void DrawInternalArc(LineRenderer coreLR, LineRenderer glowLR)
    {
        // Two random points on the orb surface
        Vector2 a = RandomOnCircle(currentRadius);
        Vector2 b = RandomOnCircle(currentRadius);

        // Scale chaos with charge for wilder arcs when charged up
        float liveChaos = chaos * (1f + charge * 0.8f);

        Vector2[] pts = GenerateArcPoints(a, b, arcSegments, liveChaos);

        ApplyArcToLR(coreLR, pts);
        ApplyArcToLR(glowLR, pts);
    }

    void ApplyArcToLR(LineRenderer lr, Vector2[] pts)
    {
        lr.positionCount = pts.Length;
        for (int i = 0; i < pts.Length; i++)
            // Convert local 2D offsets to world-space positions
            lr.SetPosition(i, transform.TransformPoint(new Vector3(pts[i].x, pts[i].y, 0f)));
    }

    #endregion

    // ─────────────────────────────────────────────────────────────
    #region Outward Bolts

    void SpawnBolt()
    {
        float angle = Random.Range(0f, Mathf.PI * 2f);
        float length = Random.Range(boltLengthMin, boltLengthMax);
        int segs = Random.Range(5, 9);

        // Start at orb surface, end outside
        Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        Vector2 start = dir * currentRadius;
        Vector2 end = dir * (currentRadius + length);

        float liveDisplace = chaos * currentRadius * 0.5f;
        Vector2[] pts = GenerateArcPoints(start, end, segs, liveDisplace / currentRadius);

        // Get or create a bolt LR pair from pool
        var (coreLR, glowLR) = GetBoltLRs();

        coreLR.startWidth = coreLR.endWidth = boltCoreWidth;
        glowLR.startWidth = glowLR.endWidth = boltGlowWidth;

        ApplyArcToLR(coreLR, pts);
        ApplyArcToLR(glowLR, pts);

        coreLR.enabled = glowLR.enabled = true;

        activeBolts.Add(new BoltInstance
        {
            coreLR = coreLR,
            glowLR = glowLR,
            life = boltLifetime,
            maxLife = boltLifetime,
        });
    }

    (LineRenderer core, LineRenderer glow) GetBoltLRs()
    {
        // Reuse a pooled bolt GameObject pair if available
        foreach (var go in boltPool)
        {
            var lrs = go.GetComponentsInChildren<LineRenderer>();
            if (lrs.Length == 2 && !lrs[0].enabled)
                return (lrs[0], lrs[1]);
        }

        // Create new
        var parent = new GameObject("_Bolt");
        parent.transform.SetParent(transform, false);
        boltPool.Add(parent);

        var core = CreateLR("Core", boltCoreWidth, coreColor, parent.transform);
        var glow = CreateLR("Glow", boltGlowWidth, boltColor, parent.transform);
        return (core, glow);
    }

    void ClearBolts()
    {
        foreach (var b in activeBolts)
        {
            if (b.coreLR) b.coreLR.enabled = false;
            if (b.glowLR) b.glowLR.enabled = false;
        }
        activeBolts.Clear();
    }

    #endregion

    // ─────────────────────────────────────────────────────────────
    #region Arc Math

    /// <summary>
    /// Midpoint displacement between two points, clamped inside the orb radius.
    /// Points are in local orb space (center = 0,0).
    /// </summary>
    Vector2[] GenerateArcPoints(Vector2 start, Vector2 end, int numSegs, float displaceFraction)
    {
        Vector2[] pts = new Vector2[numSegs + 1];
        pts[0] = start;
        pts[numSegs] = end;

        Vector2 dir = (end - start).normalized;
        Vector2 perp = new Vector2(-dir.y, dir.x);
        float disp = currentRadius * displaceFraction;

        for (int i = 1; i < numSegs; i++)
        {
            float tt = (float)i / numSegs;
            Vector2 linear = Vector2.Lerp(start, end, tt);
            float taper = Mathf.Sin(tt * Mathf.PI);
            float offset = Random.Range(-disp, disp) * taper;
            Vector2 pt = linear + perp * offset;

            // Clamp inside orb so internal arcs don't escape
            if (pt.magnitude > currentRadius * 0.94f)
                pt = pt.normalized * currentRadius * 0.94f;

            pts[i] = pt;
        }

        return pts;
    }

    Vector2 RandomOnCircle(float r)
    {
        float a = Random.Range(0f, Mathf.PI * 2f);
        return new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
    }

    #endregion

    // ─────────────────────────────────────────────────────────────
    #region Helpers

    LineRenderer CreateLR(string goName, float width, Color color,
                          Transform parent = null)
    {
        var go = new GameObject(goName);
        go.transform.SetParent(parent != null ? parent : transform, false);

        var lr = go.AddComponent<LineRenderer>();
        lr.startWidth = lr.endWidth = width;
        lr.startColor = lr.endColor = color;
        lr.useWorldSpace = true;
        lr.numCapVertices = 3;
        lr.material = lrMaterial;
        lr.enabled = false;
        return lr;
    }

    void SetLRAlpha(LineRenderer lr, Color baseColor, float alpha)
    {
        Color c = baseColor;
        c.a = alpha;
        lr.startColor = lr.endColor = c;
    }

    void SetAllVisible(bool v)
    {
        foreach (var lr in arcLRs) if (lr) lr.enabled = v;
        foreach (var lr in arcGlowLRs) if (lr) lr.enabled = v;
        if (glowSR) glowSR.enabled = v;

        if (orbLight)
        {
            orbLight.enabled = v;
            orbLight.intensity = v ? lightIntensity : 0f;
        }

        if (!v) ClearBolts();
    }

    #endregion
}