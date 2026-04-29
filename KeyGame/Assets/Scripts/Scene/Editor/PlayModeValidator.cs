#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/* Play時にSceneDataのチェックを行うエディタースクリプト
 * 
 * 止める条件
 * ・SceneDataに重複がある場合
 * ・SceneDataにDEBUGがある場合
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
        SceneData sceneData = Resources.Load<SceneData>("SceneData");

        if (sceneData == null) return;

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
            // DEBUGは禁止
            if (info.sceneType == SCENETYPE.DEBUG)
            {
                Debug.LogError("SCENETYPE に DEBUG が設定されています。 Playを停止しました。");
                return true;
            }

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
