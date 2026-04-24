using Cysharp.Threading.Tasks;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;

/*  * アクション処理クラス
 *  * 次のシーンに進む
 */


public class Action_NextScene : BaseAction
{
    // ===========================================
    // メンバー変数
    // ===========================================
    private SceneManagement m_SceneManager;


    // ===========================================
    // 初期化
    // ===========================================
    private void Start()
    {
        // シーンマネージャーの取得
        m_SceneManager = SceneManagement.GetInstance();
    }


    // ===========================================
    // 派生クラスのゴール処理
    // ===========================================
    public override UniTask Execute(CancellationToken token)
    {
        // 次のシーンに遷移
        m_SceneManager.NextScene();

        return UniTask.CompletedTask;
    }
}
