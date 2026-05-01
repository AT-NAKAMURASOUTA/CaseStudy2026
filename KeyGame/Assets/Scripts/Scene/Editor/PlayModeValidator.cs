#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/* Play時にSceneDataのチェックを行うエディタースクリプト
 * 
 * 止める条件
 * ・SceneDataに重複がある場合
*/

[InitializeOnLoad]
public static class PlayModeValidator
{
    // ====================================================
    // イベントに登録
    // ====================================================
    static PlayModeValidator()
    {
        // PlayModeStateChangeイベントに関数登録
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    // ====================================================
    // PlayModeが変わるときに呼ばれる関数
    // ====================================================
    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        // Playモードに入るときのみチェックを行う
        if (state != PlayModeStateChange.ExitingEditMode) return;

        // SceneDataを取得
        SceneConfig sceneConfig = Resources.Load<SceneConfig>("SceneConfig");
        if (sceneConfig == null) 
        {
            Debug.Log("SceneConfig が見つかりません");
            EditorApplication.isPlaying = false;
            return;
        }

        SceneData sceneData = sceneConfig.GetSceneData();
        if (sceneData == null) 
        {
            Debug.Log("SceneData が nullです。");
            EditorApplication.isPlaying = false;
            return;
        }

        // 重複がある場合はエラーログを出してPlayを停止
        if (DebugCheck(sceneData))
        {
            EditorApplication.isPlaying = false;
        }
    }

    // ====================================================
    // チェック関数
    // ====================================================
    private static bool DebugCheck(SceneData sceneData)
    {
        HashSet<SCENETYPE> used = new HashSet<SCENETYPE>();

        foreach (var info in sceneData.sceneInfos)
        {
            // すでに使用されているシーンタイプがあれば重複とみなす
            if (!used.Add(info.sceneType))
            {
                Debug.LogError($"SCENETYPE に {info.sceneType} が重複しています。 Playを停止しました。");
                return true;
            }
        }

        return false;
    }
}
#endif
