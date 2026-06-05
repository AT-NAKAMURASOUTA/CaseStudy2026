using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[ExecuteAlways]
public sealed class TitleBookMenu : MonoBehaviour
{
    private enum MenuAction
    {
        NewGame,
        DataLoad,
        Config,
        ShutDown
    }

    private sealed class MenuItem
    {
        public MenuAction Action;
        public RectTransform Root;
        public Image ButtonImage;
        public Sprite NormalSprite;
        public Sprite SelectedSprite;
        public bool HasVisual;
    }

    private const string TitleSceneName = "TitleScene";
    private const string RuntimeRootName = "TitleBookMenu_Runtime";
    private const string DeskBackgroundPath = "UI/Title/title_desk_background";
    private const string BookBasePath = "UI/Title/BookParts/title_book_base_user";
    private const string BookCoverPath = "UI/Title/BookParts/title_book_cover_user";
    private const string TitleUiPath = "UI/Title/BookParts/TitleUi/";
    private const float ReferenceBookSize = 700f;

    // InspectorとSceneViewのハンドルから調整
    [SerializeField] private Vector2 m_BookPosition = new Vector2(330f, -115f);
    [SerializeField] private float m_BookSize = 700f;
    // 表紙の回転軸と中心位置は基準サイズで保持し、本のサイズに合わせて拡大縮小
    [SerializeField] private float m_CoverHingeX = -245f;
    [SerializeField] private float m_CoverCenterOffsetFromHinge = 244f;
    [SerializeField] private Vector2 m_TitlePosition = new Vector2(258f, 196f);
    [SerializeField] private Vector2 m_TitleSize = new Vector2(454f, 126f);
    [SerializeField] private Vector2 m_ButtonSize = new Vector2(198f, 80f);
    [SerializeField]
    private Vector2[] m_MenuPositions =
    {
        new Vector2(150f, -6f),
        new Vector2(352f, -6f),
        new Vector2(150f, -106f),
        new Vector2(352f, -106f)
    };

    private static Sprite s_WhiteSprite;

    private readonly MenuItem[] m_MenuItems = new MenuItem[4];
    private RectTransform m_BookRoot;
    private RectTransform m_BookCoverPivot;
    private RectTransform m_CoverUiRoot;
    private CanvasGroup m_CoverUiGroup;
    private Image m_FadeImage;
    private int m_SelectedIndex;
    private bool m_IsTransitioning;

    private void Awake()
    {
        if (Application.isPlaying && gameObject.name == RuntimeRootName && transform.parent != null)
        {
            ClearGeneratedChildren();
            Build(transform.parent);
        }
    }

    private void OnEnable()
    {
        if (!Application.isPlaying && gameObject.name == RuntimeRootName && gameObject.scene.IsValid() && gameObject.scene.name == TitleSceneName && transform.parent != null)
        {
            RebuildEditorPreview();
        }
    }

    private void OnValidate()
    {
        m_BookSize = Mathf.Max(1f, m_BookSize);
        EnsureMenuPositionCount();
        if (!Application.isPlaying && isActiveAndEnabled && gameObject.name == RuntimeRootName && gameObject.scene.IsValid() && gameObject.scene.name == TitleSceneName && transform.parent != null)
        {
            RebuildEditorPreview();
        }
    }

