#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class StageBuilderWindow : EditorWindow
{
    private const string GroundParentName = "GroundParent";
    private const string GroundSpritePath = "Assets/PrototypeAssets/Common/PrototypeSquare.png";
    private const string PrefabFolderPath = "Assets/StageObject/Prefab";

    private static readonly PaletteItem[] PaletteItems =
    {
        new PaletteItem("Player", "Player", BuilderTab.PlayerGoal),
        new PaletteItem("Goal", "Goal", BuilderTab.PlayerGoal),
        new PaletteItem("Saw", "SawBlade", BuilderTab.Gimmick),
        new PaletteItem("Low Gravity", "LowGravity", BuilderTab.Gimmick),
        new PaletteItem("Accel", "AccelerationArea", BuilderTab.Gimmick),
        new PaletteItem("Switch", "SwitchObject", BuilderTab.Gimmick),
        new PaletteItem("Door", "DoorObject", BuilderTab.Gimmick),
        new PaletteItem("Switch Door", "SwitchAndDoorObject", BuilderTab.Gimmick),
        new PaletteItem("Open Door", "OpenDoor", BuilderTab.Gimmick),
        new PaletteItem("Open Switch", "OpenDoorAndSwitchObject", BuilderTab.Gimmick),
        new PaletteItem("Lever", "HangLever", BuilderTab.Gimmick)
    };

    private enum BuilderTab
    {
        Field,
        Gimmick,
        PlayerGoal
    }

    private enum ToolMode
    {
        Ground,
        TriangleGround,
        CameraBounds,
        Prefab,
        Erase,
        Off
    }

    private enum AttachSide
    {
        Floor,
        LeftWall,
        RightWall,
        Ceiling
    }

    private readonly Dictionary<string, GameObject> m_Prefabs = new Dictionary<string, GameObject>();
    private readonly Dictionary<string, Vector2> m_PrefabSizeOverrides = new Dictionary<string, Vector2>();
    private readonly Dictionary<string, Vector2> m_PairTargetSizeOverrides = new Dictionary<string, Vector2>();

    private ToolMode m_Mode = ToolMode.Ground;
    private string m_SelectedPrefabName = "Player";
    private Sprite m_GroundSprite;
    private Vector2 m_GroundStart;
    private Vector2 m_GroundEnd;
    private Vector2 m_AreaStart;
    private Vector2 m_AreaEnd;
    private Vector2 m_CameraBoundsStart;
    private Vector2 m_CameraBoundsEnd;
    private Vector2 m_PairTargetStart;
    private Vector2 m_PairTargetEnd;
    private bool m_IsDraggingGround;
    private bool m_IsDraggingArea;
    private bool m_IsDraggingCameraBounds;
    private bool m_IsDraggingPairTarget;
    private readonly List<Vector2> m_TrianglePoints = new List<Vector2>(3);
    private float m_GridSize = 1f;
    private Vector2 m_DefaultGroundSize = Vector2.one;
    private Vector2 m_ScrollPosition;
    private BuilderTab m_Tab = BuilderTab.Field;
    private GameObject m_PendingPairObject;
    private string m_PendingPairPrefabName;
    private Transform m_PendingPairDoor;
    private Rect m_PendingPairTargetRect;
    private bool m_IsWaitingForPairMoveEnd;
    private GameObject m_PendingOpenDoorObject;
    private Rect m_PendingOpenDoorRect;
    private GameObject m_CursorPreviewObject;
    private string m_CursorPreviewKey;
    private Texture2D m_SawBladePreviewTexture;

    private GUIStyle m_HeaderStyle;
    private GUIStyle m_CardStyle;
    private GUIStyle m_SelectedButtonStyle;
    private GUIStyle m_ButtonStyle;
    private GUIStyle m_PreviewBoxStyle;
    private GUIStyle m_TabStyle;
    private GUIStyle m_FieldLabelStyle;

    private readonly GUIContent[] m_TabContents =
    {
        new GUIContent("Field"),
        new GUIContent("Gimmick"),
        new GUIContent("Player/Goal")
    };

    [MenuItem("Tools/Stage Builder")]
    public static void Open()
    {
        StageBuilderWindow window = GetWindow<StageBuilderWindow>("Stage Builder");
        window.minSize = new Vector2(360f, 520f);
    }

    private void OnEnable()
    {
        LoadAssets();
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        DestroyCursorPreview();
        CancelPendingPairPlacement();
        CancelTriangleGround();
        m_IsDraggingCameraBounds = false;

        if (m_SawBladePreviewTexture != null)
        {
            DestroyImmediate(m_SawBladePreviewTexture);
            m_SawBladePreviewTexture = null;
        }
    }

    private void BuildStyles()
    {
        if (m_HeaderStyle != null)
        {
            return;
        }

        m_HeaderStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(10, 10, 8, 8)
        };

        m_CardStyle = new GUIStyle(EditorStyles.helpBox)
        {
            padding = new RectOffset(12, 12, 10, 12),
            margin = new RectOffset(8, 8, 6, 8)
        };

        m_ButtonStyle = new GUIStyle(GUI.skin.button)
        {
            fixedHeight = 34,
            alignment = TextAnchor.MiddleLeft,
            imagePosition = ImagePosition.ImageLeft,
            padding = new RectOffset(8, 6, 4, 4),
            wordWrap = false,
            clipping = TextClipping.Clip
        };

        m_SelectedButtonStyle = new GUIStyle(m_ButtonStyle)
        {
            fontStyle = FontStyle.Bold
        };

        m_PreviewBoxStyle = new GUIStyle(EditorStyles.helpBox)
        {
            alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset(6, 6, 6, 6)
        };

        m_TabStyle = new GUIStyle(GUI.skin.button)
        {
            fixedHeight = 34,
            alignment = TextAnchor.MiddleCenter,
            clipping = TextClipping.Clip,
            fontStyle = FontStyle.Bold,
            padding = new RectOffset(4, 4, 4, 4)
        };

        m_FieldLabelStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleLeft,
            fixedWidth = 54f
        };
    }

    private void LoadAssets()
    {
        m_GroundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(GroundSpritePath);
        m_Prefabs.Clear();

        foreach (PaletteItem item in PaletteItems)
        {
            string path = $"{PrefabFolderPath}/{item.PrefabName}.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                m_Prefabs[item.PrefabName] = prefab;
            }
        }
    }

    private void OnGUI()
    {
        BuildStyles();

        DrawHeader();
        m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);

        DrawTabBar();
        DrawEditActions();
        DrawActiveTab();

        EditorGUILayout.EndScrollView();
    }

    private void DrawHeader()
    {
        Rect rect = GUILayoutUtility.GetRect(0f, 44f, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(rect, new Color(0.13f, 0.18f, 0.22f));
        GUI.Label(rect, "Stage Builder", m_HeaderStyle);

        Rect statusRect = new Rect(rect.xMax - 150f, rect.y + 12f, 140f, 20f);
        GUI.Label(statusRect, GetModeStatus(), EditorStyles.whiteMiniLabel);
    }

    private void DrawTabBar()
    {
        EditorGUILayout.BeginVertical(m_CardStyle);
        Rect rect = GUILayoutUtility.GetRect(0f, 34f, GUILayout.ExpandWidth(true));
        const float gap = 6f;
        float tabWidth = (rect.width - gap * (m_TabContents.Length - 1)) / m_TabContents.Length;
        for (int i = 0; i < m_TabContents.Length; i++)
        {
            Rect tabRect = new Rect(rect.x + i * (tabWidth + gap), rect.y, tabWidth, rect.height);
            bool selected = i == (int)m_Tab;
            Color previousColor = GUI.backgroundColor;
            GUI.backgroundColor = selected ? new Color(0.48f, 0.68f, 0.82f) : new Color(0.55f, 0.55f, 0.55f);
            if (GUI.Button(tabRect, m_TabContents[i], m_TabStyle) && !selected)
            {
                SelectTab((BuilderTab)i);
            }
            GUI.backgroundColor = previousColor;
        }
        EditorGUILayout.EndVertical();
    }

    private void SelectTab(BuilderTab tab)
    {
        CancelPendingPairPlacement();
        CancelTriangleGround();
        m_IsDraggingCameraBounds = false;
        m_Tab = tab;
        if (m_Tab == BuilderTab.Field)
        {
            m_Mode = ToolMode.Ground;
        }
        else if (m_Tab == BuilderTab.Gimmick || m_Tab == BuilderTab.PlayerGoal)
        {
            m_Mode = ToolMode.Prefab;
            SelectFirstPrefabInTab(m_Tab);
        }
    }

    private void DrawEditActions()
    {
        EditorGUILayout.BeginVertical(m_CardStyle);
        using (new EditorGUILayout.HorizontalScope())
        {
            DrawActionButton("Erase", ToolMode.Erase);
            DrawActionButton("None", ToolMode.Off);
        }

        m_GridSize = Mathf.Max(0.25f, EditorGUILayout.FloatField("Grid Size", m_GridSize));
        EditorGUILayout.EndVertical();
    }

    private void DrawActionButton(string label, ToolMode mode)
    {
        Color previousColor = GUI.backgroundColor;
        if (m_Mode == mode)
        {
            GUI.backgroundColor = mode == ToolMode.Erase
                ? new Color(1f, 0.45f, 0.35f)
                : new Color(0.7f, 0.7f, 0.7f);
        }

        if (GUILayout.Button(label, GUILayout.Height(26f)))
        {
            CancelPendingPairPlacement();
            CancelTriangleGround();
            m_IsDraggingCameraBounds = false;
            m_Mode = mode;
        }

        GUI.backgroundColor = previousColor;
    }

    private void DrawActiveTab()
    {
        switch (m_Tab)
        {
            case BuilderTab.Field:
                DrawGroundCard();
                break;
            case BuilderTab.Gimmick:
                DrawPrefabCard("Gimmick Palette", BuilderTab.Gimmick);
                break;
            case BuilderTab.PlayerGoal:
                DrawPrefabCard("Player / Goal", BuilderTab.PlayerGoal);
                break;
        }
    }

    private void DrawGroundCard()
    {
        EditorGUILayout.BeginVertical(m_CardStyle);
        EditorGUILayout.LabelField("Field", EditorStyles.boldLabel);

        DrawGroundToolButton();
        DrawTriangleGroundToolButton();
        DrawCameraBoundsToolButton();

        EditorGUILayout.Space(4f);
        m_DefaultGroundSize = DrawSizeField("Size", m_DefaultGroundSize);

        EditorGUILayout.EndVertical();
    }

    private void DrawGroundToolButton()
    {
        bool selected = m_Mode == ToolMode.Ground;
        Color previousColor = GUI.backgroundColor;
        if (selected)
        {
            GUI.backgroundColor = new Color(0.55f, 0.85f, 1f);
        }

        if (GUILayout.Button("Ground", selected ? m_SelectedButtonStyle : m_ButtonStyle, GUILayout.Height(34f)))
        {
            m_Mode = ToolMode.Ground;
        }

        GUI.backgroundColor = previousColor;
    }

    private void DrawCameraBoundsToolButton()
    {
        bool selected = m_Mode == ToolMode.CameraBounds;
        Color previousColor = GUI.backgroundColor;
        if (selected)
        {
            GUI.backgroundColor = new Color(1f, 0.85f, 0.35f);
        }

        if (GUILayout.Button("Camera Bounds", selected ? m_SelectedButtonStyle : m_ButtonStyle, GUILayout.Height(34f)))
        {
            CancelPendingPairPlacement();
            CancelTriangleGround();
            m_IsDraggingCameraBounds = false;
            m_Mode = ToolMode.CameraBounds;
        }

        GUI.backgroundColor = previousColor;
    }

    private void DrawPrefabCard(string title, BuilderTab tab)
    {
        EditorGUILayout.BeginVertical(m_CardStyle);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

        List<PaletteItem> visibleItems = GetPaletteItems(tab);
        DrawPaletteGrid(visibleItems);

        EditorGUILayout.Space(4f);

        if (IsSelectedPrefabVisibleInTab(tab))
        {
            DrawSelectedPrefabSizeControls();
            DrawSelectedPrefabPreview();
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawPaletteGrid(List<PaletteItem> visibleItems)
    {
        const int columns = 2;
        const float gap = 6f;
        const float cellHeight = 38f;

        int rows = Mathf.CeilToInt(visibleItems.Count / (float)columns);
        float totalHeight = rows * cellHeight + Mathf.Max(0, rows - 1) * gap;
        Rect gridRect = GUILayoutUtility.GetRect(0f, totalHeight, GUILayout.ExpandWidth(true));
        float cellWidth = (gridRect.width - gap * (columns - 1)) / columns;

        for (int i = 0; i < visibleItems.Count; i++)
        {
            int row = i / columns;
            int column = i % columns;
            Rect cellRect = new Rect(
                gridRect.x + column * (cellWidth + gap),
                gridRect.y + row * (cellHeight + gap),
                cellWidth,
                cellHeight);

            DrawPaletteButton(cellRect, visibleItems[i]);
        }
    }

    private void DrawPaletteButton(Rect buttonRect, PaletteItem item)
    {
        bool selected = m_Mode == ToolMode.Prefab && m_SelectedPrefabName == item.PrefabName;
        GUIStyle style = selected ? m_SelectedButtonStyle : m_ButtonStyle;
        Color previousColor = GUI.backgroundColor;
        if (selected)
        {
            GUI.backgroundColor = new Color(0.55f, 0.85f, 1f);
        }

        using (new EditorGUI.DisabledScope(!m_Prefabs.ContainsKey(item.PrefabName)))
        {
            Texture2D preview = GetPrefabPreview(item.PrefabName);
            GUIContent content = new GUIContent(item.Label, preview);
            if (GUI.Button(buttonRect, content, style))
            {
                if (m_SelectedPrefabName != item.PrefabName)
                {
                    CancelPendingPairPlacement();
                }

                m_Mode = ToolMode.Prefab;
                m_SelectedPrefabName = item.PrefabName;
            }
        }

        GUI.backgroundColor = previousColor;
    }

    private void DrawTriangleGroundToolButton()
    {
        bool selected = m_Mode == ToolMode.TriangleGround;
        Color previousColor = GUI.backgroundColor;
        if (selected)
        {
            GUI.backgroundColor = new Color(0.55f, 0.85f, 1f);
        }

        if (GUILayout.Button("Triangle Ground", selected ? m_SelectedButtonStyle : m_ButtonStyle, GUILayout.Height(34f)))
        {
            CancelPendingPairPlacement();
            m_Mode = ToolMode.TriangleGround;
            m_TrianglePoints.Clear();
        }

        GUI.backgroundColor = previousColor;
    }

    private void DrawSelectedPrefabSizeControls()
    {
        EditorGUILayout.Space(4f);

        if (IsSequentialPairPrefab(m_SelectedPrefabName))
        {
            Vector2 controlSize = GetPrefabSize(m_SelectedPrefabName);
            Vector2 nextControlSize = DrawSizeField("Control", controlSize);
            SetPrefabSize(m_SelectedPrefabName, nextControlSize);

            Vector2 targetSize = GetPairTargetSize(m_SelectedPrefabName);
            Vector2 nextTargetSize = DrawSizeField("Target", targetSize);
            SetPairTargetSize(m_SelectedPrefabName, nextTargetSize);
            return;
        }

        Vector2 size = GetPrefabSize(m_SelectedPrefabName);
        Vector2 nextSize = DrawSizeField("Size", size);
        SetPrefabSize(m_SelectedPrefabName, nextSize);
    }

    private Vector2 DrawSizeField(string label, Vector2 value)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label(label, m_FieldLabelStyle);
            GUILayout.Label("X", EditorStyles.miniLabel, GUILayout.Width(12f));
            value.x = EditorGUILayout.FloatField(value.x, GUILayout.MinWidth(48f));
            GUILayout.Space(8f);
            GUILayout.Label("Y", EditorStyles.miniLabel, GUILayout.Width(12f));
            value.y = EditorGUILayout.FloatField(value.y, GUILayout.MinWidth(48f));
        }

        return value;
    }

    private void DrawSelectedPrefabPreview()
    {
        if (!m_Prefabs.TryGetValue(m_SelectedPrefabName, out GameObject selectedPrefab) || selectedPrefab == null)
        {
            return;
        }

        Texture2D preview = GetPrefabPreview(m_SelectedPrefabName);

        Rect previewRect = GUILayoutUtility.GetRect(0f, 88f, m_PreviewBoxStyle, GUILayout.ExpandWidth(true));
        GUI.Box(previewRect, GUIContent.none, m_PreviewBoxStyle);

        Rect imageRect = new Rect(previewRect.x + 8f, previewRect.y + 8f, 72f, 72f);
        Rect textRect = new Rect(imageRect.xMax + 10f, previewRect.y + 12f, Mathf.Max(80f, previewRect.width - imageRect.width - 30f), 60f);

        if (preview != null)
        {
            GUI.DrawTexture(imageRect, preview, ScaleMode.ScaleToFit, true);
        }

        GUI.Label(textRect, selectedPrefab.name, EditorStyles.boldLabel);

        if (AssetPreview.IsLoadingAssetPreview(selectedPrefab.GetInstanceID()))
        {
            Repaint();
        }
    }

    private Texture2D GetPrefabPreview(string prefabName)
    {
        if (!m_Prefabs.TryGetValue(prefabName, out GameObject prefab) || prefab == null)
        {
            return null;
        }

        if (prefabName == "SawBlade")
        {
            return GetSawBladePreviewTexture();
        }

        Texture2D preview = AssetPreview.GetAssetPreview(prefab);
        return preview != null ? preview : AssetPreview.GetMiniThumbnail(prefab);
    }

    private Texture2D GetSawBladePreviewTexture()
    {
        if (m_SawBladePreviewTexture != null)
        {
            return m_SawBladePreviewTexture;
        }

        const int size = 96;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.HideAndDontSave,
            wrapMode = TextureWrapMode.Clamp
        };

        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float toothTipRadius = size * 0.42f;
        float baseOuterRadius = size * 0.37f;
        float shoulderRadius = size * 0.395f;
        float valleyRadius = size * 0.34f;
        int teethCount = 22;
        float toothSpan = Mathf.PI * 2f / teethCount;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 point = new Vector2(x + 0.5f, y + 0.5f) - center;
                float radius = point.magnitude;
                float angle = Mathf.Atan2(point.y, point.x);
                if (angle < 0f)
                {
                    angle += Mathf.PI * 2f;
                }

                float localRatio = (angle % toothSpan) / toothSpan;
                float toothRadius = valleyRadius;
                if (localRatio < 0.12f)
                {
                    toothRadius = Mathf.Lerp(valleyRadius, baseOuterRadius, localRatio / 0.12f);
                }
                else if (localRatio < 0.34f)
                {
                    toothRadius = Mathf.Lerp(baseOuterRadius, toothTipRadius, Mathf.InverseLerp(0.12f, 0.34f, localRatio));
                }
                else if (localRatio < 0.84f)
                {
                    toothRadius = Mathf.Lerp(toothTipRadius, shoulderRadius, Mathf.InverseLerp(0.34f, 0.84f, localRatio));
                }
                else
                {
                    toothRadius = Mathf.Lerp(shoulderRadius, valleyRadius, Mathf.InverseLerp(0.84f, 1f, localRatio));
                }

                if (radius <= toothRadius)
                {
                    float shade = Mathf.Lerp(0.88f, 0.68f, radius / toothTipRadius);
                    texture.SetPixel(x, y, new Color(shade, shade + 0.02f, shade + 0.04f, 1f));
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 point = new Vector2(x + 0.5f, y + 0.5f) - center;
                if (point.magnitude <= size * 0.13f)
                {
                    texture.SetPixel(x, y, new Color(0.24f, 0.18f, 0.12f, 1f));
                }
            }
        }

        texture.Apply();
        m_SawBladePreviewTexture = texture;
        return m_SawBladePreviewTexture;
    }

    private Vector2 GetPrefabSize(string prefabName)
    {
        if (string.IsNullOrEmpty(prefabName))
        {
            return Vector2.one * m_GridSize;
        }

        if (!m_PrefabSizeOverrides.TryGetValue(prefabName, out Vector2 size))
        {
            size = GetDefaultPrefabSize(prefabName);
            m_PrefabSizeOverrides[prefabName] = size;
        }

        return SanitizeSize(size);
    }

    private void SetPrefabSize(string prefabName, Vector2 size)
    {
        if (!string.IsNullOrEmpty(prefabName))
        {
            m_PrefabSizeOverrides[prefabName] = SanitizeSize(size);
        }
    }

    private Vector2 GetPairTargetSize(string prefabName)
    {
        if (string.IsNullOrEmpty(prefabName))
        {
            return Vector2.one * m_GridSize;
        }

        if (!m_PairTargetSizeOverrides.TryGetValue(prefabName, out Vector2 size))
        {
            size = GetDefaultPairTargetSize(prefabName);
            m_PairTargetSizeOverrides[prefabName] = size;
        }

        return SanitizeSize(size);
    }

    private void SetPairTargetSize(string prefabName, Vector2 size)
    {
        if (!string.IsNullOrEmpty(prefabName))
        {
            m_PairTargetSizeOverrides[prefabName] = SanitizeSize(size);
        }
    }

    private Vector2 GetDefaultPrefabSize(string prefabName)
    {
        return prefabName switch
        {
            "DoorObject" or "OpenDoor" => Vector2.one * m_GridSize,
            "SwitchObject" or "SwitchAndDoorObject" or "OpenDoorAndSwitchObject" => new Vector2(m_GridSize * 2f, m_GridSize * 0.5f),
            "HangLever" => new Vector2(m_GridSize, m_GridSize * 2f),
            "SawBlade" => Vector2.one * m_GridSize,
            "LowGravity" or "AccelerationArea" => Vector2.one * m_GridSize,
            "Player" => new Vector2(m_GridSize, m_GridSize * 2f),
            _ => MeasurePrefabSize(prefabName)
        };
    }

    private Vector2 GetDefaultPairTargetSize(string prefabName)
    {
        return prefabName switch
        {
            "SwitchAndDoorObject" or "OpenDoorAndSwitchObject" => Vector2.one * m_GridSize,
            _ => Vector2.one * m_GridSize
        };
    }

    private Vector2 MeasurePrefabSize(string prefabName)
    {
        if (!m_Prefabs.TryGetValue(prefabName, out GameObject prefab) || prefab == null)
        {
            return Vector2.one * m_GridSize;
        }

        if (TryGetRendererBounds(prefab, out Bounds bounds)
            && bounds.size.x > 0.01f
            && bounds.size.y > 0.01f)
        {
            return new Vector2(bounds.size.x, bounds.size.y);
        }

        return Vector2.one * m_GridSize;
    }

    private Vector2 MeasurePlayerColliderSize()
    {
        if (!m_Prefabs.TryGetValue("Player", out GameObject prefab) || prefab == null)
        {
            return Vector2.one * m_GridSize;
        }

        BoxCollider2D collider = prefab.GetComponent<BoxCollider2D>();
        if (collider == null)
        {
            return MeasurePrefabSize("Player");
        }

        Vector3 scale = prefab.transform.localScale;
        Vector2 size = new(
            Mathf.Abs(collider.size.x * scale.x),
            Mathf.Abs(collider.size.y * scale.y));

        return SanitizeSize(size);
    }

    private Vector2 SanitizeSize(Vector2 size)
    {
        return new Vector2(
            Mathf.Max(m_GridSize * 0.25f, size.x),
            Mathf.Max(m_GridSize * 0.25f, size.y));
    }

    private List<PaletteItem> GetPaletteItems(BuilderTab tab)
    {
        List<PaletteItem> items = new List<PaletteItem>();
        foreach (PaletteItem item in PaletteItems)
        {
            if (item.Tab == tab)
            {
                items.Add(item);
            }
        }

        return items;
    }

    private bool IsSelectedPrefabVisibleInTab(BuilderTab tab)
    {
        foreach (PaletteItem item in PaletteItems)
        {
            if (item.Tab == tab && item.PrefabName == m_SelectedPrefabName)
            {
                return true;
            }
        }

        return false;
    }

    private void SelectFirstPrefabInTab(BuilderTab tab)
    {
        if (IsSelectedPrefabVisibleInTab(tab))
        {
            return;
        }

        foreach (PaletteItem item in PaletteItems)
        {
            if (item.Tab == tab)
            {
                m_SelectedPrefabName = item.PrefabName;
                return;
            }
        }
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (m_Mode == ToolMode.Off)
        {
            return;
        }

        Event current = Event.current;
        if (current == null || current.alt)
        {
            return;
        }

        Vector2 mouseWorld = GetMouseWorldPosition(current.mousePosition);
        Vector2 cellOrigin = SnapToCellOrigin(mouseWorld);
        Vector2 gridPoint = SnapToGridPoint(mouseWorld);
        AttachSide attachSide = GetPrefabAttachSide(mouseWorld, cellOrigin);
        Quaternion placementRotation = GetPrefabPlacementRotation(cellOrigin, attachSide);
        Vector2 placementPosition = GetPrefabPlacementPosition(cellOrigin, attachSide, placementRotation);
        DrawSceneOverlay(cellOrigin);
        DrawCursorPrefabPreview(cellOrigin, placementPosition, placementRotation, attachSide);

        if (current.type == EventType.MouseMove)
        {
            sceneView.Repaint();
        }

        if (m_Mode == ToolMode.Ground)
        {
            HandleGroundInput(current, cellOrigin);
        }
        else if (m_Mode == ToolMode.TriangleGround)
        {
            HandleTriangleGroundInput(current, gridPoint);
        }
        else if (m_Mode == ToolMode.CameraBounds)
        {
            HandleCameraBoundsInput(current, cellOrigin);
        }
        else if (m_Mode == ToolMode.Prefab)
        {
            if (m_PendingOpenDoorObject != null)
            {
                HandlePendingOpenDoorMoveEndInput(current, cellOrigin);
            }
            else if (m_PendingPairObject != null)
            {
                if (m_IsWaitingForPairMoveEnd)
                {
                    HandlePendingPairMoveEndInput(current, cellOrigin);
                }
                else
                {
                    HandlePendingPairTargetInput(current, cellOrigin);
                }
            }
            else if (IsAreaPrefab(m_SelectedPrefabName))
            {
                HandleAreaInput(current, cellOrigin);
            }
            else
            {
                HandlePrefabInput(current, placementPosition, placementRotation, attachSide);
            }
        }
        else if (m_Mode == ToolMode.Erase)
        {
            HandleEraseInput(current);
        }

        if (GUI.changed)
        {
            sceneView.Repaint();
        }
    }

    private void HandleGroundInput(Event current, Vector2 cellOrigin)
    {
        if (current.type == EventType.MouseDown && current.button == 0)
        {
            m_IsDraggingGround = true;
            m_GroundStart = cellOrigin;
            m_GroundEnd = cellOrigin;
            current.Use();
        }

        if (m_IsDraggingGround && current.type == EventType.MouseDrag)
        {
            m_GroundEnd = cellOrigin;
            current.Use();
            SceneView.RepaintAll();
        }

        if (m_IsDraggingGround)
        {
            DrawGroundPreview(m_GroundStart, m_GroundEnd);
        }

        if (m_IsDraggingGround && current.type == EventType.MouseUp && current.button == 0)
        {
            m_GroundEnd = cellOrigin;
            CreateGround(GetToolRect(m_GroundStart, m_GroundEnd, m_DefaultGroundSize));
            m_IsDraggingGround = false;
            current.Use();
        }
    }

    private void HandleTriangleGroundInput(Event current, Vector2 gridPoint)
    {
        DrawTriangleGroundPreview(gridPoint);

        if (current.type == EventType.MouseMove)
        {
            SceneView.RepaintAll();
        }

        if (current.type != EventType.MouseDown || current.button != 0)
        {
            return;
        }

        if (m_TrianglePoints.Count == 0 || m_TrianglePoints[m_TrianglePoints.Count - 1] != gridPoint)
        {
            m_TrianglePoints.Add(gridPoint);
        }

        if (m_TrianglePoints.Count >= 3)
        {
            CreateTriangleGround(m_TrianglePoints[0], m_TrianglePoints[1], m_TrianglePoints[2]);
            m_TrianglePoints.Clear();
        }

        current.Use();
        SceneView.RepaintAll();
    }

    private void HandleCameraBoundsInput(Event current, Vector2 cellOrigin)
    {
        if (current.type == EventType.MouseDown && current.button == 0)
        {
            m_IsDraggingCameraBounds = true;
            m_CameraBoundsStart = cellOrigin;
            m_CameraBoundsEnd = cellOrigin;
            current.Use();
        }

        if (m_IsDraggingCameraBounds && current.type == EventType.MouseDrag)
        {
            m_CameraBoundsEnd = cellOrigin;
            current.Use();
            SceneView.RepaintAll();
        }

        if (m_IsDraggingCameraBounds)
        {
            DrawCameraBoundsPreview(GetCellSelectionRect(m_CameraBoundsStart, m_CameraBoundsEnd));
        }
        else
        {
            DrawExistingCameraBounds();
        }

        if (m_IsDraggingCameraBounds && current.type == EventType.MouseUp && current.button == 0)
        {
            m_CameraBoundsEnd = cellOrigin;
            ApplyCameraBounds(GetCellSelectionRect(m_CameraBoundsStart, m_CameraBoundsEnd));
            m_IsDraggingCameraBounds = false;
            current.Use();
        }
    }

    private void HandlePrefabInput(Event current, Vector2 mouseWorld, Quaternion rotation, AttachSide attachSide)
    {
        if (current.type != EventType.MouseDown || current.button != 0)
        {
            return;
        }

        if (IsSequentialPairPrefab(m_SelectedPrefabName))
        {
            HandleSequentialPairPlacement(mouseWorld, rotation, attachSide);
            current.Use();
            return;
        }

        PlaceSelectedPrefab(mouseWorld, rotation, attachSide);
        current.Use();
    }

    private void HandleAreaInput(Event current, Vector2 cellOrigin)
    {
        if (current.type == EventType.MouseDown && current.button == 0)
        {
            m_IsDraggingArea = true;
            m_AreaStart = cellOrigin;
            m_AreaEnd = cellOrigin;
            current.Use();
        }

        if (m_IsDraggingArea && current.type == EventType.MouseDrag)
        {
            m_AreaEnd = cellOrigin;
            current.Use();
            SceneView.RepaintAll();
        }

        if (m_IsDraggingArea)
        {
            DrawAreaPreview(m_AreaStart, m_AreaEnd);
        }

        if (m_IsDraggingArea && current.type == EventType.MouseUp && current.button == 0)
        {
            m_AreaEnd = cellOrigin;
            PlaceAreaPrefab(GetToolRect(m_AreaStart, m_AreaEnd, GetPrefabSize(m_SelectedPrefabName)));
            m_IsDraggingArea = false;
            current.Use();
        }
    }

    private void HandlePendingPairTargetInput(Event current, Vector2 cellOrigin)
    {
        if (current.type == EventType.MouseDown && current.button == 0)
        {
            m_IsDraggingPairTarget = true;
            m_PairTargetStart = cellOrigin;
            m_PairTargetEnd = cellOrigin;
            current.Use();
        }

        if (m_IsDraggingPairTarget && current.type == EventType.MouseDrag)
        {
            m_PairTargetEnd = cellOrigin;
            current.Use();
            SceneView.RepaintAll();
        }

        if (m_IsDraggingPairTarget)
        {
            Rect rect = GetToolRect(m_PairTargetStart, m_PairTargetEnd, GetPairTargetSize(m_PendingPairPrefabName));
            DrawPairTargetPreview(rect);
            UpdatePendingPairTargetPreview(rect);
        }

        if (m_IsDraggingPairTarget && current.type == EventType.MouseUp && current.button == 0)
        {
            m_PairTargetEnd = cellOrigin;
            CompleteSequentialPairPlacement(GetToolRect(m_PairTargetStart, m_PairTargetEnd, GetPairTargetSize(m_PendingPairPrefabName)));
            m_IsDraggingPairTarget = false;
            current.Use();
        }
    }

    private void HandlePendingPairMoveEndInput(Event current, Vector2 cellOrigin)
    {
        Rect moveEndRect = GetPendingPairMoveEndRect(cellOrigin);
        DrawPairMoveEndPreview(moveEndRect);

        if (current.type == EventType.MouseMove)
        {
            SceneView.RepaintAll();
        }

        if (current.type != EventType.MouseDown || current.button != 0)
        {
            return;
        }

        CompletePendingPairMoveEnd(moveEndRect);
        current.Use();
    }

    private void HandlePendingOpenDoorMoveEndInput(Event current, Vector2 cellOrigin)
    {
        Rect moveEndRect = GetMoveEndRect(cellOrigin, m_PendingOpenDoorRect.size);
        DrawPairMoveEndPreview(moveEndRect);

        if (current.type == EventType.MouseMove)
        {
            SceneView.RepaintAll();
        }

        if (current.type != EventType.MouseDown || current.button != 0)
        {
            return;
        }

        CompletePendingOpenDoorMoveEnd(moveEndRect);
        current.Use();
    }

    private void HandleEraseInput(Event current)
    {
        if (current.type != EventType.MouseDown || current.button != 0)
        {
            return;
        }

        GameObject picked = HandleUtility.PickGameObject(current.mousePosition, false);
        if (picked == null)
        {
            return;
        }

        Undo.DestroyObjectImmediate(picked);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        current.Use();
    }

    private void DrawSceneOverlay(Vector2 cellOrigin)
    {
        Color color = m_Mode switch
        {
            ToolMode.Erase => new Color(1f, 0.35f, 0.25f, 0.8f),
            ToolMode.CameraBounds => new Color(1f, 0.78f, 0.18f, 0.9f),
            _ => new Color(0.2f, 0.8f, 1f, 0.75f)
        };

        Handles.color = color;
        Rect cellRect = new Rect(cellOrigin.x, cellOrigin.y, m_GridSize, m_GridSize);
        DrawRectOutline(cellRect, color);

        Handles.BeginGUI();
        Rect labelRect = new Rect(12f, 12f, 260f, 42f);
        EditorGUI.DrawRect(labelRect, new Color(0f, 0f, 0f, 0.55f));
        GUI.Label(new Rect(labelRect.x + 8f, labelRect.y + 5f, labelRect.width - 16f, labelRect.height - 10f), GetSceneHint(), EditorStyles.whiteMiniLabel);
        Handles.EndGUI();
    }

    private void DrawGroundPreview(Vector2 start, Vector2 end)
    {
        Rect rect = GetCellSelectionRect(start, end);
        DrawRect(rect, new Color(1f, 1f, 1f, 0.16f), Color.white);
    }

    private void DrawTriangleGroundPreview(Vector2 currentPoint)
    {
        Color outline = new Color(0.25f, 1f, 0.55f, 0.95f);
        Color fill = new Color(outline.r, outline.g, outline.b, 0.16f);

        Handles.color = outline;
        foreach (Vector2 point in m_TrianglePoints)
        {
            Handles.DrawSolidDisc(point, Vector3.forward, Mathf.Max(m_GridSize * 0.08f, 0.04f));
        }

        if (m_TrianglePoints.Count == 0)
        {
            Handles.DrawWireDisc(currentPoint, Vector3.forward, Mathf.Max(m_GridSize * 0.08f, 0.04f));
            return;
        }

        if (m_TrianglePoints.Count == 1)
        {
            Handles.DrawAAPolyLine(3f, m_TrianglePoints[0], currentPoint);
            Handles.DrawWireDisc(currentPoint, Vector3.forward, Mathf.Max(m_GridSize * 0.08f, 0.04f));
            return;
        }

        Vector3[] vertices =
        {
            m_TrianglePoints[0],
            m_TrianglePoints[1],
            currentPoint
        };
        Handles.color = fill;
        Handles.DrawAAConvexPolygon(vertices);
        Handles.color = outline;
        Handles.DrawAAPolyLine(3f, m_TrianglePoints[0], m_TrianglePoints[1], currentPoint, m_TrianglePoints[0]);
        Handles.DrawWireDisc(currentPoint, Vector3.forward, Mathf.Max(m_GridSize * 0.08f, 0.04f));
    }

    private void DrawAreaPreview(Vector2 start, Vector2 end)
    {
        Rect rect = GetCellSelectionRect(start, end);
        Color outline = GetAreaPreviewColor(m_SelectedPrefabName);
        Color fill = new Color(outline.r, outline.g, outline.b, 0.16f);
        DrawRect(rect, fill, outline);
    }

    private void DrawPairTargetPreview(Rect rect)
    {
        Color outline = new Color(0.65f, 0.45f, 1f, 0.95f);
        DrawRect(rect, new Color(outline.r, outline.g, outline.b, 0.16f), outline);
    }

    private void DrawPairMoveEndPreview(Rect rect)
    {
        Color outline = new Color(0.25f, 1f, 0.55f, 0.95f);
        DrawRect(rect, new Color(outline.r, outline.g, outline.b, 0.16f), outline);
    }

    private void DrawCameraBoundsPreview(Rect rect)
    {
        Color outline = new Color(1f, 0.78f, 0.18f, 0.95f);
        DrawRect(rect, new Color(outline.r, outline.g, outline.b, 0.12f), outline);
    }

    private void DrawExistingCameraBounds()
    {
        if (!TryGetCameraBounds(out Rect rect))
        {
            return;
        }

        DrawCameraBoundsPreview(rect);
    }

    private void ApplyCameraBounds(Rect rect)
    {
        PlayerRespawn playerRespawn = FindFirstObjectByType<PlayerRespawn>();
        if (playerRespawn == null)
        {
            Debug.LogWarning("PlayerRespawn was not found. Place a Player before setting camera bounds.");
            return;
        }

        Undo.RecordObject(playerRespawn, "Set Camera Bounds");

        SerializedObject serializedRespawn = new SerializedObject(playerRespawn);
        serializedRespawn.FindProperty("useCameraBounds").boolValue = true;
        serializedRespawn.FindProperty("cameraBoundsMin").vector2Value = rect.min;
        serializedRespawn.FindProperty("cameraBoundsMax").vector2Value = rect.max;
        serializedRespawn.ApplyModifiedProperties();

        EditorUtility.SetDirty(playerRespawn);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    private bool TryGetCameraBounds(out Rect rect)
    {
        rect = default;

        PlayerRespawn playerRespawn = FindFirstObjectByType<PlayerRespawn>();
        if (playerRespawn == null)
        {
            return false;
        }

        SerializedObject serializedRespawn = new SerializedObject(playerRespawn);
        SerializedProperty useBoundsProperty = serializedRespawn.FindProperty("useCameraBounds");
        SerializedProperty minProperty = serializedRespawn.FindProperty("cameraBoundsMin");
        SerializedProperty maxProperty = serializedRespawn.FindProperty("cameraBoundsMax");

        if (useBoundsProperty == null || minProperty == null || maxProperty == null || !useBoundsProperty.boolValue)
        {
            return false;
        }

        Vector2 min = Vector2.Min(minProperty.vector2Value, maxProperty.vector2Value);
        Vector2 max = Vector2.Max(minProperty.vector2Value, maxProperty.vector2Value);
        rect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        return true;
    }

    private void DrawCursorPrefabPreview(Vector2 cellOrigin, Vector2 placementPosition, Quaternion rotation, AttachSide attachSide)
    {
        if (m_Mode != ToolMode.Prefab || IsAreaPrefab(m_SelectedPrefabName) || m_PendingOpenDoorObject != null)
        {
            DestroyCursorPreview();
            return;
        }

        if (m_IsWaitingForPairMoveEnd)
        {
            DestroyCursorPreview();
            return;
        }

        UpdateCursorPreviewObject(placementPosition, rotation, attachSide);

        Rect footprint = GetPrefabFootprintRect(cellOrigin, placementPosition, rotation);
        if (m_SelectedPrefabName == "SwitchObject"
            && m_PendingPairObject == null
            && TryGetSwitchBaseBoundsRect(m_CursorPreviewObject, true, out Rect switchBaseFootprint))
        {
            footprint = switchBaseFootprint;
        }

        Color outline = new Color(0.2f, 0.8f, 1f, 0.95f);
        DrawRect(footprint, new Color(0.2f, 0.8f, 1f, 0.12f), outline);
    }

    private void CreateGroundFromDrag(Vector2 start, Vector2 end)
    {
        Rect rect = GetCellSelectionRect(start, end);
        Vector2 size = rect.size;
        if (size.x < 0.01f || size.y < 0.01f)
        {
            size = new Vector2(
                Mathf.Max(m_GridSize, m_DefaultGroundSize.x),
                Mathf.Max(m_GridSize, m_DefaultGroundSize.y));
            rect = new Rect(start, size);
        }

        CreateGround(rect);
    }

    private void CreateGround(Rect worldRect)
    {
        GameObject ground = new GameObject(GetUniqueGroundName());
        Undo.RegisterCreatedObjectUndo(ground, "Create Ground");

        int floorLayer = LayerMask.NameToLayer("Floor");
        if (floorLayer >= 0)
        {
            ground.layer = floorLayer;
        }

        Transform parent = EnsureGroundParent().transform;
        ground.transform.SetParent(parent, false);
        SetWorldRectTransform(ground.transform, worldRect, parent);

        SpriteRenderer renderer = ground.AddComponent<SpriteRenderer>();
        renderer.sprite = m_GroundSprite;
        renderer.sortingOrder = 0;

        BoxCollider2D collider2D = ground.AddComponent<BoxCollider2D>();
        collider2D.size = Vector2.one;

        Selection.activeGameObject = ground;
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    private void CreateTriangleGround(Vector2 first, Vector2 second, Vector2 third)
    {
        float area = Mathf.Abs(Vector3.Cross(second - first, third - first).z);
        if (Mathf.Approximately(area, 0f))
        {
            Debug.LogWarning("Triangle ground points are collinear.");
            return;
        }

        GameObject triangle = new GameObject(GetUniqueTriangleGroundName());
        Undo.RegisterCreatedObjectUndo(triangle, "Create Triangle Ground");

        Transform parent = EnsureGroundParent().transform;
        triangle.transform.SetParent(parent, true);
        triangle.transform.position = new Vector3(first.x, first.y, 0f);
        triangle.transform.rotation = Quaternion.identity;
        triangle.transform.localScale = Vector3.one;

        TriangleMesh triangleMesh = triangle.AddComponent<TriangleMesh>();
        triangleMesh.SetPoints(Vector2.zero, second - first, third - first);

        Selection.activeGameObject = triangle;
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    private void PlaceSelectedPrefab(Vector2 position, Quaternion rotation, AttachSide attachSide)
    {
        if (!m_Prefabs.TryGetValue(m_SelectedPrefabName, out GameObject prefab) || prefab == null)
        {
            Debug.LogWarning($"{m_SelectedPrefabName} prefab is missing.");
            return;
        }

        GameObject placed = PrefabUtility.InstantiatePrefab(prefab, SceneManager.GetActiveScene()) as GameObject;
        if (placed == null)
        {
            return;
        }

        Undo.RegisterCreatedObjectUndo(placed, $"Place {m_SelectedPrefabName}");
        placed.transform.position = new Vector3(position.x, position.y, 0f);
        placed.transform.rotation = rotation;
        Vector2 size = GetPrefabSize(m_SelectedPrefabName);
        Rect sizeRect = GetCenteredFootprint(position, size);
        if (m_SelectedPrefabName == "HangLever")
        {
            ConfigureHangLeverGridSize(placed, position, size, attachSide);
        }
        else if (IsSwitchPrefab(m_SelectedPrefabName))
        {
            ConfigureSwitchBaseGridSize(placed, size);
            AlignSwitchBaseBoundsCenter(placed, position);
        }
        else if (m_SelectedPrefabName == "Player")
        {
            FitPlayerColliderToRect(placed, sizeRect);
        }
        else if (m_SelectedPrefabName == "SawBlade")
        {
            ConfigureSawBladeGridSize(placed, sizeRect);
        }
        else
        {
            FitRendererBoundsToRect(placed, sizeRect);
        }

        Selection.activeGameObject = placed;
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    private void HandleSequentialPairPlacement(Vector2 position, Quaternion rotation, AttachSide attachSide)
    {
        if (m_PendingPairObject == null || m_PendingPairPrefabName != m_SelectedPrefabName)
        {
            BeginSequentialPairPlacement(position, rotation, attachSide);
            return;
        }

        CompleteSequentialPairPlacement(GetCenteredFootprint(position, GetPairTargetSize(m_SelectedPrefabName)));
    }

    private void BeginSequentialPairPlacement(Vector2 switchPosition, Quaternion rotation, AttachSide attachSide)
    {
        if (!m_Prefabs.TryGetValue(m_SelectedPrefabName, out GameObject prefab) || prefab == null)
        {
            Debug.LogWarning($"{m_SelectedPrefabName} prefab is missing.");
            return;
        }

        GameObject placed = PrefabUtility.InstantiatePrefab(prefab, SceneManager.GetActiveScene()) as GameObject;
        if (placed == null)
        {
            return;
        }

        Undo.RegisterCreatedObjectUndo(placed, $"Place {m_SelectedPrefabName}");
        placed.transform.position = Vector3.zero;

        Transform controlPart = FindPairControlPart(placed.transform, m_SelectedPrefabName);
        Transform doorPart = FindPairDoorPart(placed.transform, m_SelectedPrefabName);
        if (controlPart == null || doorPart == null)
        {
            Debug.LogWarning($"{m_SelectedPrefabName} pair parts are missing.");
            Undo.DestroyObjectImmediate(placed);
            return;
        }

        PlacePairPart(controlPart, switchPosition, rotation, GetPrefabSize(m_SelectedPrefabName), attachSide);
        doorPart.gameObject.SetActive(false);

        m_PendingPairObject = placed;
        m_PendingPairPrefabName = m_SelectedPrefabName;
        m_PendingPairDoor = doorPart;
        Selection.activeGameObject = controlPart.gameObject;
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    private void CompleteSequentialPairPlacement(Rect targetRect)
    {
        if (m_PendingPairObject == null || m_PendingPairDoor == null)
        {
            ClearPendingPairPlacement();
            return;
        }

        m_PendingPairDoor.gameObject.SetActive(true);
        PlacePairTarget(m_PendingPairDoor, targetRect);

        if (ShouldPlacePairMoveEnd(m_PendingPairPrefabName, m_PendingPairDoor))
        {
            m_PendingPairTargetRect = targetRect;
            m_IsWaitingForPairMoveEnd = true;
            SetOpenDoorEndRect(m_PendingPairDoor, OffsetRect(targetRect, Vector2.up * m_GridSize));
            Selection.activeGameObject = m_PendingPairDoor.gameObject;
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            DestroyCursorPreview();
            return;
        }

        Selection.activeGameObject = m_PendingPairObject;
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        ClearPendingPairPlacement();
    }

    private void PlacePairTarget(Transform target, Rect targetRect)
    {
        if (target == null)
        {
            return;
        }

        if (IsOpenDoorTarget(target))
        {
            FitOpenDoorBlockToRect(target, targetRect);
            return;
        }

        FitRendererBoundsToRect(target.gameObject, targetRect);
    }

    private void CompletePendingPairMoveEnd(Rect endRect)
    {
        if (m_PendingPairObject == null || m_PendingPairDoor == null)
        {
            ClearPendingPairPlacement();
            return;
        }

        SetOpenDoorEndRect(m_PendingPairDoor, endRect);
        Selection.activeGameObject = m_PendingPairObject;
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        ClearPendingPairPlacement();
    }

    private void PlacePairPart(Transform part, Vector2 position, Quaternion rotation, Vector2 size, AttachSide attachSide = AttachSide.Floor)
    {
        if (part == null)
        {
            return;
        }

        part.position = new Vector3(position.x, position.y, part.position.z);
        part.rotation = rotation;
        if (IsSwitchObjectTransform(part))
        {
            ConfigureSwitchBaseGridSize(part.gameObject, size);
            AlignSwitchBaseBoundsCenter(part.gameObject, position);
            return;
        }
        else if (part.name == "HangLever")
        {
            ConfigureHangLeverGridSize(part.gameObject, position, size, attachSide);
            return;
        }

        FitRendererBoundsToRect(part.gameObject, GetCenteredFootprint(position, size));
    }

    private void CancelPendingPairPlacement()
    {
        if (m_PendingPairObject != null)
        {
            Undo.DestroyObjectImmediate(m_PendingPairObject);
        }

        if (m_PendingOpenDoorObject != null)
        {
            Undo.DestroyObjectImmediate(m_PendingOpenDoorObject);
        }

        ClearPendingPairPlacement();
    }

    private void ClearPendingPairPlacement()
    {
        m_PendingPairObject = null;
        m_PendingPairPrefabName = null;
        m_PendingPairDoor = null;
        m_PendingPairTargetRect = default;
        m_IsWaitingForPairMoveEnd = false;
        m_PendingOpenDoorObject = null;
        m_PendingOpenDoorRect = default;
        m_IsDraggingPairTarget = false;
        DestroyCursorPreview();
    }

    private void CancelTriangleGround()
    {
        m_TrianglePoints.Clear();
    }

    private void UpdateCursorPreviewObject(Vector2 position, Quaternion rotation, AttachSide attachSide)
    {
        string previewKey = GetCursorPreviewKey();
        if (string.IsNullOrEmpty(previewKey))
        {
            DestroyCursorPreview();
            return;
        }

        if (m_CursorPreviewObject == null || m_CursorPreviewKey != previewKey)
        {
            DestroyCursorPreview();
            m_CursorPreviewObject = CreateCursorPreviewObject();
            m_CursorPreviewKey = previewKey;
        }

        if (m_CursorPreviewObject == null)
        {
            return;
        }

        if (m_PendingPairDoor != null)
        {
            UpdatePendingPairTargetPreview(GetCenteredFootprint(position, Vector2.one * m_GridSize));
            return;
        }

        if (IsSequentialPairPrefab(m_SelectedPrefabName))
        {
            Transform controlPart = FindPairControlPart(m_CursorPreviewObject.transform, m_SelectedPrefabName);
            Transform doorPart = FindPairDoorPart(m_CursorPreviewObject.transform, m_SelectedPrefabName);
            if (doorPart != null)
            {
                doorPart.gameObject.SetActive(false);
            }

            if (controlPart != null)
            {
                PlacePreviewPart(controlPart, position, rotation, m_SelectedPrefabName, attachSide);
            }

            return;
        }

        if (m_SelectedPrefabName == "HangLever"
            || m_SelectedPrefabName == "SawBlade"
            || m_SelectedPrefabName == "Player"
            || IsSwitchPrefab(m_SelectedPrefabName))
        {
            PlacePreviewPart(m_CursorPreviewObject.transform, position, rotation, m_SelectedPrefabName, attachSide);
            return;
        }

        m_CursorPreviewObject.transform.position = new Vector3(position.x, position.y, 0f);
        m_CursorPreviewObject.transform.rotation = rotation;
        AlignRendererBoundsCenter(m_CursorPreviewObject, position);
    }

    private void PlacePreviewPart(Transform part, Vector2 position, Quaternion rotation, string sourcePrefabName = null, AttachSide attachSide = AttachSide.Floor)
    {
        part.position = new Vector3(position.x, position.y, part.position.z);
        part.rotation = rotation;
        if (IsSwitchObjectTransform(part) || sourcePrefabName == "SwitchObject")
        {
            ConfigureSwitchBaseGridSize(part.gameObject, GetPrefabSize(sourcePrefabName ?? m_SelectedPrefabName));
            AlignSwitchBaseBoundsCenter(part.gameObject, position, sourcePrefabName == "SwitchObject");
            return;
        }
        else if ((sourcePrefabName ?? m_SelectedPrefabName) == "HangLever" || part.name == "HangLever")
        {
            ConfigureHangLeverGridSize(part.gameObject, position, GetPrefabSize(sourcePrefabName ?? m_SelectedPrefabName), attachSide);
            return;
        }

        if ((sourcePrefabName ?? m_SelectedPrefabName) == "Player")
        {
            FitPlayerColliderToRect(part.gameObject, GetCenteredFootprint(position, GetPrefabSize(sourcePrefabName ?? m_SelectedPrefabName)));
            return;
        }

        if ((sourcePrefabName ?? m_SelectedPrefabName) == "SawBlade")
        {
            ConfigureSawBladeGridSize(part.gameObject, GetCenteredFootprint(position, GetPrefabSize(sourcePrefabName ?? m_SelectedPrefabName)));
            return;
        }

        FitRendererBoundsToRect(part.gameObject, GetCenteredFootprint(position, GetPrefabSize(sourcePrefabName ?? m_SelectedPrefabName)));
    }

    private GameObject CreateCursorPreviewObject()
    {
        GameObject source = null;
        if (m_PendingPairDoor != null)
        {
            source = m_PendingPairDoor.gameObject;
        }
        else if (m_Prefabs.TryGetValue(m_SelectedPrefabName, out GameObject prefab))
        {
            source = prefab;
        }

        if (source == null)
        {
            return null;
        }

        GameObject previewObject = Instantiate(source);
        previewObject.name = $"StageBuilderPreview_{source.name}";
        previewObject.SetActive(true);
        PreparePreviewObject(previewObject);
        return previewObject;
    }

    private void UpdatePendingPairTargetPreview(Rect targetRect)
    {
        if (m_CursorPreviewObject == null || m_PendingPairDoor == null)
        {
            return;
        }

        if (IsOpenDoorTarget(m_CursorPreviewObject.transform))
        {
            FitOpenDoorBlockToRect(m_CursorPreviewObject.transform, targetRect);
            return;
        }

        FitRendererBoundsToRect(m_CursorPreviewObject, targetRect);
    }

    private void PreparePreviewObject(GameObject previewObject)
    {
        foreach (Transform child in previewObject.GetComponentsInChildren<Transform>(true))
        {
            child.gameObject.hideFlags = HideFlags.HideAndDontSave | HideFlags.NotEditable;
        }

        foreach (Collider2D collider2D in previewObject.GetComponentsInChildren<Collider2D>(true))
        {
            collider2D.enabled = false;
        }

        foreach (MonoBehaviour behaviour in previewObject.GetComponentsInChildren<MonoBehaviour>(true))
        {
            behaviour.enabled = false;
        }

        foreach (SpriteRenderer renderer in previewObject.GetComponentsInChildren<SpriteRenderer>(true))
        {
            Color color = renderer.color;
            color.a = Mathf.Min(color.a, 0.55f);
            renderer.color = color;
        }
    }

    private string GetCursorPreviewKey()
    {
        if (m_Mode != ToolMode.Prefab || IsAreaPrefab(m_SelectedPrefabName))
        {
            return null;
        }

        if (m_PendingPairDoor != null)
        {
            return $"{m_SelectedPrefabName}:target:{m_PendingPairDoor.name}";
        }

        return $"{m_SelectedPrefabName}:control";
    }

    private void DestroyCursorPreview()
    {
        if (m_CursorPreviewObject != null)
        {
            DestroyImmediate(m_CursorPreviewObject);
            m_CursorPreviewObject = null;
            m_CursorPreviewKey = null;
        }
    }

    private void ConfigureHangLeverGridSize(GameObject leverObject, Vector2 baseCenter, Vector2 baseSize, AttachSide attachSide = AttachSide.Floor)
    {
        if (leverObject == null)
        {
            return;
        }

        Transform baseVisual = leverObject.transform.Find("BaseVisual");
        Transform armPivot = leverObject.transform.Find("LeverArmPivot");
        Transform blocker = leverObject.transform.Find("LeverBlocker");
        Transform armOffset = armPivot != null ? armPivot.Find("ArmOffset") : null;
        Transform armVisual = armOffset != null ? armOffset.Find("ArmVisual") : null;
        int floorLayer = LayerMask.NameToLayer("Floor");

        leverObject.transform.position = new Vector3(baseCenter.x, baseCenter.y, leverObject.transform.position.z);
        leverObject.transform.localScale = Vector3.one;

        if (baseVisual != null)
        {
            if (floorLayer >= 0)
            {
                baseVisual.gameObject.layer = floorLayer;
            }

            baseVisual.localPosition = Vector3.zero;
            SetSpriteRendererLocalSize(baseVisual, baseSize);

            SpriteRenderer baseRenderer = baseVisual.GetComponent<SpriteRenderer>();
            if (baseRenderer != null)
            {
                baseRenderer.flipX = attachSide == AttachSide.RightWall;
            }
        }

        if (blocker != null)
        {
            if (floorLayer >= 0)
            {
                blocker.gameObject.layer = floorLayer;
            }

            blocker.localPosition = Vector3.zero;
            blocker.localScale = new Vector3(baseSize.x, baseSize.y, blocker.localScale.z);
        }

        if (armPivot != null)
        {
            armPivot.localPosition = Vector3.zero;
            armPivot.localRotation = Quaternion.identity;
        }

        if (armOffset != null)
        {
            armOffset.localPosition = Vector3.zero;
            armOffset.localRotation = Quaternion.identity;
            Vector3 armOffsetScale = armOffset.localScale;
            armOffsetScale.x = Mathf.Abs(armOffsetScale.x);
            armOffset.localScale = armOffsetScale;
        }

        if (armVisual != null)
        {
            if (floorLayer >= 0)
            {
                armVisual.gameObject.layer = floorLayer;
            }

            float armLength = Mathf.Max(baseSize.x, baseSize.y);
            SetSpriteRendererLocalWidth(armVisual, armLength);
            armVisual.localPosition = Vector3.zero;
            armVisual.localRotation = Quaternion.identity;
        }

        ConfigureHangLeverArmDirection(leverObject, attachSide);
    }

    private static void ConfigureHangLeverArmDirection(GameObject leverObject, AttachSide attachSide)
    {
        if (leverObject == null)
        {
            return;
        }

        HangLever lever = leverObject.GetComponent<HangLever>();
        if (lever == null)
        {
            return;
        }

        SerializedObject serializedLever = new SerializedObject(lever);
        SerializedProperty idleAngle = serializedLever.FindProperty("idleAngle");
        SerializedProperty activatedAngle = serializedLever.FindProperty("activatedAngle");
        if (idleAngle == null || activatedAngle == null)
        {
            return;
        }

        bool wallIsOnRight = attachSide == AttachSide.RightWall;

        if (wallIsOnRight)
        {
            idleAngle.floatValue = 152f;
            activatedAngle.floatValue = 224f;
        }
        else
        {
            idleAngle.floatValue = 28f;
            activatedAngle.floatValue = -44f;
        }
        serializedLever.ApplyModifiedPropertiesWithoutUndo();
        Transform armOffset = leverObject.transform.Find("LeverArmPivot/ArmOffset");
        if (armOffset != null)
        {
            armOffset.localRotation = Quaternion.identity;
            Vector3 scale = armOffset.localScale;
            scale.x = Mathf.Abs(scale.x);
            armOffset.localScale = scale;
        }

        Transform armPivot = leverObject.transform.Find("LeverArmPivot");
        if (armPivot != null)
        {
            armPivot.localRotation = Quaternion.Euler(0f, 0f, idleAngle.floatValue);
        }

        lever.SendMessage("OnValidate", SendMessageOptions.DontRequireReceiver);
    }

    private void ConfigureSwitchBaseGridSize(GameObject switchObject, Vector2 baseSize)
    {
        foreach (Transform switchBase in switchObject.GetComponentsInChildren<Transform>(true))
        {
            if (!IsSwitchObjectTransform(switchBase))
            {
                continue;
            }

            switchBase.localScale = new Vector3(baseSize.x, baseSize.y, switchBase.localScale.z);

            BoxCollider2D baseCollider = switchBase.GetComponent<BoxCollider2D>();
            if (baseCollider != null)
            {
                baseCollider.size = Vector2.one;
                baseCollider.offset = Vector2.zero;
            }

            SpriteRenderer baseRenderer = switchBase.GetComponent<SpriteRenderer>();
            int baseSortingOrder = baseRenderer != null ? baseRenderer.sortingOrder : 0;

            Transform switchVisual = switchBase.Find("Switch");
            if (switchVisual != null)
            {
                switchVisual.localPosition = new Vector3(0f, 0.75f, switchVisual.localPosition.z);
                switchVisual.localScale = new Vector3(0.8f, 0.5f, switchVisual.localScale.z);

                SpriteRenderer switchRenderer = switchVisual.GetComponent<SpriteRenderer>();
                if (switchRenderer != null)
                {
                    switchRenderer.sortingLayerID = baseRenderer != null ? baseRenderer.sortingLayerID : switchRenderer.sortingLayerID;
                    switchRenderer.sortingOrder = baseSortingOrder + 1;
                    switchRenderer.color = new Color(1f, 0f, 0f, switchRenderer.color.a);
                }
            }

            Transform switchCollision = switchBase.Find("SwitchCollision");
            if (switchCollision != null)
            {
                switchCollision.localPosition = new Vector3(0f, 0.75f, switchCollision.localPosition.z);
                switchCollision.localScale = new Vector3(0.8f, 0.5f, switchCollision.localScale.z);
            }
        }
    }

    private static void AlignSwitchBaseBoundsCenter(GameObject target, Vector2 desiredCenter, bool useRootAsSwitchBase = false)
    {
        if (target == null)
        {
            return;
        }

        Transform switchBase = useRootAsSwitchBase || IsSwitchObjectTransform(target.transform)
            ? target.transform
            : target.transform.Find("SwitchObject");
        if (switchBase == null)
        {
            return;
        }

        SpriteRenderer baseRenderer = switchBase.GetComponent<SpriteRenderer>();
        if (baseRenderer == null)
        {
            return;
        }

        Bounds bounds = baseRenderer.bounds;
        Vector3 offset = new Vector3(desiredCenter.x - bounds.center.x, desiredCenter.y - bounds.center.y, 0f);
        target.transform.position += offset;
    }

    private static bool IsSwitchObjectTransform(Transform transform)
    {
        return transform != null && transform.name.StartsWith("SwitchObject");
    }

    private static bool TryGetSwitchBaseBoundsRect(GameObject target, bool useRootAsSwitchBase, out Rect rect)
    {
        rect = default;
        if (target == null)
        {
            return false;
        }

        Transform switchBase = useRootAsSwitchBase || IsSwitchObjectTransform(target.transform)
            ? target.transform
            : target.transform.Find("SwitchObject");
        if (switchBase == null)
        {
            return false;
        }

        SpriteRenderer baseRenderer = switchBase.GetComponent<SpriteRenderer>();
        if (baseRenderer == null)
        {
            return false;
        }

        Bounds bounds = baseRenderer.bounds;
        rect = new Rect(bounds.min.x, bounds.min.y, bounds.size.x, bounds.size.y);
        return true;
    }

    private void PlaceAreaPrefab(Rect worldRect)
    {
        if (!m_Prefabs.TryGetValue(m_SelectedPrefabName, out GameObject prefab) || prefab == null)
        {
            Debug.LogWarning($"{m_SelectedPrefabName} prefab is missing.");
            return;
        }

        GameObject placed = PrefabUtility.InstantiatePrefab(prefab, SceneManager.GetActiveScene()) as GameObject;
        if (placed == null)
        {
            return;
        }

        Undo.RegisterCreatedObjectUndo(placed, $"Place {m_SelectedPrefabName}");
        if (m_SelectedPrefabName == "OpenDoor")
        {
            FitOpenDoorBlockToRect(placed.transform, worldRect);
            SetOpenDoorEndRect(placed.transform, OffsetRect(worldRect, Vector2.up * m_GridSize));
            m_PendingOpenDoorObject = placed;
            m_PendingOpenDoorRect = worldRect;
            Selection.activeGameObject = placed;
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            return;
        }

        SetWorldRectTransform(placed.transform, worldRect, null);
        AlignRendererBoundsCenter(placed, worldRect.center);
        Selection.activeGameObject = placed;
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    private static bool ShouldPlacePairMoveEnd(string prefabName, Transform target)
    {
        return prefabName == "OpenDoorAndSwitchObject" && IsOpenDoorTarget(target);
    }

    private Rect GetPendingPairMoveEndRect(Vector2 cellOrigin)
    {
        return GetMoveEndRect(cellOrigin, m_PendingPairTargetRect.size);
    }

    private Rect GetMoveEndRect(Vector2 cellOrigin, Vector2 size)
    {
        if (size.x < 0.01f || size.y < 0.01f)
        {
            size = Vector2.one * m_GridSize;
        }

        return new Rect(cellOrigin, size);
    }

    private static bool IsOpenDoorTarget(Transform target)
    {
        if (target == null)
        {
            return false;
        }

        return target.name.StartsWith("OpenDoor")
            || target.name.StartsWith("DoorOnOpenbOject")
            || target.Find("Door") != null && target.Find("endPos") != null;
    }

    private static void FitOpenDoorBlockToRect(Transform openDoorTarget, Rect worldRect)
    {
        if (openDoorTarget == null)
        {
            return;
        }

        Transform doorBlock = FindDescendant(openDoorTarget, "Door") ?? openDoorTarget;
        FitRendererBoundsToRect(doorBlock.gameObject, worldRect);
    }

    private static void SetOpenDoorEndRect(Transform openDoorTarget, Rect endRect)
    {
        if (openDoorTarget == null)
        {
            return;
        }

        Transform endPos = FindDescendant(openDoorTarget, "endPos");
        if (endPos == null)
        {
            return;
        }

        Undo.RecordObject(endPos, "Set Open Door Move Target");
        Vector3 position = endPos.position;
        endPos.position = new Vector3(endRect.center.x, endRect.center.y, position.z);
        NormalizeOpenDoorEndTransform(endPos, endRect.size);
    }

    private void CompletePendingOpenDoorMoveEnd(Rect endRect)
    {
        if (m_PendingOpenDoorObject == null)
        {
            ClearPendingPairPlacement();
            return;
        }

        SetOpenDoorEndRect(m_PendingOpenDoorObject.transform, endRect);
        Selection.activeGameObject = m_PendingOpenDoorObject;
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        m_PendingOpenDoorObject = null;
        m_PendingOpenDoorRect = default;
    }

    private static void NormalizeOpenDoorEndTransform(Transform endPos, Vector2 worldSize)
    {
        Vector3 parentScale = endPos.parent != null ? endPos.parent.lossyScale : Vector3.one;
        float scaleX = Mathf.Approximately(parentScale.x, 0f) ? worldSize.x : worldSize.x / parentScale.x;
        float scaleY = Mathf.Approximately(parentScale.y, 0f) ? worldSize.y : worldSize.y / parentScale.y;
        endPos.localScale = new Vector3(scaleX, scaleY, 1f);

        foreach (Renderer renderer in endPos.GetComponentsInChildren<Renderer>(true))
        {
            renderer.enabled = false;
        }
    }

    private static Rect OffsetRect(Rect rect, Vector2 offset)
    {
        rect.position += offset;
        return rect;
    }

    private static void SetWorldRectTransform(Transform target, Rect worldRect, Transform parent)
    {
        Vector2 center = worldRect.center;
        Vector2 size = worldRect.size;
        target.position = new Vector3(center.x, center.y, 0f);

        Vector3 parentScale = parent != null ? parent.lossyScale : Vector3.one;
        float scaleX = Mathf.Approximately(parentScale.x, 0f) ? size.x : size.x / parentScale.x;
        float scaleY = Mathf.Approximately(parentScale.y, 0f) ? size.y : size.y / parentScale.y;
        target.localScale = new Vector3(scaleX, scaleY, 1f);
    }

    private static void FitRendererBoundsToRect(GameObject target, Rect worldRect)
    {
        if (target == null)
        {
            return;
        }

        Transform targetTransform = target.transform;
        float z = targetTransform.position.z;
        targetTransform.rotation = Quaternion.identity;
        targetTransform.localScale = Vector3.one;
        targetTransform.position = new Vector3(worldRect.center.x, worldRect.center.y, z);

        if (!TryGetRendererBounds(target, out Bounds bounds)
            || Mathf.Approximately(bounds.size.x, 0f)
            || Mathf.Approximately(bounds.size.y, 0f))
        {
            SetWorldRectTransform(targetTransform, worldRect, targetTransform.parent);
            return;
        }

        Vector3 scale = targetTransform.localScale;
        scale.x *= worldRect.width / bounds.size.x;
        scale.y *= worldRect.height / bounds.size.y;
        targetTransform.localScale = scale;
        AlignRendererBoundsCenter(target, worldRect.center);
    }

    private static bool TryGetRendererBounds(GameObject target, out Bounds bounds)
    {
        bounds = default;
        if (target == null)
        {
            return false;
        }

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || !renderer.enabled)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }

    private static void AlignRendererBoundsCenter(GameObject target, Vector2 desiredCenter)
    {
        if (target == null)
        {
            return;
        }

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        Vector3 offset = new Vector3(desiredCenter.x - bounds.center.x, desiredCenter.y - bounds.center.y, 0f);
        target.transform.position += offset;
    }

    private static void ConfigureSawBladeGridSize(GameObject sawObject, Rect bladeRect)
    {
        if (sawObject == null)
        {
            return;
        }

        Transform sawTransform = sawObject.transform;
        float z = sawTransform.position.z;
        sawTransform.rotation = Quaternion.identity;
        sawTransform.localScale = Vector3.one;
        sawTransform.position = new Vector3(bladeRect.center.x, bladeRect.center.y, z);

        Transform bladeVisual = sawTransform.Find("BladeVisual");
        if (bladeVisual != null)
        {
            bladeVisual.localPosition = Vector3.zero;
            bladeVisual.localRotation = Quaternion.identity;
        }

        if (!TryGetSawBladeCoreBounds(sawObject, out Bounds coreBounds)
            || Mathf.Approximately(coreBounds.size.x, 0f)
            || Mathf.Approximately(coreBounds.size.y, 0f))
        {
            FitRendererBoundsToRect(sawObject, bladeRect);
            return;
        }

        Vector3 scale = sawTransform.localScale;
        scale.x *= bladeRect.width / coreBounds.size.x;
        scale.y *= bladeRect.height / coreBounds.size.y;
        sawTransform.localScale = scale;
        AlignSawBladeCoreBoundsCenter(sawObject, bladeRect.center);
    }

    private static bool TryGetSawBladeCoreBounds(GameObject sawObject, out Bounds bounds)
    {
        bounds = default;
        if (sawObject == null)
        {
            return false;
        }

        Transform core = sawObject.transform.Find("BladeVisual/Core");
        if (core == null)
        {
            return false;
        }

        SpriteRenderer renderer = core.GetComponent<SpriteRenderer>();
        if (renderer == null || renderer.sprite == null || !renderer.enabled)
        {
            return false;
        }

        bounds = renderer.bounds;
        return true;
    }

    private static void AlignSawBladeCoreBoundsCenter(GameObject sawObject, Vector2 desiredCenter)
    {
        if (!TryGetSawBladeCoreBounds(sawObject, out Bounds bounds))
        {
            return;
        }

        Vector3 offset = new Vector3(desiredCenter.x - bounds.center.x, desiredCenter.y - bounds.center.y, 0f);
        sawObject.transform.position += offset;
    }

    private static void FitPlayerColliderToRect(GameObject target, Rect worldRect)
    {
        if (target == null)
        {
            return;
        }

        BoxCollider2D collider = target.GetComponent<BoxCollider2D>();
        if (collider == null
            || Mathf.Approximately(collider.size.x, 0f)
            || Mathf.Approximately(collider.size.y, 0f))
        {
            FitRendererBoundsToRect(target, worldRect);
            return;
        }

        Transform targetTransform = target.transform;
        float z = targetTransform.position.z;
        targetTransform.rotation = Quaternion.identity;
        targetTransform.localScale = new Vector3(
            worldRect.width / collider.size.x,
            worldRect.height / collider.size.y,
            targetTransform.localScale.z);

        Vector3 colliderCenterOffset = targetTransform.TransformVector(collider.offset);
        targetTransform.position = new Vector3(
            worldRect.center.x - colliderCenterOffset.x,
            worldRect.center.y - colliderCenterOffset.y,
            z);

        AlignPlayerGroundCheckToColliderBottom(target, collider);
    }

    private static void AlignPlayerGroundCheckToColliderBottom(GameObject target, BoxCollider2D collider)
    {
        MonoBehaviour playerMove = target != null ? target.GetComponent<PlayerMove>() : null;
        if (playerMove == null || collider == null)
        {
            return;
        }

        Vector2 localBottomCenter = collider.offset + Vector2.down * (collider.size.y * 0.5f);
        Vector3 checkWorldPosition = target.transform.TransformPoint(localBottomCenter);
        Vector3 checkOffset = checkWorldPosition - target.transform.position;

        SerializedObject serializedPlayerMove = new SerializedObject(playerMove);
        SerializedProperty checkPosProperty = serializedPlayerMove.FindProperty("m_CheckPos");
        if (checkPosProperty == null)
        {
            return;
        }

        checkPosProperty.vector3Value = checkOffset;
        serializedPlayerMove.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetSpriteRendererLocalSize(Transform target, Vector2 localSize)
    {
        SpriteRenderer renderer = target != null ? target.GetComponent<SpriteRenderer>() : null;
        if (renderer == null || renderer.sprite == null)
        {
            return;
        }

        Vector2 spriteSize = renderer.sprite.bounds.size;
        Vector3 scale = target.localScale;
        if (!Mathf.Approximately(spriteSize.x, 0f))
        {
            scale.x = localSize.x / spriteSize.x;
        }

        if (!Mathf.Approximately(spriteSize.y, 0f))
        {
            scale.y = localSize.y / spriteSize.y;
        }

        target.localScale = scale;
    }

    private static void SetSpriteRendererLocalWidth(Transform target, float localWidth)
    {
        SpriteRenderer renderer = target != null ? target.GetComponent<SpriteRenderer>() : null;
        if (renderer == null || renderer.sprite == null)
        {
            return;
        }

        float spriteWidth = renderer.sprite.bounds.size.x;
        if (Mathf.Approximately(spriteWidth, 0f))
        {
            return;
        }

        float uniformScale = localWidth / spriteWidth;
        target.localScale = new Vector3(uniformScale, uniformScale, target.localScale.z);
    }

    private GameObject EnsureGroundParent()
    {
        GameObject parent = GameObject.Find(GroundParentName);
        if (parent != null)
        {
            return parent;
        }

        parent = new GameObject(GroundParentName);
        Undo.RegisterCreatedObjectUndo(parent, "Create GroundParent");
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        return parent;
    }

    private string GetUniqueGroundName()
    {
        int index = 0;
        string candidate;
        do
        {
            candidate = index == 0 ? "Ground" : $"Ground_{index}";
            index++;
        } while (GameObject.Find(candidate) != null);

        return candidate;
    }

    private string GetUniqueTriangleGroundName()
    {
        int index = 0;
        string candidate;
        do
        {
            candidate = index == 0 ? "TriangleGround" : $"TriangleGround_{index}";
            index++;
        } while (GameObject.Find(candidate) != null);

        return candidate;
    }

    private Vector2 GetMouseWorldPosition(Vector2 mousePosition)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
        if (Mathf.Abs(ray.direction.z) < 0.0001f)
        {
            return ray.origin;
        }

        float distance = -ray.origin.z / ray.direction.z;
        Vector3 world = ray.GetPoint(distance);
        return new Vector2(world.x, world.y);
    }

    private void DrawRectOutline(Rect rect, Color outlineColor)
    {
        DrawRect(rect, Color.clear, outlineColor);
    }

    private void DrawRect(Rect rect, Color fillColor, Color outlineColor)
    {
        Vector3[] corners =
        {
            new Vector3(rect.xMin, rect.yMin, 0f),
            new Vector3(rect.xMin, rect.yMax, 0f),
            new Vector3(rect.xMax, rect.yMax, 0f),
            new Vector3(rect.xMax, rect.yMin, 0f)
        };

        Handles.DrawSolidRectangleWithOutline(corners, fillColor, outlineColor);
    }

    private Vector2 SnapToCellOrigin(Vector2 value)
    {
        return new Vector2(
            Mathf.Floor(value.x / m_GridSize) * m_GridSize,
            Mathf.Floor(value.y / m_GridSize) * m_GridSize);
    }

    private Vector2 SnapToGridPoint(Vector2 value)
    {
        return new Vector2(
            Mathf.Round(value.x / m_GridSize) * m_GridSize,
            Mathf.Round(value.y / m_GridSize) * m_GridSize);
    }

    private AttachSide GetPrefabAttachSide(Vector2 mouseWorld, Vector2 cellOrigin)
    {
        if (!ShouldUsePlacementAssist())
        {
            return AttachSide.Floor;
        }

        if (m_SelectedPrefabName == "HangLever")
        {
            return GetHangLeverAttachSide(cellOrigin);
        }

        return GetSwitchAttachSide(mouseWorld, cellOrigin);
    }

    private Quaternion GetPrefabPlacementRotation(Vector2 cellOrigin, AttachSide attachSide)
    {
        if (!ShouldUsePlacementAssist())
        {
            return Quaternion.identity;
        }

        if (m_SelectedPrefabName == "HangLever")
        {
            return Quaternion.identity;
        }

        return Quaternion.Euler(0f, 0f, attachSide switch
        {
            AttachSide.LeftWall => -90f,
            AttachSide.RightWall => 90f,
            AttachSide.Ceiling => 180f,
            _ => 0f
        });
    }

    private Vector2 GetPrefabPlacementPosition(Vector2 cellOrigin, AttachSide attachSide, Quaternion rotation)
    {
        if (m_PendingPairObject != null)
        {
            return cellOrigin + Vector2.one * (m_GridSize * 0.5f);
        }

        if (m_SelectedPrefabName == "HangLever")
        {
            return GetAttachedPlacementPosition(cellOrigin, GetPrefabSize(m_SelectedPrefabName), attachSide, rotation);
        }

        if (IsSwitchPrefab(m_SelectedPrefabName))
        {
            return GetAttachedPlacementPosition(cellOrigin, GetPrefabSize(m_SelectedPrefabName), attachSide, rotation);
        }

        if (m_SelectedPrefabName == "SawBlade")
        {
            return cellOrigin + GetPrefabSize(m_SelectedPrefabName) * 0.5f;
        }

        return cellOrigin + Vector2.one * (m_GridSize * 0.5f);
    }

    private Rect GetPrefabFootprintRect(Vector2 cellOrigin, Vector2 placementPosition, Quaternion rotation)
    {
        if (m_PendingPairObject != null)
        {
            return GetCenteredFootprint(placementPosition, Vector2.one * m_GridSize);
        }

        return m_SelectedPrefabName switch
        {
            "HangLever" => GetCenteredFootprint(placementPosition, GetRotatedFootprintSize(GetPrefabSize(m_SelectedPrefabName), rotation)),
            "SwitchObject" or "SwitchAndDoorObject" or "OpenDoorAndSwitchObject" => GetCenteredFootprint(placementPosition, GetRotatedFootprintSize(GetPrefabSize(m_SelectedPrefabName), rotation)),
            _ => GetCenteredFootprint(placementPosition, GetPrefabSize(m_SelectedPrefabName))
        };
    }

    private Vector2 GetAttachedPlacementPosition(Vector2 cellOrigin, Vector2 baseSize, AttachSide attachSide, Quaternion rotation)
    {
        Vector2 size = GetRotatedFootprintSize(baseSize, rotation);
        return attachSide switch
        {
            AttachSide.Floor => new Vector2(cellOrigin.x + size.x * 0.5f, cellOrigin.y + size.y * 0.5f),
            AttachSide.Ceiling => new Vector2(cellOrigin.x + size.x * 0.5f, cellOrigin.y + m_GridSize - size.y * 0.5f),
            AttachSide.LeftWall => new Vector2(cellOrigin.x + size.x * 0.5f, cellOrigin.y + size.y * 0.5f),
            AttachSide.RightWall => new Vector2(cellOrigin.x + m_GridSize - size.x * 0.5f, cellOrigin.y + size.y * 0.5f),
            _ => cellOrigin + size * 0.5f
        };
    }

    private static Vector2 GetRotatedFootprintSize(Vector2 size, Quaternion rotation)
    {
        float z = Mathf.Abs(Mathf.DeltaAngle(0f, rotation.eulerAngles.z));
        return Mathf.Approximately(z, 90f) ? new Vector2(size.y, size.x) : size;
    }

    private bool ShouldUsePlacementAssist()
    {
        if (m_Mode != ToolMode.Prefab || m_PendingPairObject != null)
        {
            return false;
        }

        return m_SelectedPrefabName == "HangLever" || IsSwitchPrefab(m_SelectedPrefabName);
    }

    private AttachSide GetSwitchAttachSide(Vector2 mouseWorld, Vector2 cellOrigin)
    {
        AttachSide bestSide = AttachSide.Floor;
        float bestDistance = float.PositiveInfinity;

        TryUseSwitchAttachSide(AttachSide.Floor, HasSolidNeighbor(cellOrigin + Vector2.down * m_GridSize), Mathf.Abs(mouseWorld.y - cellOrigin.y), ref bestSide, ref bestDistance);
        TryUseSwitchAttachSide(AttachSide.LeftWall, HasSolidNeighbor(cellOrigin + Vector2.left * m_GridSize), Mathf.Abs(mouseWorld.x - cellOrigin.x), ref bestSide, ref bestDistance);
        TryUseSwitchAttachSide(AttachSide.RightWall, HasSolidNeighbor(cellOrigin + Vector2.right * m_GridSize), Mathf.Abs(mouseWorld.x - (cellOrigin.x + m_GridSize)), ref bestSide, ref bestDistance);
        TryUseSwitchAttachSide(AttachSide.Ceiling, HasSolidNeighbor(cellOrigin + Vector2.up * m_GridSize), Mathf.Abs(mouseWorld.y - (cellOrigin.y + m_GridSize)), ref bestSide, ref bestDistance);

        return float.IsPositiveInfinity(bestDistance) ? AttachSide.Floor : bestSide;
    }

    private static void TryUseSwitchAttachSide(AttachSide side, bool hasSupport, float distance, ref AttachSide bestSide, ref float bestDistance)
    {
        if (!hasSupport || distance >= bestDistance)
        {
            return;
        }

        bestSide = side;
        bestDistance = distance;
    }

    private AttachSide GetHangLeverAttachSide(Vector2 cellOrigin)
    {
        Rect baseRect = new Rect(cellOrigin, GetPrefabSize("HangLever"));
        if (HasStageSurfaceBesideRect(baseRect, Vector2.left))
        {
            return AttachSide.LeftWall;
        }

        if (HasStageSurfaceBesideRect(baseRect, Vector2.right))
        {
            return AttachSide.RightWall;
        }

        if (HasStageSurfaceBesideRect(baseRect, Vector2.down))
        {
            return AttachSide.Floor;
        }

        if (HasStageSurfaceBesideRect(baseRect, Vector2.up))
        {
            return AttachSide.Ceiling;
        }

        return AttachSide.Floor;
    }

    private bool HasStageSurfaceBesideRect(Rect rect, Vector2 direction)
    {
        float probeThickness = Mathf.Max(m_GridSize * 0.1f, 0.05f);
        Vector2 center;
        Vector2 size;

        if (direction == Vector2.left)
        {
            center = new Vector2(rect.xMin - probeThickness * 0.5f, rect.center.y);
            size = new Vector2(probeThickness, Mathf.Max(m_GridSize * 0.5f, rect.height * 0.9f));
        }
        else if (direction == Vector2.right)
        {
            center = new Vector2(rect.xMax + probeThickness * 0.5f, rect.center.y);
            size = new Vector2(probeThickness, Mathf.Max(m_GridSize * 0.5f, rect.height * 0.9f));
        }
        else if (direction == Vector2.down)
        {
            center = new Vector2(rect.center.x, rect.yMin - probeThickness * 0.5f);
            size = new Vector2(Mathf.Max(m_GridSize * 0.5f, rect.width * 0.9f), probeThickness);
        }
        else
        {
            center = new Vector2(rect.center.x, rect.yMax + probeThickness * 0.5f);
            size = new Vector2(Mathf.Max(m_GridSize * 0.5f, rect.width * 0.9f), probeThickness);
        }

        Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, 0f);
        foreach (Collider2D hit in hits)
        {
            if (hit == null || hit.isTrigger || !hit.enabled)
            {
                continue;
            }

            GameObject hitObject = hit.gameObject;
            if (hitObject == null || hitObject.hideFlags != HideFlags.None)
            {
                continue;
            }

            if (IsStageSurface(hitObject))
            {
                return true;
            }
        }

        return false;
    }

    private bool HasSolidNeighbor(Vector2 neighborCellOrigin)
    {
        Vector2 center = neighborCellOrigin + Vector2.one * (m_GridSize * 0.5f);
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, Vector2.one * (m_GridSize * 0.75f), 0f);
        foreach (Collider2D hit in hits)
        {
            if (hit == null || hit.isTrigger || !hit.enabled)
            {
                continue;
            }

            GameObject hitObject = hit.gameObject;
            if (hitObject == null || hitObject.hideFlags != HideFlags.None)
            {
                continue;
            }

            if (IsStageSurface(hitObject))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsStageSurface(GameObject sceneObject)
    {
        int floorLayer = LayerMask.NameToLayer("Floor");
        if (floorLayer >= 0 && sceneObject.layer == floorLayer)
        {
            return true;
        }

        string objectName = sceneObject.name;
        return objectName.StartsWith("Floor")
            || objectName.StartsWith("Wall")
            || objectName.StartsWith("Ground")
            || sceneObject.transform.parent != null && sceneObject.transform.parent.name == GroundParentName;
    }

    private static Rect GetCenteredFootprint(Vector2 center, Vector2 size)
    {
        return new Rect(center - size * 0.5f, size);
    }

    private string GetModeStatus()
    {
        if (m_Mode == ToolMode.Prefab && m_PendingOpenDoorObject != null)
        {
            return "Place Move Target";
        }

        if (m_Mode == ToolMode.Prefab && m_PendingPairObject != null)
        {
            return m_IsWaitingForPairMoveEnd ? "Place Move Target" : "Place Pair Target";
        }

        return m_Mode switch
        {
            ToolMode.Ground => "Ground Tool",
            ToolMode.TriangleGround => "Triangle Ground",
            ToolMode.CameraBounds => "Camera Bounds",
            ToolMode.Prefab => IsAreaPrefab(m_SelectedPrefabName)
                ? $"{m_SelectedPrefabName} Area"
                : IsSequentialPairPrefab(m_SelectedPrefabName)
                    ? $"Place {m_SelectedPrefabName} Pair"
                    : $"Place {m_SelectedPrefabName}",
            ToolMode.Erase => "Erase Tool",
            _ => "Tool Off"
        };
    }

    private string GetSceneHint()
    {
        if (m_Mode == ToolMode.Prefab && m_PendingOpenDoorObject != null)
        {
            return "OpenDoor Move Target";
        }

        if (m_Mode == ToolMode.Prefab && m_PendingPairObject != null)
        {
            return m_IsWaitingForPairMoveEnd
                ? $"{m_SelectedPrefabName} Move Target"
                : $"{m_SelectedPrefabName} Target";
        }

        return m_Mode switch
        {
            ToolMode.Ground => "Ground",
            ToolMode.TriangleGround => $"Triangle {Mathf.Min(m_TrianglePoints.Count + 1, 3)}/3",
            ToolMode.CameraBounds => "Drag Camera Bounds",
            ToolMode.Prefab => IsAreaPrefab(m_SelectedPrefabName)
                ? $"{m_SelectedPrefabName} Area"
                : IsSequentialPairPrefab(m_SelectedPrefabName)
                    ? $"{m_SelectedPrefabName} Pair"
                    : m_SelectedPrefabName,
            ToolMode.Erase => "Erase",
            _ => string.Empty
        };
    }

    private static Rect GetRect(Vector2 start, Vector2 end)
    {
        Vector2 min = Vector2.Min(start, end);
        Vector2 max = Vector2.Max(start, end);
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    private Rect GetCellSelectionRect(Vector2 startCellOrigin, Vector2 endCellOrigin)
    {
        Vector2 min = Vector2.Min(startCellOrigin, endCellOrigin);
        Vector2 max = Vector2.Max(startCellOrigin, endCellOrigin) + Vector2.one * m_GridSize;
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    private Rect GetToolRect(Vector2 startCellOrigin, Vector2 endCellOrigin, Vector2 clickSize)
    {
        if (startCellOrigin == endCellOrigin)
        {
            return new Rect(startCellOrigin, SanitizeSize(clickSize));
        }

        return GetCellSelectionRect(startCellOrigin, endCellOrigin);
    }

    private static bool IsAreaPrefab(string prefabName)
    {
        return prefabName == "LowGravity"
            || prefabName == "AccelerationArea"
            || prefabName == "DoorObject"
            || prefabName == "OpenDoor";
    }

    private static bool IsSwitchPrefab(string prefabName)
    {
        return prefabName == "SwitchObject"
            || prefabName == "SwitchAndDoorObject"
            || prefabName == "OpenDoorAndSwitchObject";
    }

    private static bool IsSequentialPairPrefab(string prefabName)
    {
        return prefabName == "SwitchAndDoorObject"
            || prefabName == "OpenDoorAndSwitchObject"
            || prefabName == "HangLever";
    }

    private static Transform FindPairControlPart(Transform root, string prefabName)
    {
        if (prefabName == "HangLever")
        {
            return root;
        }

        return FindDescendant(root, "SwitchObject");
    }

    private static Transform FindDescendant(Transform root, string objectName)
    {
        if (root == null)
        {
            return null;
        }

        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == objectName)
            {
                return child;
            }
        }

        return null;
    }

    private static Transform FindPairDoorPart(Transform root, string prefabName)
    {
        if (prefabName == "SwitchAndDoorObject")
        {
            return FindDescendant(root, "DoorObject");
        }

        if (prefabName == "OpenDoorAndSwitchObject")
        {
            Transform openDoor = FindDescendant(root, "DoorOnOpenbOject");
            return openDoor != null ? openDoor : FindDescendant(root, "OpenDoor");
        }

        if (prefabName == "HangLever")
        {
            return FindDescendant(root, "LeverBlocker");
        }

        return null;
    }

    private static Color GetAreaPreviewColor(string prefabName)
    {
        return prefabName switch
        {
            "LowGravity" => new Color(0.35f, 0.75f, 1f, 0.95f),
            "AccelerationArea" => new Color(1f, 0.55f, 0.25f, 0.95f),
            "DoorObject" => new Color(0.75f, 0.45f, 1f, 0.95f),
            "OpenDoor" => new Color(0.55f, 0.85f, 1f, 0.95f),
            _ => new Color(0.2f, 0.8f, 1f, 0.95f)
        };
    }

    private readonly struct PaletteItem
    {
        public readonly string Label;
        public readonly string PrefabName;
        public readonly BuilderTab Tab;

        public PaletteItem(string label, string prefabName, BuilderTab tab)
        {
            Label = label;
            PrefabName = prefabName;
            Tab = tab;
        }
    }
}
#endif
