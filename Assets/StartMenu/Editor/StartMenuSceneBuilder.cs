#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

/// <summary>
/// Editor tool: select your background image in the Project window, then run
/// Tools > Start Menu > Build Start Menu Scene.
/// Creates Assets/StartMenu/Scenes/StartMenu.unity fully wired up.
/// PUT THIS FILE IN A FOLDER NAMED "Editor" (e.g. Assets/StartMenu/Editor).
/// </summary>
public static class StartMenuSceneBuilder
{
    const string SceneFolder = "Assets/StartMenu/Scenes";
    const string ScenePath = SceneFolder + "/StartMenu.unity";

    [MenuItem("Tools/Start Menu/Build Start Menu Scene")]
    public static void Build()
    {
        var tex = Selection.activeObject as Texture2D;
        if (tex == null)
        {
            EditorUtility.DisplayDialog("Start Menu",
                "Select your background image in the Project window first, then run this again.", "OK");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        // Make sure the image imports as a full-res Sprite (default max size would shrink 2500px).
        string texPath = AssetDatabase.GetAssetPath(tex);
        var importer = AssetImporter.GetAtPath(texPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 4096;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(texPath);
        if (sprite == null)
        {
            EditorUtility.DisplayDialog("Start Menu", "Couldn't load the selected image as a Sprite.", "OK");
            return;
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Camera
        var camGO = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        camGO.transform.position = new Vector3(0f, 0f, -10f);
        camGO.AddComponent<AudioListener>();

        // Background
        var bgGO = new GameObject("Background");
        var sr = bgGO.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = -10;

        // Canvas
        var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // "Press any key" prompt
        var promptGO = new GameObject("PressAnyKey", typeof(RectTransform), typeof(CanvasGroup));
        promptGO.transform.SetParent(canvasGO.transform, false);
        var promptRect = promptGO.GetComponent<RectTransform>();
        promptRect.anchorMin = promptRect.anchorMax = new Vector2(0.5f, 0.12f);
        promptRect.pivot = new Vector2(0.5f, 0.5f);
        promptRect.sizeDelta = new Vector2(800f, 80f);
        promptRect.anchoredPosition = Vector2.zero;
        var promptText = MakeText(promptGO.transform, "Label", "PRESS ANY KEY", 44);
        Stretch(promptText.rectTransform);

        // Menu (4 vertically stacked buttons)
        var menuGO = new GameObject("MenuButtons", typeof(RectTransform), typeof(CanvasGroup), typeof(VerticalLayoutGroup));
        menuGO.transform.SetParent(canvasGO.transform, false);
        var menuRect = menuGO.GetComponent<RectTransform>();
        menuRect.anchorMin = menuRect.anchorMax = new Vector2(0.5f, 0.5f);
        menuRect.pivot = new Vector2(0.5f, 0.5f);
        menuRect.sizeDelta = new Vector2(420f, 4 * 72f + 3 * 20f);
        menuRect.anchoredPosition = Vector2.zero;
        var vlg = menuGO.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 20f;
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = true;

        var newGame = MakeButton(menuGO.transform, "New Game");
        var options = MakeButton(menuGO.transform, "Options");
        var coolStuff = MakeButton(menuGO.transform, "Cool Stuff");
        var credits = MakeButton(menuGO.transform, "Credits");

        // Event system (matches whichever input backend the project uses)
        var esGO = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        esGO.AddComponent<InputSystemUIInputModule>();
#else
        esGO.AddComponent<StandaloneInputModule>();
#endif

        // Controller
        var ctrlGO = new GameObject("StartMenuController");
        var ctrl = ctrlGO.AddComponent<StartMenuController>();
        var so = new SerializedObject(ctrl);
        so.FindProperty("menuCamera").objectReferenceValue = cam;
        so.FindProperty("background").objectReferenceValue = sr;
        so.FindProperty("pressAnyKeyGroup").objectReferenceValue = promptGO.GetComponent<CanvasGroup>();
        so.FindProperty("menuGroup").objectReferenceValue = menuGO.GetComponent<CanvasGroup>();
        so.FindProperty("firstSelected").objectReferenceValue = newGame;
        so.ApplyModifiedPropertiesWithoutUndo();

        UnityEventTools.AddPersistentListener(newGame.onClick, ctrl.OnNewGame);
        UnityEventTools.AddPersistentListener(options.onClick, ctrl.OnOptions);
        UnityEventTools.AddPersistentListener(coolStuff.onClick, ctrl.OnCoolStuff);
        UnityEventTools.AddPersistentListener(credits.onClick, ctrl.OnCredits);

        Directory.CreateDirectory(SceneFolder);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.Refresh();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        Debug.Log("Start menu scene created at " + ScenePath);
    }

    static Button MakeButton(Transform parent, string label)
    {
        var go = new GameObject(label + " Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        go.GetComponent<LayoutElement>().minHeight = 72f;

        var img = go.GetComponent<Image>();
        img.color = Color.white; // the button's color block does the tinting

        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        var colors = btn.colors;
        colors.normalColor = new Color(0f, 0f, 0f, 0.55f);
        colors.highlightedColor = new Color(0.25f, 0.25f, 0.25f, 0.85f);
        colors.selectedColor = new Color(0.25f, 0.25f, 0.25f, 0.85f);
        colors.pressedColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        colors.fadeDuration = 0.08f;
        btn.colors = colors;

        var text = MakeText(go.transform, "Label", label, 34);
        Stretch(text.rectTransform);
        return btn;
    }

    static Text MakeText(Transform parent, string name, string content, int size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<Text>();
        t.text = content;
        t.font = GetFont();
        t.fontSize = size;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;
        t.raycastTarget = false;
        return t;
    }

    static Font GetFont()
    {
#if UNITY_2022_2_OR_NEWER
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
        return Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
    }

    static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
    }
}
#endif
