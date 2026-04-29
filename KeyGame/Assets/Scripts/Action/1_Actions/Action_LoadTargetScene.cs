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


    // ===========================================
    // 特定のシーンに移行するアクション処理
    // ===========================================
    public override UniTask Execute(CancellationToken token)
    {
        // シーン遷移処理
        SceneTransitionManager.GetInstance().SceneTransition(m_TargetScene);
        return UniTask.CompletedTask;
    }

}
