#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

/*  * シーンデータにビルド設定を同期するボタンを追加するエディタ拡張
 */

// SceneDataのインスペクターに「Sync BuildSettings」ボタンを追加
[CustomEditor(typeof(SceneData))]

public class SceneDataEditor : Editor
{
    // ====================================================
    // メンバー変数
    // ====================================================
    // SceneConfig をキャッシュ
    private SceneConfig config;


    // ====================================================
    // SceneConfig をロード
    // ====================================================
    private void OnEnable()
    {
        config = Resources.Load<SceneConfig>("SceneConfig");
    }

    // ====================================================
    // インスペクターGUIを拡張
    // ====================================================
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        // BuildMode を表示
        ChangBuildMode();

        // ビルド設定同期関数
        SyncBuildSettings();
    }

    // ====================================================
    // BuildMode を表示・切り替え 関数
    // ====================================================
    public void ChangBuildMode()
    {
        if (config == null)
        {
            EditorGUILayout.HelpBox("SceneConfig が見つかりません", MessageType.Error);
            return;
        }

        // BuildMode を表示
        EditorGUILayout.LabelField("ビルドモード", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            $"現在のBuildMode: {config.GetBuildMode()}",
            MessageType.Info);

        // BuildMode を切り替えるためのボタンを追加
        GUILayout.BeginHorizontal();

        // DEBUG と RELEASE の切り替えボタン
        if (GUILayout.Button("DEBUG"))
        {
            config.SetBuildMode(BuildMode.DEBUG);
        }
        if (GUILayout.Button("RELEASE"))
        {
            config.SetBuildMode(BuildMode.RELEASE);
        }

        GUILayout.EndHorizontal();

        // スペースを追加
        GUILayout.Space(10);
    }

    // ====================================================
    // シーンデータのビルド設定を同期する関数
    // ====================================================
    public void SyncBuildSettings()
    {
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
