#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/*  * シーンを自動的にBuildSettingsに追加するエディタ拡張
 */

public static class SceneBuildSync
{
    [MenuItem("Tools/Scene/Sync BuildSettings")]
    public static void Sync()
    {
        // SceneConfig を取得
        var config = Resources.Load<SceneConfig>("SceneConfig");

        // エラーチェック
        if (config == null)
        {
            Debug.LogError("SceneConfig が見つかりません");
            return;
        }
        var sceneData = config.GetSceneData();
        if (sceneData == null)
        {
            Debug.LogError("SceneData が nullです。");
            return;
        }

        // 重複や未設定のチェック
        if (!Validate(sceneData))
        {
            Debug.LogError("BuildSettingsの更新に失敗しました。エラーを確認してください。");
            return;
        }

        // Titleシーンを先頭に、重複を除いてBuildSettingsSceneの配列を作成
        var buildScenes = sceneData.sceneInfos
            .OrderBy(x => x.sceneType == SCENETYPE.TITLE ? 0 : 1)
            .Select(x => AssetDatabase.GetAssetPath(x.scene))
            .Distinct()
            .Select(p => new EditorBuildSettingsScene(p, true))
            .ToArray();

        // BuildSettingsのシーンを上書き
        EditorBuildSettings.scenes = buildScenes;

        // ログ出力
        Debug.Log($"現在のBuildMode: {config.GetBuildMode()}\n" +
            $"BuildSettings 更新完了: {buildScenes.Length}シーン");
    }

    // ====================================================
    // 重複や未設定のチェック
    // ====================================================
    private static bool Validate(SceneData data)
    {
        // SceneInfosがnullかチェック
        if (data.sceneInfos == null)
        {
            Debug.LogError("SceneData が不正です");
            return false;
        }

        // nullチェック
        for (int i = 0; i < data.sceneInfos.Length; i++)
        {
            var x = data.sceneInfos[i];

            if (x == null)
            {
                Debug.LogError($"SceneInfos[{i}] が nullです");
                return false;
            }
            if (x.scene == null)
            {
                Debug.LogError($"SceneInfos[{i}] の Scene が未設定です");
                return false;
            }
        }

        // Titleシーンがあるかチェック
        if (!data.sceneInfos.Any(x => x.sceneType == SCENETYPE.TITLE))
        {
            Debug.LogError("Titleシーンが設定されていません");
            return false;
        }

        // SceneType重複チェック
        var seen = new HashSet<SCENETYPE>();
        bool hasError = false;

        // チェック
        for (int i = 0; i < data.sceneInfos.Length; i++)
        {
            var x = data.sceneInfos[i];

            // すでに入っているかチェック
            if (!seen.Add(x.sceneType))
            {
                Debug.LogError($"SceneType 重複: {x.sceneType} (index: {i})");
                hasError = true;
            }
        }
        if (hasError) return false;

        return true;
    }
}
#endif