    private void EnsureMenuPositionCount()
    {
        if (m_MenuPositions != null && m_MenuPositions.Length == m_MenuItems.Length)
        {
            return;
        }

        m_MenuPositions = new[]
        {
            new Vector2(150f, -6f),
            new Vector2(352f, -6f),
            new Vector2(150f, -106f),
            new Vector2(352f, -106f)
        };
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void SubscribeSceneLoaded()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateForInitialScene()
    {
        TryCreateForScene(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryCreateForScene(scene);
    }

    private static void TryCreateForScene(Scene scene)
    {
        if (!scene.IsValid() || scene.name != TitleSceneName || GameObject.Find(RuntimeRootName) != null)
        {
            return;
        }

        // タイトルシーン側が空のCanvasなら実行時にメニューを作成
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        }
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        if (FindFirstObjectByType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        GameObject rootObject = new GameObject(RuntimeRootName, typeof(RectTransform), typeof(TitleBookMenu));
        rootObject.transform.SetParent(canvas.transform, false);
        rootObject.GetComponent<TitleBookMenu>().Build(canvas.transform);
    }

    private void Build(Transform canvasTransform)
    {
        RectTransform root = GetComponent<RectTransform>();
        Stretch(root);
        HideExistingTitleButtons(canvasTransform);

        CreateBackground(root);
        CreateBook(root);
        CreateFade(root);
        SelectItem(0);
    }

    private void RebuildEditorPreview()
    {
        ClearGeneratedChildren();
        Build(transform.parent);
    }

#if UNITY_EDITOR
    public void RefreshEditorPreview()
    {
        if (!Application.isPlaying && gameObject.name == RuntimeRootName && gameObject.scene.IsValid() && gameObject.scene.name == TitleSceneName && transform.parent != null)
        {
            RebuildEditorPreview();
        }
    }
#endif

    private void ClearGeneratedChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(child.gameObject);
            }
            else
#endif
            {
                Destroy(child.gameObject);
            }
        }
    }

    private void Update()
    {
        if (!Application.isPlaying || m_IsTransitioning)
        {
            return;
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            ActivateSelected();
        }
        else if (Keyboard.current == null)
        {
            return;
        }
        else if (Keyboard.current.sKey.wasPressedThisFrame)
        {
            SelectAndActivate(0);
        }
        else if (Keyboard.current.lKey.wasPressedThisFrame)
        {
            SelectAndActivate(1);
        }
        else if (Keyboard.current.cKey.wasPressedThisFrame)
        {
            SelectAndActivate(2);
        }
        else if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            SelectAndActivate(3);
        }
        else if (Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.downArrowKey.wasPressedThisFrame)
        {
            SelectItem((m_SelectedIndex + 2) % m_MenuItems.Length);
        }
        else if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
        {
            SelectItem((m_SelectedIndex + m_MenuItems.Length - 1) % m_MenuItems.Length);
        }
        else if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
        {
            SelectItem((m_SelectedIndex + 1) % m_MenuItems.Length);
        }
        else if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            ActivateSelected();
        }
    }

    private void HideExistingTitleButtons(Transform canvasTransform)
    {
        if (canvasTransform == null)
        {
            return;
        }

        for (int i = canvasTransform.childCount - 1; i >= 0; i--)
        {
            Transform child = canvasTransform.GetChild(i);
            if (child == transform)
            {
                continue;
            }

            if (child.name.Contains("Button") || child.name == "BackGround")
            {
                child.gameObject.SetActive(false);
            }
        }
    }

    private void CreateBackground(RectTransform root)
    {
        Image background = CreateSpriteImage("DeskBackground", root, DeskBackgroundPath, Vector2.zero, new Vector2(1920f, 1080f));
        if (background == null)
        {
            Debug.LogError($"Title background was not found in Resources: {DeskBackgroundPath}");
            CreateImage("MissingTitleBackground", root, new Color(0.08f, 0.04f, 0.03f), Vector2.zero, new Vector2(1920f, 1080f));
        }
    }

    private void CreateBook(RectTransform root)
    {
        float bookScale = m_BookSize / ReferenceBookSize;
        float scaledCoverHingeX = m_CoverHingeX * bookScale;
        float scaledCoverCenterOffsetFromHinge = m_CoverCenterOffsetFromHinge * bookScale;

        m_BookRoot = CreatePanel("TitleBookParts", root, m_BookPosition, new Vector2(m_BookSize, m_BookSize));

        Image baseImage = CreateSpriteImage("BookBase", m_BookRoot, BookBasePath, Vector2.zero, new Vector2(m_BookSize, m_BookSize));
        if (baseImage == null)
        {
            Debug.LogError($"Title book base was not found in Resources: {BookBasePath}");
        }

        // 土台の左側を軸に表紙を回転させる
        m_BookCoverPivot = CreatePanel("BookCoverPivot", m_BookRoot, new Vector2(scaledCoverHingeX, 0f), Vector2.one);
        Image coverImage = CreateSpriteImage("BookCover", m_BookCoverPivot, BookCoverPath, new Vector2(scaledCoverCenterOffsetFromHinge, 0f), new Vector2(m_BookSize, m_BookSize));
        if (coverImage == null)
        {
            Debug.LogError($"Title book cover was not found in Resources: {BookCoverPath}");
        }

        m_CoverUiRoot = CreatePanel("CoverUiRoot", m_BookCoverPivot, Vector2.zero, Vector2.one);
        m_CoverUiGroup = m_CoverUiRoot.gameObject.AddComponent<CanvasGroup>();
        m_CoverUiGroup.alpha = 1f;
        m_CoverUiGroup.blocksRaycasts = true;
        m_CoverUiGroup.interactable = true;

        CreateCoverTitleImage(bookScale);
        CreateMenuItem(0, MenuAction.NewGame, GetMenuPosition(0) * bookScale, bookScale);
        CreateMenuItem(1, MenuAction.DataLoad, GetMenuPosition(1) * bookScale, bookScale);
        CreateMenuItem(2, MenuAction.Config, GetMenuPosition(2) * bookScale, bookScale);
        CreateMenuItem(3, MenuAction.ShutDown, GetMenuPosition(3) * bookScale, bookScale);
    }

    private void CreateCoverTitleImage(float scale)
    {
        Image titleImage = CreateSpriteImage("TitleImage", m_CoverUiRoot, $"{TitleUiPath}title_key_world", m_TitlePosition * scale, m_TitleSize * scale);
        if (titleImage == null)
        {
            Debug.LogError($"Title UI image was not found in Resources: {TitleUiPath}title_key_world");
        }
    }

    private Vector2 GetMenuPosition(int index)
    {
        EnsureMenuPositionCount();
        return m_MenuPositions[Mathf.Clamp(index, 0, m_MenuPositions.Length - 1)];
    }

    private void CreateMenuItem(int index, MenuAction action, Vector2 position, float scale)
    {
        Vector2 size = m_ButtonSize * scale;
        RectTransform item = CreatePanel($"MenuItem_{action}", m_CoverUiRoot, position, size);
        string normalPath = GetButtonSpritePath(action, false);
        string selectedPath = GetButtonSpritePath(action, true);
        Image buttonImage = CreateSpriteImage("ButtonImage", item, normalPath, Vector2.zero, size);
        Sprite normalSprite = buttonImage != null ? buttonImage.sprite : null;
        Sprite selectedSprite = LoadResourceSprite(selectedPath);

        if (buttonImage == null)
        {
            Debug.LogError($"Title button image was not found in Resources: {normalPath}");
            buttonImage = item.gameObject.AddComponent<Image>();
            buttonImage.sprite = WhiteSprite;
            buttonImage.color = new Color(1f, 1f, 1f, 0f);
        }
        buttonImage.raycastTarget = true;

        Button button = item.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.targetGraphic = buttonImage;

        // マウスとキーボードの選択状態を同じ番号で扱う
        int capturedIndex = index;
        button.onClick.AddListener(() =>
        {
            SelectItem(capturedIndex);
            ActivateSelected();
        });

        AddPointerEnterTrigger(item.gameObject, capturedIndex);
        AddPointerEnterTrigger(buttonImage.gameObject, capturedIndex);

        m_MenuItems[index] = new MenuItem
        {
            Action = action,
            Root = item,
            ButtonImage = buttonImage,
            NormalSprite = normalSprite,
            SelectedSprite = selectedSprite,
            HasVisual = true
        };
    }

    private void AddPointerEnterTrigger(GameObject target, int index)
    {
        EventTrigger trigger = target.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = target.AddComponent<EventTrigger>();
        }

        EventTrigger.Entry pointerEnter = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerEnter
        };
        pointerEnter.callback.AddListener(_ =>
        {
            if (!m_IsTransitioning)
            {
                SelectItem(index);
            }
        });
        trigger.triggers.Add(pointerEnter);
    }

    private void CreateFade(RectTransform root)
    {
        m_FadeImage = CreateImage("SceneFade", root, new Color(0f, 0f, 0f, 0f), Vector2.zero, new Vector2(1920f, 1080f));
        m_FadeImage.raycastTarget = false;
        m_FadeImage.transform.SetAsLastSibling();
    }

    private void SelectAndActivate(int index)
    {
        SelectItem(index);
        ActivateSelected();
    }

    private void SelectItem(int index)
    {
        m_SelectedIndex = Mathf.Clamp(index, 0, m_MenuItems.Length - 1);

        for (int i = 0; i < m_MenuItems.Length; i++)
        {
            MenuItem item = m_MenuItems[i];
            bool selected = i == m_SelectedIndex;
            item.Root.localScale = selected ? Vector3.one * 1.04f : Vector3.one;
            if (item.HasVisual)
            {
                if (item.ButtonImage != null)
                {
                    item.ButtonImage.sprite = selected && item.SelectedSprite != null ? item.SelectedSprite : item.NormalSprite;
                }
            }
        }
    }

    private void ActivateSelected()
    {
        if (!m_IsTransitioning)
        {
            StartCoroutine(ActivateRoutine(m_MenuItems[m_SelectedIndex].Action));
        }
    }

    private IEnumerator ActivateRoutine(MenuAction action)
    {
        m_IsTransitioning = true;

        yield return PlayBookOpen();
        yield return PlayEnterBook();

        switch (action)
        {
            case MenuAction.NewGame:
            case MenuAction.DataLoad:
                SceneTransitionManager.GetInstance().SceneTransition(SCENETYPE.STAGESELECT);
                break;
            case MenuAction.Config:
                SceneTransitionManager.GetInstance().SceneTransition(SCENETYPE.MENU);
                break;
            case MenuAction.ShutDown:
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
                break;
        }
    }

    private IEnumerator PlayBookOpen()
    {
        const float duration = 1.25f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float fadeT = EaseInOutCubic(elapsed / duration);
            if (m_BookCoverPivot != null)
            {
                m_BookCoverPivot.localRotation = Quaternion.Euler(0f, -180f * fadeT, 0f);
            }
            // 表紙が裏返るときタイトルやボタンを表示しない
            SetCoverUiVisibility(Mathf.Clamp01((0.50f - fadeT) / 0.08f));

            m_FadeImage.color = new Color(0.02f, 0.012f, 0.006f, Mathf.Lerp(0f, 0.34f, Mathf.Max(0f, (fadeT - 0.78f) / 0.22f)));
            yield return null;
        }

        if (m_BookCoverPivot != null)
        {
            m_BookCoverPivot.localRotation = Quaternion.Euler(0f, -180f, 0f);
        }
        SetCoverUiVisibility(0f);

        yield return new WaitForSeconds(0.1f);
    }

    private void SetCoverUiVisibility(float alpha)
    {
        if (m_CoverUiGroup == null)
        {
            return;
        }

        alpha = Mathf.Clamp01(alpha);
        m_CoverUiGroup.alpha = alpha;
        m_CoverUiGroup.blocksRaycasts = alpha > 0.01f && !m_IsTransitioning;
        m_CoverUiGroup.interactable = alpha > 0.01f && !m_IsTransitioning;
    }

    private IEnumerator PlayEnterBook()
    {
        const float duration = 0.8f;
        Vector2 startPosition = m_BookRoot != null ? m_BookRoot.anchoredPosition : Vector2.zero;
        Vector3 startScale = m_BookRoot != null ? m_BookRoot.localScale : Vector3.one;
        Vector2 targetPosition = new Vector2(70f, -35f);
        Vector3 targetScale = Vector3.one * 2.9f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            // 最初はゆっくり開き加速させる(緩急をつける)
            float easedEnter = EaseInCubic(progress);

            if (m_BookRoot != null)
            {
                float settle = Mathf.Sin(progress * Mathf.PI) * 0.035f;
                m_BookRoot.anchoredPosition = Vector2.LerpUnclamped(startPosition, targetPosition, easedEnter);
                m_BookRoot.localScale = Vector3.LerpUnclamped(startScale, targetScale, easedEnter) + Vector3.one * settle;
            }

            if (m_FadeImage != null)
            {
                float alpha = Mathf.Lerp(0.18f, 1f, Mathf.Clamp01((easedEnter - 0.12f) / 0.88f));
                m_FadeImage.color = new Color(0.02f, 0.012f, 0.006f, alpha);
            }

            yield return null;
        }

        if (m_FadeImage != null)
        {
            m_FadeImage.color = new Color(0.02f, 0.012f, 0.006f, 1f);
        }
    }

    private static Image CreateSpriteImage(string name, RectTransform parent, string resourcePath, Vector2 position, Vector2 size)
    {
        Sprite sprite = LoadResourceSprite(resourcePath);
        if (sprite == null)
        {
            return null;
        }

        Image image = CreateImage(name, parent, Color.white, position, size);
        image.sprite = sprite;
        image.preserveAspect = true;
        return image;
    }

    private static Sprite LoadResourceSprite(string resourcePath)
    {
        Texture2D texture = Resources.Load<Texture2D>(resourcePath);
        if (texture == null)
        {
            return null;
        }

        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
    }

    private static string GetButtonSpritePath(MenuAction action, bool selected)
    {
        string state = selected ? "selected" : "normal";
        switch (action)
        {
            case MenuAction.NewGame:
                return $"{TitleUiPath}button_start_{state}";
            case MenuAction.DataLoad:
                return $"{TitleUiPath}button_load_{state}";
            case MenuAction.Config:
                return $"{TitleUiPath}button_config_{state}";
            case MenuAction.ShutDown:
                return $"{TitleUiPath}button_exit_{state}";
            default:
                return $"{TitleUiPath}button_start_{state}";
        }
    }

    private static RectTransform CreatePanel(string name, RectTransform parent, Vector2 position, Vector2 size)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static Image CreateImage(string name, RectTransform parent, Color color, Vector2 position, Vector2 size)
    {
        RectTransform rect = CreatePanel(name, parent, position, size);
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = WhiteSprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    private static float EaseOutCubic(float value)
    {
        value = Mathf.Clamp01(value);
        return 1f - Mathf.Pow(1f - value, 3f);
    }

    private static float EaseInCubic(float value)
    {
        value = Mathf.Clamp01(value);
        return value * value * value;
    }

    private static float EaseInOutCubic(float value)
    {
        value = Mathf.Clamp01(value);
        return value < 0.55f
            ? 0.5f * Mathf.Pow(value / 0.55f, 4f)
            : 0.5f + 0.5f * (1f - Mathf.Pow(1f - ((value - 0.55f) / 0.45f), 3f));
    }

    private static Sprite WhiteSprite
    {
        get
        {
            if (s_WhiteSprite == null)
            {
                s_WhiteSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 100f);
            }

            return s_WhiteSprite;
        }
    }

}
