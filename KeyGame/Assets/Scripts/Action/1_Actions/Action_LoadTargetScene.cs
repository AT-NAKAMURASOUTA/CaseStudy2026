using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;


/*  * アクション処理クラス
 *  * 特定のシーンに移行するアクション処理
 */
public class Action_LoadTargetScene : BaseAction
{
    // ===========================================
    // メンバー変数
    // ===========================================
    // シーン名
    [SerializeField] private SCENETYPE m_TargetScene;
    [SerializeField] private bool m_UseRestartStage;
    [SerializeField] private bool m_QuitGame;


    // ===========================================
    // 特定のシーンに移行するアクション処理
    // ===========================================
    public override UniTask Execute(CancellationToken token)
    {
        if (m_QuitGame)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
            return UniTask.CompletedTask;
        }

        // シーン遷移処理
        if (m_UseRestartStage)
        {
            // リターン
            SceneTransitionManager.GetInstance().ReturenScene();
        }
        else
        {
            // シーン遷移
            SceneTransitionManager.GetInstance().SceneTransition(m_TargetScene);
        }

        return UniTask.CompletedTask;
    }

    // ===========================================
    // 初期処理
    // ===========================================
    public void Init(
        SCENETYPE sceneType,
        bool useRestart = false,
        bool quitGame = false)
    {
        m_TargetScene = sceneType;
        m_UseRestartStage = useRestart;
        m_QuitGame = quitGame;
    }
}
