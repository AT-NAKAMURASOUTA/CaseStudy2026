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
        SceneManagement.GetInstance();

        Debug.Log("ゲーム開始時に各マネージャー呼び出し");
    }
}
