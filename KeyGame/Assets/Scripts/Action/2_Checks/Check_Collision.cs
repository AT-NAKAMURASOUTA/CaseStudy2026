using UnityEngine;


/*  * 物体と当たったときにタグを判定してアクションを実行するクラス
 */


// ==============================================
// 必須コンポーネント定義
// ==============================================
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(ActionsManager))]


public class Check_Collision : MonoBehaviour
{
    // ==============================================
    // メンバー変数
    // ==============================================
    // アクションを実行するタグ
    [SerializeField] private string m_Tag = "Player";
    // アクションマネージャー
    private ActionsManager m_ActionsManager;


    // ==============================================
    // 初期化処理
    // ==============================================
    void Start()
    {
        // アクションマネージャーを取得
        m_ActionsManager = GetComponent<ActionsManager>();

        // BoxCollider2Dを取得
        BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
        // トリガーに設定
        boxCollider.isTrigger = true;
    }


    // ==============================================
    // アクション判定処理
    // ==============================================
    void OnTriggerEnter2D(Collider2D other)
    {
        // 判定するタグを持つオブジェクトが衝突した場合
        if (other.CompareTag(m_Tag))
        {
            // アクションマネージャーに実行を指示
            m_ActionsManager.ExecuteAction();
        }
    }
}
