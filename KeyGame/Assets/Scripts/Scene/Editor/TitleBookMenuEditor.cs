using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TitleBookMenu))]
public sealed class TitleBookMenuEditor : Editor
{
    // ツールバーの順番はOnSceneGUIと合わせる0が本全体1がタイトル2-5 が各ボタン。
    private static readonly string[] s_HandleLabels = { "Book", "Title", "Start", "Load", "Config", "Exit" };
    private static int s_SelectedHandle;

    private SerializedProperty m_BookPositionProperty;
    private SerializedProperty m_BookSizeProperty;
    private SerializedProperty m_TitlePositionProperty;
    private SerializedProperty m_MenuPositionsProperty;

    private void OnEnable()
    {
        m_BookPositionProperty = serializedObject.FindProperty("m_BookPosition");
        m_BookSizeProperty = serializedObject.FindProperty("m_BookSize");
        m_TitlePositionProperty = serializedObject.FindProperty("m_TitlePosition");
        m_MenuPositionsProperty = serializedObject.FindProperty("m_MenuPositions");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.LabelField("Scene View Move Handle", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        s_SelectedHandle = GUILayout.Toolbar(s_SelectedHandle, s_HandleLabels);
        if (EditorGUI.EndChangeCheck())
        {
            SceneView.RepaintAll();
        }
        EditorGUILayout.Space(6f);
        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();
        bool propertyChanged = EditorGUI.EndChangeCheck();
        serializedObject.ApplyModifiedProperties();
        if (propertyChanged && target is TitleBookMenu menu)
        {
            menu.RefreshEditorPreview();
            SceneView.RepaintAll();
        }
    }

    private void OnSceneGUI()
    {
        if (m_BookPositionProperty == null || target is not TitleBookMenu menu)
        {
            return;
        }

        RectTransform rectTransform = menu.transform as RectTransform;
        if (rectTransform == null)
        {
            return;
        }

        // シーンビューが見やすいようにハンドルを一つだけ表示する
        serializedObject.Update();

        if (s_SelectedHandle == 0)
        {
            Vector2 bookPosition = m_BookPositionProperty.vector2Value;
            Vector3 worldPosition = rectTransform.TransformPoint(new Vector3(bookPosition.x, bookPosition.y, 0f));
            DrawRootHandle(menu, rectTransform, worldPosition);
            return;
        }

        Transform coverUiRoot = menu.transform.Find("TitleBookParts/BookCoverPivot/CoverUiRoot");
        if (coverUiRoot != null)
        {
            float bookScale = Mathf.Max(1f, m_BookSizeProperty.floatValue) / 700f;
            if (s_SelectedHandle == 1)
            {
                DrawCoverHandle(menu, coverUiRoot, m_TitlePositionProperty, bookScale, "Title");
                return;
            }

            int menuIndex = s_SelectedHandle - 2;
            if (m_MenuPositionsProperty != null && m_MenuPositionsProperty.isArray && menuIndex >= 0 && menuIndex < m_MenuPositionsProperty.arraySize)
            {
                DrawCoverHandle(menu, coverUiRoot, m_MenuPositionsProperty.GetArrayElementAtIndex(menuIndex), bookScale, s_HandleLabels[s_SelectedHandle]);
            }
        }
    }

    private void DrawRootHandle(TitleBookMenu menu, RectTransform rectTransform, Vector3 worldPosition)
    {
        EditorGUI.BeginChangeCheck();
        Handles.Label(worldPosition + Vector3.up * 35f, "Book");
        Vector3 newWorldPosition = Handles.PositionHandle(worldPosition, Quaternion.identity);
        if (!EditorGUI.EndChangeCheck())
        {
            return;
        }

        Undo.RecordObject(menu, "Move Title Book");
        Vector3 localPosition = rectTransform.InverseTransformPoint(newWorldPosition);
        m_BookPositionProperty.vector2Value = new Vector2(localPosition.x, localPosition.y);
        serializedObject.ApplyModifiedProperties();
        menu.RefreshEditorPreview();
        EditorUtility.SetDirty(menu);
        SceneView.RepaintAll();
    }

    private void DrawCoverHandle(TitleBookMenu menu, Transform coverUiRoot, SerializedProperty positionProperty, float bookScale, string label)
    {
        if (positionProperty == null)
        {
            return;
        }

        // UIの位置は基準サイズの本の座標で保存し、プレビュー時だけ現在の本のサイズに合わせる
        Vector2 position = positionProperty.vector2Value;
        Vector3 worldPosition = coverUiRoot.TransformPoint(new Vector3(position.x * bookScale, position.y * bookScale, 0f));

        EditorGUI.BeginChangeCheck();
        Handles.Label(worldPosition + Vector3.up * 22f, label);
        Vector3 newWorldPosition = Handles.PositionHandle(worldPosition, Quaternion.identity);
        if (!EditorGUI.EndChangeCheck())
        {
            return;
        }

        Undo.RecordObject(menu, $"Move Title {label}");
        Vector3 localPosition = coverUiRoot.InverseTransformPoint(newWorldPosition);
        positionProperty.vector2Value = new Vector2(localPosition.x / bookScale, localPosition.y / bookScale);
        serializedObject.ApplyModifiedProperties();
        menu.RefreshEditorPreview();
        EditorUtility.SetDirty(menu);
        SceneView.RepaintAll();
    }
}
