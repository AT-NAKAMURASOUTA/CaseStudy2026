#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Linq;

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

        // SceneAssetからパスを取得して重複を排除
        var paths = sceneData.sceneInfos
            .Where(x => x.scene != null)
            .Select(x => AssetDatabase.GetAssetPath(x.scene))
            .Distinct()
            .ToArray();

        // 重複や未設定のチェック
        Validate(sceneData);

        // BuildSettingsのシーンを更新
        var buildScenes = paths
            .Select(p => new EditorBuildSettingsScene(p, true))
            .ToArray();

        // BuildSettingsのシーンを上書き
        EditorBuildSettings.scenes = buildScenes;

        Debug.Log($"BuildSettings 更新完了: {buildScenes.Length}シーン");
    }

    // ====================================================
    // 重複や未設定のチェック
    // ====================================================
    private static void Validate(SceneData data)
    {
        // SceneAsset未設定チェック
        if (data.sceneInfos.Any(x => x == null || x.scene == null))
        {
            Debug.LogError("SceneAsset 未設定があります");
        }

        // SceneType重複チェック
        var duplicateTypes = data.sceneInfos
            .GroupBy(x => x.sceneType)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);

        // 重複があればエラーログを出す
        foreach (var type in duplicateTypes)
        {
            Debug.LogError($"SceneType 重複: {type}");
        }
    }
}
#endif
