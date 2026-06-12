using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class ConfigSceneController : MonoBehaviour
{
    private const string ConfigSceneName = "ConfigScene";
    private const string MenuResourcePath = "Menu";
    private const string RestartStageButtonName = "RestartStage_Button";
    private const string StageSelectButtonName = "StageSelect_Button";
    private const string ReturnTitleButtonName = "ReturnTitle_Button";
    private const float PanelWidth = 720f;
    private const float RowHeight = 72f;
    private const float MenuButtonSpacing = 160f;

    private static bool s_SubscribedToSceneLoaded;

    private CanvasGroup m_ConfigCanvasGroup;
    private GameObject m_MenuInstance;
    private GameObject m_MenuPrefab;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (!s_SubscribedToSceneLoaded)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            s_SubscribedToSceneLoaded = true;
        }

        TryCreateForActiveScene();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == ConfigSceneName)
        {
            TryCreateForActiveScene();
        }
    }

    private static void TryCreateForActiveScene()
    {
        if (SceneManager.GetActiveScene().name != ConfigSceneName)
        {
            return;
        }

        if (FindFirstObjectByType<ConfigSceneController>() != null)
        {
            return;
        }

        GameObject root = new GameObject(nameof(ConfigSceneController));
        root.AddComponent<ConfigSceneController>();
    }

    private void Awake()
    {
        RemoveCopiedMenuSceneObjects();
        EnsureCamera();
        EnsureEventSystem();
        BuildConfigUi();
        GameAudioSettings.ApplyAllAudioSources();
    }

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            return;
        }

        ToggleMenu();
    }

    private void ToggleMenu()
    {
        if (m_MenuInstance != null && m_MenuInstance.activeSelf)
        {
            m_MenuInstance.SetActive(false);
            SetConfigUiInteraction(true);
            return;
        }

        ShowMenu();
    }

    private void ShowMenu()
    {
        StageMenuState.SetRestartStage(SCENETYPE.CONFIG);

        if (m_MenuInstance == null)
        {
            if (m_MenuPrefab == null)
            {
                m_MenuPrefab = Resources.Load<GameObject>(MenuResourcePath);
            }

            if (m_MenuPrefab == null)
            {
                Debug.LogError("Config menu prefab was not found in Resources/Menu.");
                return;
            }

            m_MenuInstance = Instantiate(m_MenuPrefab);
            ConfigureMenuButtons(m_MenuInstance);
        }

        m_MenuInstance.SetActive(true);
        SetConfigUiInteraction(false);
    }

    private static void ConfigureMenuButtons(GameObject menu)
    {
        if (menu == null)
        {
            return;
        }

        GameObject restartButton = FindChild(menu.transform, RestartStageButtonName);
        GameObject stageSelectButton = FindChild(menu.transform, StageSelectButtonName);
        GameObject returnTitleButton = FindChild(menu.transform, ReturnTitleButtonName);

        SetButtonVisible(restartButton, false);
        SetButtonVisible(stageSelectButton, false);
        SetButtonVisible(returnTitleButton, true);
        LayoutVisibleButtons(restartButton, stageSelectButton, returnTitleButton);
    }

    private void SetConfigUiInteraction(bool enabled)
    {
        if (m_ConfigCanvasGroup == null)
        {
            return;
        }

        m_ConfigCanvasGroup.interactable = enabled;
        m_ConfigCanvasGroup.blocksRaycasts = enabled;
    }

    private static void RemoveCopiedMenuSceneObjects()
    {
        StageMenuCloser[] closers = FindObjectsByType<StageMenuCloser>(FindObjectsSortMode.None);
        foreach (StageMenuCloser closer in closers)
        {
            Destroy(closer);
        }

        GameObject menuCanvas = GameObject.Find("Canvas");
        if (menuCanvas != null && FindChild(menuCanvas.transform, "ReturnTitle_Button") != null)
        {
            menuCanvas.SetActive(false);
            Destroy(menuCanvas);
        }
    }

    private static void EnsureCamera()
    {
        if (Camera.main != null)
        {
            return;
        }

        GameObject cameraObject = new GameObject("Main Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        cameraObject.AddComponent<AudioListener>();
        cameraObject.tag = "MainCamera";
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.055f, 0.065f, 0.075f);
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    private void BuildConfigUi()
    {
        GameObject canvasObject = new GameObject("ConfigCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        m_ConfigCanvasGroup = canvasObject.GetComponent<CanvasGroup>();

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        CreateImage(canvasRect, "Background", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.07f, 0.08f, 0.09f, 1f));

        RectTransform panel = CreatePanel(canvasRect);
        CreateText(panel, "Title", "VOLUME", new Vector2(0f, 210f), new Vector2(PanelWidth, 96f), 58, TextAnchor.MiddleCenter);
        CreateSliderRow(panel, "BGM", new Vector2(0f, 60f), GameAudioSettings.BgmVolume, GameAudioSettings.SetBgmVolume);
        CreateSliderRow(panel, "SE", new Vector2(0f, -60f), GameAudioSettings.SeVolume, GameAudioSettings.SetSeVolume);
    }

    private static RectTransform CreatePanel(RectTransform parent)
    {
        RectTransform panel = CreateImage(parent, "ConfigPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(860f, 560f), new Color(0.13f, 0.15f, 0.17f, 0.94f));
        return panel;
    }

    private static void CreateSliderRow(RectTransform parent, string label, Vector2 position, float value, UnityEngine.Events.UnityAction<float> onChanged)
    {
        RectTransform row = CreateRect(parent, label + "Row", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(PanelWidth, RowHeight));
        CreateText(row, label + "Label", label, new Vector2(-270f, 0f), new Vector2(150f, RowHeight), 34, TextAnchor.MiddleLeft);

        Slider slider = CreateSlider(row, label + "Slider", new Vector2(110f, 0f), new Vector2(460f, 48f));
        slider.SetValueWithoutNotify(Mathf.Clamp01(value));
        slider.onValueChanged.AddListener(onChanged);
    }

    private static Slider CreateSlider(RectTransform parent, string name, Vector2 position, Vector2 size)
    {
        RectTransform root = CreateRect(parent, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);
        Slider slider = root.gameObject.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;

        RectTransform background = CreateImage(root, "Background", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.25f, 0.28f, 0.3f, 1f));
        RectTransform fillArea = CreateRect(root, "Fill Area", Vector2.zero, Vector2.one, new Vector2(-10f, 0f), new Vector2(-20f, 0f));
        RectTransform fill = CreateImage(fillArea, "Fill", Vector2.zero, new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Color(0.82f, 0.63f, 0.23f, 1f));
        RectTransform handleArea = CreateRect(root, "Handle Slide Area", Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-20f, 0f));
        RectTransform handle = CreateImage(handleArea, "Handle", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(42f, 58f), new Color(0.94f, 0.92f, 0.86f, 1f));

        slider.targetGraphic = handle.GetComponent<Image>();
        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.direction = Slider.Direction.LeftToRight;

        background.SetAsFirstSibling();
        return slider;
    }

    private static Text CreateText(RectTransform parent, string name, string text, Vector2 position, Vector2 size, int fontSize, TextAnchor alignment)
    {
        RectTransform rect = CreateRect(parent, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);
        Text textComponent = rect.gameObject.AddComponent<Text>();
        textComponent.text = text;
        textComponent.font = GetDefaultFont();
        textComponent.fontSize = fontSize;
        textComponent.alignment = alignment;
        textComponent.color = new Color(0.94f, 0.92f, 0.86f, 1f);
        return textComponent;
    }

    private static RectTransform CreateImage(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, Color color)
    {
        RectTransform rect = CreateRect(parent, name, anchorMin, anchorMax, position, size);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        return rect;
    }

    private static RectTransform CreateRect(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static Font GetDefaultFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    private static GameObject FindChild(Transform root, string objectName)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == objectName)
        {
            return root.gameObject;
        }

        foreach (Transform child in root)
        {
            GameObject found = FindChild(child, objectName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static void SetButtonVisible(GameObject button, bool visible)
    {
        if (button != null)
        {
            button.SetActive(visible);
        }
    }

    private static void LayoutVisibleButtons(params GameObject[] buttons)
    {
        int visibleCount = 0;
        foreach (GameObject button in buttons)
        {
            if (button != null && button.activeSelf)
            {
                visibleCount++;
            }
        }

        int visibleIndex = 0;
        foreach (GameObject button in buttons)
        {
            if (button == null || !button.activeSelf)
            {
                continue;
            }

            RectTransform rectTransform = button.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                Vector2 anchoredPosition = rectTransform.anchoredPosition;
                anchoredPosition.y = (visibleCount - 1) * MenuButtonSpacing * 0.5f - visibleIndex * MenuButtonSpacing;
                rectTransform.anchoredPosition = anchoredPosition;
            }

            visibleIndex++;
        }
    }
}
