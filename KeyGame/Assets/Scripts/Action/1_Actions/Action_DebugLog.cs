using UnityEngine;
using System.Threading;
using Cysharp.Threading.Tasks;


/*  * アクション処理クラス
 *  * デバッグ用のログを表示するアクション
 */


public class Action_DebugLog : BaseAction
{
    // ===========================================
    // デバッグアクション処理
    // ===========================================
    public override UniTask Execute(CancellationToken token)
    {
        Debug.Log("アクション処理が実行されました。");
        return UniTask.CompletedTask;
    }
}
