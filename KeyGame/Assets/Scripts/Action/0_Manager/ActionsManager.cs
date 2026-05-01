using UnityEngine;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

/*  * アクション処理を呼び出すマネージャークラス
 *  * アクション処理を配列順に呼び出すようにしている
 */


public sealed class ActionsManager : MonoBehaviour
{
    // ------------------------------------------
    // メンバー変数
    // ------------------------------------------
    // アクション処理のリスト
    [SerializeField] private List<BaseAction> m_GoalActions;
    // キャンセルトークン
    private CancellationTokenSource m_Cts;
    // アクション実行フラグ
    private bool m_IsGoal = false;


    // ==============================================
    // アクション処理呼び出し
    // ==============================================
    public void ExecuteAction()
    {
        // ゴール処理が既に実行されている場合は何もしない
        if (m_IsGoal)
        {
            Debug.Log("ゴール処理が既に実行されています。");
            return;
        }

        // ゴールフラグを立てる
        m_IsGoal = true;
        Debug.Log("ゴールに到達しました！");

        // キャンセルトークンを生成
        m_Cts = new CancellationTokenSource();

        // ゴール処理の呼び出し
        ActionCall(m_Cts.Token).Forget();
    }


    // ==============================================
    // ゴール処理の呼び出し
    // ==============================================
    private async UniTaskVoid ActionCall(CancellationToken token)
    {
        // アクション処理を順番に呼び出す
        foreach (var action in m_GoalActions)
        {
            // キャンセルトークンがキャンセルされている場合は処理を中断する
            if (token.IsCancellationRequested)
            {
                Debug.Log("アクションがキャンセルされました。");
                return;
            }
            // ゴール処理を呼び出す
            await action.Execute(token);
        }
    }


    // ==============================================
    // ゴールフラグのゲッター
    // ==============================================
    public bool IsGoal()
    {
        return m_IsGoal;
    }

}
