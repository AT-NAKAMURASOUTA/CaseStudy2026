using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

/*  * ゴール処理を呼び出すマネージャークラス
 *  * ゴール処理をどのようにするのか決まっていないため、配列順にゴール処理を呼び出すようにしている
 */


public sealed class GoalManager : MonoBehaviour
{
    // ------------------------------------------
    // メンバー変数
    // ------------------------------------------
    // ゴール処理のリスト
    [SerializeField] private List<BaseGoalAction> m_GoalActions;

    // ゴールマネージャーのインスタンス
    private static GoalManager m_Instance;
    // キャンセルトークン
    private CancellationTokenSource m_Cts;
    // ゴールフラグ
    private bool m_IsGoal = false;


    // ==============================================
    // 初期化
    // ==============================================
    void Awake()
    {
        // シングルトン
        if (m_Instance == null)
        {
            // インスタンスを設定
            m_Instance = this;
        }
        else
        {
            // オブジェクトを削除
            Destroy(gameObject);
            Debug.LogError("GoalManager のインスタンスは既に存在しています。");
        }
    }


    // ==============================================
    // ゴール処理呼び出し
    // ==============================================
    public void ExecuteGoal()
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
        GoalActionCall(m_Cts.Token).Forget();
    }


    // ==============================================
    // ゴール処理の呼び出し
    // ==============================================
    private async UniTaskVoid GoalActionCall(CancellationToken token)
    {
        // ゴール処理を順番に呼び出す
        foreach (var action in m_GoalActions)
        {
            // キャンセルトークンがキャンセルされている場合は処理を中断する
            if (token.IsCancellationRequested)
            {
                Debug.Log("ゴール処理がキャンセルされました。");
                return;
            }
            // ゴール処理を呼び出す
            await action.Execute(token);
        }
    }


    // ==============================================
    // インスタンスのゲッター
    // ==============================================
    public static GoalManager GetGoalManager()
    {
        if (m_Instance == null)
        {
            Debug.LogError("GoalManager がシーンに存在しません");
            return null;
        }

        return m_Instance;
    }

    // ==============================================
    // ゴールフラグのゲッター
    // ==============================================
    public bool IsGoal()
    {
        return m_IsGoal;
    }

}
