using System.Collections;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Start menu flow:
///  1. Title state: background shows only its top half, "Press any key" pulses.
///  2. Any key/click/gamepad button: camera scrolls down to the bottom half.
///  3. Menu fades in (New Game / Options / Cool Stuff / Credits).
///
/// The background sprite is stretched so that its HEIGHT is exactly 2x the
/// camera view height and its WIDTH matches the camera view width. That way
/// the camera only ever sees half of the image at a time, at any resolution.
/// </summary>
public class StartMenuController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Camera menuCamera;
    [SerializeField] SpriteRenderer background;
    [SerializeField] CanvasGroup pressAnyKeyGroup;
    [SerializeField] CanvasGroup menuGroup;
    [SerializeField] Selectable firstSelected;

    [Header("Scroll")]
    [SerializeField] float scrollDuration = 1.6f;
    [SerializeField] AnimationCurve scrollCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [Tooltip("How long each button takes to fade in.")]
    [SerializeField] float fadeDuration = 0.6f;
    [Tooltip("Delay between one button starting to fade and the next.")]
    [SerializeField] float buttonStagger = 0.18f;

    [Header("Press Any Key Prompt")]
    [SerializeField] float pulseSpeed = 2.5f;

    [Header("Actions")]
    [Tooltip("Scene loaded by New Game. Must be added to File > Build Profiles / Build Settings.")]
    [SerializeField] string gameSceneName = "Playground";
    public UnityEvent onOptions;
    public UnityEvent onCoolStuff;
    public UnityEvent onCredits;

    enum State { Title, Scrolling, Menu }
    State state = State.Title;

    float titleY;
    int lastW, lastH;
    CanvasGroup[] buttonGroups;

    float ViewHeight => menuCamera.orthographicSize * 2f;
    float MenuY => titleY - ViewHeight;

    void Awake()
    {
        if (menuCamera == null) menuCamera = Camera.main;
        titleY = menuCamera.transform.position.y;

        // Container is "visible" but not clickable; each button starts fully transparent
        // and is faded in individually after the scroll finishes.
        menuGroup.alpha = 1f;
        menuGroup.interactable = false;
        menuGroup.blocksRaycasts = false;

        int count = menuGroup.transform.childCount;
        buttonGroups = new CanvasGroup[count];
        for (int i = 0; i < count; i++)
        {
            var child = menuGroup.transform.GetChild(i);
            var cg = child.GetComponent<CanvasGroup>();
            if (cg == null) cg = child.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            buttonGroups[i] = cg;
        }

        pressAnyKeyGroup.alpha = 1f;
    }

    void Start()
    {
        Layout();
    }

    void Update()
    {
        if (Screen.width != lastW || Screen.height != lastH) Layout();

        if (state != State.Title) return;

        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * pulseSpeed);
        pressAnyKeyGroup.alpha = 0.35f + 0.65f * pulse;

        if (AnyInputPressed()) StartCoroutine(ScrollToMenu());
    }

    // Fits the background to the camera and snaps the camera for the current state.
    void Layout()
    {
        lastW = Screen.width;
        lastH = Screen.height;

        float h = ViewHeight;
        float w = h * menuCamera.aspect;
        Vector3 camPos = menuCamera.transform.position;

        // Stretch: width = view width, height = 2x view height.
        Vector2 spriteSize = background.sprite.bounds.size; // size at scale 1
        background.transform.localScale = new Vector3(w / spriteSize.x, 2f * h / spriteSize.y, 1f);

        // Top edge of the image sits on the top edge of the title view.
        float centerY = titleY - h * 0.5f;
        Vector3 p = background.transform.position;
        background.transform.position = new Vector3(camPos.x, centerY, p.z);

        // Correct for sprites whose pivot isn't centered.
        Vector3 c = background.bounds.center;
        background.transform.position += new Vector3(camPos.x - c.x, centerY - c.y, 0f);

        if (state == State.Title) SetCameraY(titleY);
        else if (state == State.Menu) SetCameraY(MenuY);
    }

    void SetCameraY(float y)
    {
        Vector3 p = menuCamera.transform.position;
        menuCamera.transform.position = new Vector3(p.x, y, p.z);
    }

    IEnumerator ScrollToMenu()
    {
        state = State.Scrolling;

        float startAlpha = pressAnyKeyGroup.alpha;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.01f, scrollDuration);
            float k = scrollCurve.Evaluate(Mathf.Clamp01(t));
            SetCameraY(Mathf.Lerp(titleY, MenuY, k));
            pressAnyKeyGroup.alpha = Mathf.Lerp(startAlpha, 0f, Mathf.Clamp01(t * 3f));
            yield return null;
        }
        SetCameraY(MenuY);
        pressAnyKeyGroup.alpha = 0f;

        // Scroll has landed on the bottom half: fade the buttons in one after another.
        int n = buttonGroups.Length;
        float dur = Mathf.Max(0.01f, fadeDuration);
        float total = dur + buttonStagger * Mathf.Max(0, n - 1);
        float f = 0f;
        while (f < total)
        {
            f += Time.unscaledDeltaTime;
            for (int i = 0; i < n; i++)
                buttonGroups[i].alpha = Mathf.Clamp01((f - i * buttonStagger) / dur);
            yield return null;
        }
        for (int i = 0; i < n; i++) buttonGroups[i].alpha = 1f;

        menuGroup.interactable = true;
        menuGroup.blocksRaycasts = true;
        state = State.Menu;

        if (EventSystem.current != null && firstSelected != null)
            EventSystem.current.SetSelectedGameObject(firstSelected.gameObject);
    }

    static bool AnyInputPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) return true;
        if (Mouse.current != null &&
            (Mouse.current.leftButton.wasPressedThisFrame ||
             Mouse.current.rightButton.wasPressedThisFrame ||
             Mouse.current.middleButton.wasPressedThisFrame)) return true;
        if (Gamepad.current != null &&
            (Gamepad.current.buttonSouth.wasPressedThisFrame ||
             Gamepad.current.startButton.wasPressedThisFrame)) return true;
        return false;
#else
        return Input.anyKeyDown;
#endif
    }

    // ---- Button handlers (wired up by the scene builder) ----

    public void OnNewGame()
    {
        if (string.IsNullOrEmpty(gameSceneName))
        {
            Debug.LogWarning("StartMenu: gameSceneName is empty.");
            return;
        }
        if (!Application.CanStreamedLevelBeLoaded(gameSceneName))
        {
            Debug.LogWarning($"StartMenu: scene '{gameSceneName}' isn't in Build Settings (or the name is wrong).");
            return;
        }
        SceneManager.LoadScene(gameSceneName);
    }

    public void OnOptions()   { Debug.Log("StartMenu: Options");    onOptions?.Invoke(); }
    public void OnCoolStuff() { Debug.Log("StartMenu: Cool Stuff"); onCoolStuff?.Invoke(); }
    public void OnCredits()   { Debug.Log("StartMenu: Credits");    onCredits?.Invoke(); }
}
