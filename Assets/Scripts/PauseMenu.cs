using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    private const string StartMenuScene = "StartMenu"; // change to your start menu scene name
    private static readonly string[] TabNames = { "Map", "Inventory", "Settings" };

    private static PauseMenu instance;

    private GameObject root;
    private GameObject[] panels;
    private Image[] tabImages;
    private GameObject ownEventSystem;
    private bool paused;
    private bool inStartMenu;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;
        new GameObject("PauseMenu").AddComponent<PauseMenu>();
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        inStartMenu = SceneManager.GetActiveScene().name == StartMenuScene;
        BuildUI();
        root.SetActive(false);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (instance != this) return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Time.timeScale = 1f;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        inStartMenu = scene.name == StartMenuScene;
        SetPaused(false);

        // If we created an EventSystem, drop it so the new scene's own one doesn't clash.
        if (ownEventSystem != null)
            Destroy(ownEventSystem);
    }

    void Update()
    {
        if (inStartMenu) return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            SetPaused(!paused);
    }

    public void SetPaused(bool value)
    {
        paused = value;
        Time.timeScale = value ? 0f : 1f;
        AudioListener.pause = value;
        root.SetActive(value);

        if (value)
        {
            EnsureEventSystem();
            ShowTab(0);
        }
    }

    void ShowTab(int index)
    {
        for (int i = 0; i < panels.Length; i++)
        {
            panels[i].SetActive(i == index);
            tabImages[i].color = i == index
                ? new Color(0.35f, 0.55f, 0.9f, 1f)
                : new Color(0.2f, 0.2f, 0.25f, 1f);
        }
    }

    void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;
        ownEventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

    // ---------- UI construction ----------

    void BuildUI()
    {
        var canvasGO = new GameObject("PauseCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);

        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // Dark full-screen overlay
        var overlay = NewRect("Overlay", canvasGO.transform);
        Stretch(overlay);
        overlay.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);
        root = overlay.gameObject;

        // Tab bar
        var tabBar = NewRect("TabBar", overlay);
        tabBar.anchorMin = tabBar.anchorMax = tabBar.pivot = new Vector2(0.5f, 1f);
        tabBar.anchoredPosition = new Vector2(0f, -60f);
        tabBar.sizeDelta = new Vector2(900f, 70f);
        var layout = tabBar.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 20f;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        // Content area
        var content = NewRect("Content", overlay);
        content.anchorMin = new Vector2(0.1f, 0.1f);
        content.anchorMax = new Vector2(0.9f, 0.8f);
        content.offsetMin = content.offsetMax = Vector2.zero;
        content.gameObject.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.12f, 0.95f);

        panels = new GameObject[TabNames.Length];
        tabImages = new Image[TabNames.Length];

        for (int i = 0; i < TabNames.Length; i++)
        {
            int index = i; // capture for the click handler

            // Tab button
            var tab = NewRect("Tab_" + TabNames[i], tabBar);
            var img = tab.gameObject.AddComponent<Image>();
            var btn = tab.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => ShowTab(index));
            AddLabel(tab, TabNames[i], 32);
            tabImages[i] = img;

            // Panel (swap the placeholder label for real content later)
            var panel = NewRect("Panel_" + TabNames[i], content);
            Stretch(panel);
            AddLabel(panel, TabNames[i] + " (coming soon)", 40);
            panels[i] = panel.gameObject;
        }
    }

    static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
    }

    static void AddLabel(Transform parent, string text, int size)
    {
        var r = NewRect("Label", parent);
        Stretch(r);
        var t = r.gameObject.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.text = text;
        t.fontSize = size;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;
        t.raycastTarget = false;
    }
}