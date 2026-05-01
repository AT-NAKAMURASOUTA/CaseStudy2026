using UnityEditor;
using UnityEngine;
using static UnityEngine.Rendering.STP;

/* @file シーン最初に、マネージャーを生成しておく
 * @brief 各マネージャー生成
 */


public class CreateManager
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void CreateResourceManager()
    {
        // マネージャー作成
        SceneTransitionManager.GetInstance();

        // リソースから初期化データを取得
        SceneConfig sceneConfig = Resources.Load<SceneConfig>("SceneConfig");

        // エラーチェック
        if (sceneConfig == null)
        {
            Debug.LogError("SceneConfig が見つかりません");
            EditorApplication.isPlaying = false;
            return;
        }

        // マネージャー初期化
        SceneTransitionManager.GetInstance().Init(sceneConfig.GetSceneData());

        Debug.Log("ゲーム開始時に各マネージャー呼び出し");
    }
}
