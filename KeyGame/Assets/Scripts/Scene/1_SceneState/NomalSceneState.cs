using UnityEditor;
using UnityEngine;


/* * @file ノーマルシーンの状態
 * @brief ノーマルシーンの状態クラス
 * @memo これをScriptableObjectとして作成して、シーンの状態として使用してください。
*/


[CreateAssetMenu(menuName = "SceneState/NomalSceneState")]
public class NomalSceneState : BaseSceneOS
{
    [Header("シーン名")]
    [SerializeField] private string m_SceneName = "SceneNmae";

    // シーンの名前のゲッター
    public override string GetSceneName() { return m_SceneName; }

    // 開始処理
    public override void Enter(ISceneData _data)
    {
        Debug.Log(m_SceneName + " 開始");
    }

    // 終了処理
    public override void Exit()
    {
        Debug.Log(m_SceneName + " 終了");
    }
}
