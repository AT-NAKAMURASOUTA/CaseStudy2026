using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using System.Collections.Generic;


/*  * アクション処理をグループにまとめるクラス
 *  * まとめたアクション処理を並列で実行する
 */

public class GroupGoalActions_Parallel : BaseAction
{
    // ===========================================
    // メンバー変数
    // ===========================================
    // アクション処理のグループ
    [SerializeField] private List<BaseAction> m_GoalActions;


    // ===========================================
    // 派生クラスのアクション処理
    // ===========================================
    public override async UniTask Execute(CancellationToken token)
    {
        // アクション処理を並列で実行
        var tasks = new List<UniTask>();

        foreach (var action in m_GoalActions)
        {
            // 各アクション処理を実行し、そのタスクをリストに追加
            tasks.Add(action.Execute(token));
        }

        // 全てのアクション処理が完了するまで待機
        await UniTask.WhenAll(tasks);
    }
}
