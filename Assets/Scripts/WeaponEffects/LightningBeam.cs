using System.Collections;
using UnityEngine;

/// <summary>
/// LightningBeam — attach to any GameObject with a LineRenderer.
/// 
/// SETUP:
///   1. Add this script + a LineRenderer to a GameObject (e.g. "LightningEmitter")
///   2. Set Target in the Inspector (the hit point / enemy transform)
///   3. Optionally assign a Particles/Additive material to both LineRenderers
///   4. Call StartFiring() / StopFiring() from your weapon script
///
/// LAYERS:
///   Core  — thin, bright white line  (this GameObject's LineRenderer)
///   Glow  — wide, colored, semi-transparent (child GameObject auto-created)
///   Branches — up to MaxBranches short offshoots (child GameObjects)
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class LightningBeam : MonoBehaviour
{
    // ── Inspector ────────────────────────────────────────────────
    [Header("Target")]
    public Transform target;               // drag in the hit-point transform

    [Header("Beam Shape")]
    [Range(6, 32)]
    public int   segments     = 16;        // more = smoother but costlier
    public float chaos        = 0.4f;      // perpendicular displacement amplitude
    public float flickerRate  = 0.04f;     // seconds between redraws (~25fps flicker)

    [Header("Line Width")]
    public float coreWidth    = 0.05f;
    public float glowWidth    = 0.28f;

    [Header("Color")]
    public Color coreColor    = new Color(0.85f, 0.97f, 1.00f, 1.00f);
    public Color glowColor    = new Color(0.20f, 0.60f, 1.00f, 0.35f);
    public Color branchColor  = new Color(0.55f, 0.80f, 1.00f, 0.70f);

    [Header("Branches")]
    [Range(0f, 1f)]
    public float branchChance = 0.35f;     // probability per segment of sprouting
    [Range(0, 5)]
    public int   maxBranches  = 3;
    public float branchLength = 0.5f;      // fraction of main beam length

    [Header("Impact Particles (optional)")]
    public ParticleSystem impactSparks;    // assign a spark PS for hit effects

    // ── Private ──────────────────────────────────────────────────
    LineRenderer   coreLR;
    LineRenderer   glowLR;
    LineRenderer[] branchLRs;

    bool      firing;
    Coroutine fireCoroutine;

    // ─────────────────────────────────────────────────────────────
    #region Unity Messages

    void Awake()
    {
        // Core beam — uses this GameObject's LineRenderer
        coreLR = GetComponent<LineRenderer>();
        InitLR(coreLR, coreWidth, coreColor);

        // Glow layer — wider, transparent, child GO
        glowLR = CreateChildLR("_Glow", glowWidth, glowColor);

        // Branch renderers
        branchLRs = new LineRenderer[maxBranches];
        for (int i = 0; i < maxBranches; i++)
            branchLRs[i] = CreateChildLR($"_Branch{i}", coreWidth * 0.45f, branchColor);

        SetAllVisible(false);
    }

    #endregion

    // ─────────────────────────────────────────────────────────────
    #region Public API

    /// <summary>Start emitting the lightning beam toward Target.</summary>
    public void StartFiring()
    {
        if (firing) return;
        firing = true;
        SetAllVisible(true);
        fireCoroutine = StartCoroutine(FireLoop());

        if (impactSparks != null)
            impactSparks.Play();
    }

    /// <summary>Stop the beam immediately.</summary>
    public void StopFiring()
    {
        firing = false;
        if (fireCoroutine != null) StopCoroutine(fireCoroutine);
        SetAllVisible(false);

        if (impactSparks != null)
            impactSparks.Stop();
    }

    /// <summary>Toggle firing state.</summary>
    public void ToggleFiring()
    {
        if (firing) StopFiring();
        else        StartFiring();
    }

    /// <summary>Redirect the beam to a new target at runtime.</summary>
    public void SetTarget(Transform newTarget) => target = newTarget;

    /// <summary>Change chaos amplitude at runtime (e.g. scale with charge level).</summary>
    public void SetChaos(float value) => chaos = Mathf.Max(0f, value);

    #endregion

    // ─────────────────────────────────────────────────────────────
    #region Beam Generation

    IEnumerator FireLoop()
    {
        while (firing)
        {
            if (target != null)
                Redraw();

            yield return new WaitForSeconds(flickerRate);
        }
    }

    void Redraw()
    {
        Vector2 start = transform.position;
        Vector2 end   = target.position;

        // Generate main beam points
        Vector2[] pts = GeneratePoints(start, end, segments, chaos);

        // Apply to core + glow
        ApplyPoints(coreLR, pts);
        ApplyPoints(glowLR, pts);

        // Move impact sparks to target position
        if (impactSparks != null)
            impactSparks.transform.position = end;

        // Branches
        int b = 0;
        for (int i = 1; i < pts.Length - 1 && b < maxBranches; i++)
        {
            if (Random.value < branchChance / pts.Length)
            {
                Vector2 dir  = (end - start).normalized;
                Vector2 perp = new Vector2(-dir.y, dir.x);

                // Branch endpoint: sideways + slightly forward
                float   bDist = Vector2.Distance(start, end) * branchLength;
                Vector2 bEnd  = pts[i]
                              + perp * Random.Range(-bDist * 0.4f,  bDist * 0.4f)
                              + dir  * Random.Range( bDist * 0.05f, bDist * 0.3f);

                Vector2[] bPts = GeneratePoints(pts[i], bEnd, 6, chaos * 0.5f);
                ApplyPoints(branchLRs[b], bPts);
                b++;
            }
        }

        // Hide unused branch renderers
        for (; b < maxBranches; b++)
            branchLRs[b].enabled = false;
    }

    /// <summary>
    /// Midpoint displacement algorithm.
    /// Divides the segment into numSegs equal parts, then displaces each
    /// midpoint perpendicular to the beam by a random amount tapered at both
    /// ends — so the beam always starts and ends exactly on target.
    /// </summary>
    Vector2[] GeneratePoints(Vector2 start, Vector2 end,
                              int numSegs, float displacement)
    {
        Vector2[] pts = new Vector2[numSegs + 1];
        pts[0]        = start;
        pts[numSegs]  = end;

        Vector2 dir  = (end - start).normalized;
        Vector2 perp = new Vector2(-dir.y, dir.x);  // rotate 90°

        for (int i = 1; i < numSegs; i++)
        {
            float   t      = (float)i / numSegs;
            Vector2 linear = Vector2.Lerp(start, end, t);

            // Sin taper: displacement is 0 at both ends, max in the middle
            float taper  = Mathf.Sin(t * Mathf.PI);
            float offset = Random.Range(-displacement, displacement) * taper;

            pts[i] = linear + perp * offset;
        }

        return pts;
    }

    #endregion

    // ─────────────────────────────────────────────────────────────
    #region Helpers

    LineRenderer CreateChildLR(string goName, float width, Color color)
    {
        var go = new GameObject(goName);
        go.transform.SetParent(transform, false);

        var lr = go.AddComponent<LineRenderer>();
        InitLR(lr, width, color);
        return lr;
    }

    void InitLR(LineRenderer lr, float width, Color color)
    {
        lr.startWidth    = lr.endWidth = width;
        lr.startColor    = lr.endColor = color;
        lr.useWorldSpace = true;                       // IMPORTANT: keeps beam fixed in world
        lr.numCapVertices = 4;                         // rounded ends
        lr.numCornerVertices = 2;

        // Use Additive blending for the glow/light feel.
        // If you have a custom material, assign it in the Inspector instead.
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.enabled  = false;
    }

    void ApplyPoints(LineRenderer lr, Vector2[] pts)
    {
        lr.enabled       = true;
        lr.positionCount = pts.Length;
        for (int i = 0; i < pts.Length; i++)
            lr.SetPosition(i, pts[i]);   // Z stays 0 for 2D
    }

    void SetAllVisible(bool visible)
    {
        if (coreLR) coreLR.enabled = visible;
        if (glowLR) glowLR.enabled = visible;
        if (branchLRs != null)
            foreach (var b in branchLRs)
                if (b) b.enabled = visible;
    }

    #endregion
}


// ═════════════════════════════════════════════════════════════════
// EXAMPLE USAGE — put this on your Player / Weapon GameObject
// ═════════════════════════════════════════════════════════════════

/// <summary>
/// Simple example: hold Space (or left mouse) to fire the beam.
/// Scales chaos with how long the player has been holding the button
/// for a "charging" feel.
/// </summary>
public class LightningWeaponExample : MonoBehaviour
{
    public LightningBeam beam;

    [Header("Charge")]
    public float maxChargeTime  = 1.5f;    // seconds to reach full chaos
    public float minChaos       = 0.1f;
    public float maxChaos       = 0.8f;

    float chargeTimer;

    void Update()
    {
        bool held = Input.GetKey(KeyCode.Space) || Input.GetMouseButton(0);

        if (held)
        {
            if (!beam.enabled) beam.StartFiring();          // first frame

            // Ramp up chaos the longer the button is held
            chargeTimer += Time.deltaTime;
            float charged = Mathf.Clamp01(chargeTimer / maxChargeTime);
            beam.SetChaos(Mathf.Lerp(minChaos, maxChaos, charged));
        }
        else
        {
            if (chargeTimer > 0f)
            {
                beam.StopFiring();
                chargeTimer = 0f;
            }
        }
    }
}
