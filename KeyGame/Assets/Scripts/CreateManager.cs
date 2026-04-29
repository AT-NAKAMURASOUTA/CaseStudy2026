using UnityEngine;

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
        SceneData sceneData = Resources.Load<SceneData>("SceneData");

        // マネージャー初期化
        SceneTransitionManager.GetInstance().Init(sceneData);

        Debug.Log("ゲーム開始時に各マネージャー呼び出し");
    }
}
