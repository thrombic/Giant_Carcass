using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[ExecuteAlways, DisallowMultipleComponent]
public sealed class GameHud : MonoBehaviour
{
    [SerializeField] private Vector2 screenOffset = new Vector2(18f, 18f);
    [SerializeField, Range(0.5f, 2f)] private float hudScale = 1f;

    private static GameHud instance;
    private PlayerController player;
    private HealthSystem health;
    private PlayerEnergy energy;
    private RectTransform panel;
    private Text energyValue;
    private Text flareValue;
    private readonly List<Image> tankFills = new List<Image>();
    private float playerSearchTimer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetInstance() => instance = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void EnsureHudExists()
    {
        if (FindFirstObjectByType<GameHud>() == null)
            new GameObject("Game HUD").AddComponent<GameHud>();
    }

    private void Awake()
    {
        if (!Application.isPlaying)
            return;

        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        if (Application.isPlaying && instance != null && instance != this)
            return;

        EnsureInterface();
        FindPlayer();
        RefreshValues();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void Update()
    {
        if (Application.isPlaying && instance != null && instance != this)
            return;

        if (energyValue == null)
            EnsureInterface();

        if (player == null || health == null || energy == null)
        {
            playerSearchTimer -= Time.unscaledDeltaTime;
            if (playerSearchTimer <= 0f)
                FindPlayer();
        }

        panel.anchoredPosition = new Vector2(screenOffset.x, -screenOffset.y);
        panel.localScale = Vector3.one * hudScale;
        RefreshValues();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => FindPlayer();

    private void FindPlayer()
    {
        player = FindFirstObjectByType<PlayerController>();
        health = player != null ? player.GetComponent<HealthSystem>() : null;
        energy = player != null ? player.GetComponent<PlayerEnergy>() : null;
        playerSearchTimer = 0.5f;
    }

    private void RefreshValues()
    {
        if (energyValue == null || flareValue == null)
            return;

        int tankCount = energy != null ? energy.EnergyTanks : 0;
        int current = !Application.isPlaying && energy != null
            ? energy.Capacity
            : (health != null ? health.CurrentHealth : 0);
        energyValue.text = (current % PlayerEnergy.EnergyPerTank).ToString("00");
        energyValue.color = current <= 20 && Application.isPlaying
            ? new Color(1f, 0.3f, 0.4f)
            : Color.white;
        int filled = current / PlayerEnergy.EnergyPerTank;

        for (int i = 0; i < tankFills.Count; i++)
        {
            tankFills[i].transform.parent.gameObject.SetActive(i < tankCount);
            tankFills[i].color = i < filled
                ? new Color(1f, 0.15f, 0.55f)
                : new Color(0.14f, 0.09f, 0.16f);
        }

        flareValue.text = player != null ? player.FlareAmmo.ToString("000") : "---";
    }

    private void EnsureInterface()
    {
        // Rebind the generated hierarchy after script reloads during Play mode.
        Transform existing = transform.Find("HUD Canvas");
        if (existing != null)
        {
            panel = existing.Find("Status Panel") as RectTransform;
            energyValue = existing.Find("Status Panel/Energy Value")?.GetComponent<Text>();
            flareValue = existing.Find("Status Panel/Flare Value")?.GetComponent<Text>();
            if (panel != null && energyValue != null && flareValue != null)
            {
                tankFills.Clear();
                for (int i = 0; i < PlayerEnergy.MaximumTanks; i++)
                    tankFills.Add(panel.Find($"Tank {i}/Fill").GetComponent<Image>());
                return;
            }

            if (Application.isPlaying)
                Destroy(existing.gameObject);
            else
                DestroyImmediate(existing.gameObject);
        }

        BuildInterface();
    }

    private void BuildInterface()
    {
        tankFills.Clear();
        GameObject canvasObject = new GameObject("HUD Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        canvas.pixelPerfect = true;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 1f;

        panel = CreateImage("Status Panel", canvasObject.transform, new Color(0.025f, 0.04f, 0.075f, 0.94f));
        SetTopLeft(panel, new Vector2(screenOffset.x, -screenOffset.y), new Vector2(312f, 112f));
        SetTopLeft(CreateImage("Accent", panel, new Color(0.15f, 0.85f, 1f)), Vector2.zero, new Vector2(3f, 112f));

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        CreateText("Energy Label", "ENERGY", font, 18, new Vector2(15f, -13f), new Vector2(96f, 28f));
        energyValue = CreateText("Energy Value", "99", font, 38, new Vector2(15f, -39f), new Vector2(85f, 48f));

        for (int i = 0; i < PlayerEnergy.MaximumTanks; i++)
        {
            RectTransform border = CreateImage($"Tank {i}", panel, new Color(0.58f, 0.3f, 0.47f));
            SetTopLeft(border, new Vector2(118f + i % 7 * 24f, -15f - i / 7 * 22f), new Vector2(19f, 16f));
            RectTransform fill = CreateImage("Fill", border, new Color(1f, 0.15f, 0.55f));
            SetTopLeft(fill, new Vector2(3f, -3f), new Vector2(13f, 10f));
            tankFills.Add(fill.GetComponent<Image>());
        }

        CreateText("Flare Label", "FLARES", font, 16, new Vector2(118f, -67f), new Vector2(85f, 28f));
        flareValue = CreateText("Flare Value", "010", font, 25, new Vector2(219f, -62f), new Vector2(77f, 36f));
        flareValue.color = new Color(0.3f, 0.9f, 1f);

        // The scene stores the HUD controller; its UI is rebuilt in editor and builds.
        foreach (Transform child in canvasObject.GetComponentsInChildren<Transform>(true))
            child.gameObject.hideFlags = HideFlags.DontSaveInEditor;
    }

    private Text CreateText(string objectName, string value, Font font, int size, Vector2 position, Vector2 dimensions)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(panel, false);
        Text text = textObject.GetComponent<Text>();
        text.text = value;
        text.font = font;
        text.fontSize = size;
        text.fontStyle = FontStyle.Bold;
        text.color = new Color(0.85f, 0.94f, 1f);
        text.alignment = TextAnchor.MiddleLeft;
        text.raycastTarget = false;
        SetTopLeft(text.rectTransform, position, dimensions);
        return text;
    }

    private static RectTransform CreateImage(string objectName, Transform parent, Color color)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image.rectTransform;
    }

    private static void SetTopLeft(RectTransform rect, Vector2 position, Vector2 dimensions)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;
    }
}
