using UnityEngine;


/*  * ゴールかどうかを判定するオブジェクトにアタッチするクラス
 */


// ==============================================
// 必須コンポーネント定義
// ==============================================
[RequireComponent(typeof(BoxCollider2D))]


public class GoalCheck_Collision : MonoBehaviour
{
    // ==============================================
    // メンバー変数
    // ==============================================
    // ゴールと判定するタグ
    [SerializeField] private string m_GoalTag = "Player";
    // ゴールマネージャー
    private GoalManager m_GoalManager;


    // ==============================================
    // 初期化処理
    // ==============================================
    void Start()
    {
        // ゴールマネージャーを取得
        m_GoalManager = GoalManager.GetGoalManager();

        // BoxCollider2Dを取得
        BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
        // トリガーに設定
        boxCollider.isTrigger = true;
    }


    // ==============================================
    // ゴール判定処理
    // ==============================================
    void OnTriggerEnter2D(Collider2D other)
    {
        // ゴールと判定するタグを持つオブジェクトが衝突した場合
        if (other.CompareTag(m_GoalTag))
        {
            // ゴールマネージャーにゴールしたことを通知する
            m_GoalManager.ExecuteGoal();
        }
    }
}
