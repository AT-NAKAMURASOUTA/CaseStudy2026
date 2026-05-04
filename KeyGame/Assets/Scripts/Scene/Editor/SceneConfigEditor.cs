#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

/*  * シーン設定にビルド設定を同期するボタンを追加するエディタ拡張
 */

[CustomEditor(typeof(SceneConfig))]

public class SceneConfigEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        // スペースを追加
        EditorGUILayout.Space(10);

        // タイトルを追加
        EditorGUILayout.LabelField("ビルド設定同期", EditorStyles.boldLabel);

        // Sync BuildSettings ボタンを追加
        if (GUILayout.Button("ビルド設定を同期させる"))
        {
            SceneBuildSync.Sync();
        }

        // 注釈
        EditorGUILayout.HelpBox(
            "※ タイトルシーンが必ずBuildSettingsの先頭になります。\n　タイトルが設定されていない場合、エラーが発生します。",
            MessageType.Warning);
    }
}

#endif
