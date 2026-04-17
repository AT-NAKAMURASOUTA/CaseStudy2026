using UnityEngine;
using System.Threading;
using Cysharp.Threading.Tasks;


/*  * ゴール処理クラス
 *  * ゴール時に、ログを表示させる
 */


public class GoalAction_DebugLog : BaseGoalAction
{
    // ===========================================
    // 派生クラスのゴール処理
    // ===========================================
    public override UniTask Execute(CancellationToken token)
    {
        Debug.Log("ゴール処理が実行されました。");
        return UniTask.CompletedTask;
    }
}
