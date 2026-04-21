using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using System.Collections.Generic;


/*  * ゴール処理をグループにまとめるクラス
 *  * まとめたゴール処理を並列で実行する
 */

public class GroupGoalActions_Parallel : BaseGoalAction
{
    // ===========================================
    // メンバー変数
    // ===========================================
    // ゴール処理のグループ
    [SerializeField] private List<BaseGoalAction> m_GoalActions;


    // ===========================================
    // 派生クラスのゴール処理
    // ===========================================
    public override async UniTask Execute(CancellationToken token)
    {
        // ゴール処理を並列で実行
        var tasks = new List<UniTask>();

        foreach (var action in m_GoalActions)
        {
            // 各ゴール処理を実行し、そのタスクをリストに追加
            tasks.Add(action.Execute(token));
        }

        // 全てのゴール処理が完了するまで待機
        await UniTask.WhenAll(tasks);
    }
}
