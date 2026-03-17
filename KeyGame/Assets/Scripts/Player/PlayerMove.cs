using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;


/*  * プレイヤーの移動を制御するスクリプト
 *  * 
 */

// ==============================================
// 必須コンポーネント定義
// ==============================================
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerInput))]


// ==============================================
// クラス定義
// ==============================================
public sealed class PlayerMove : MonoBehaviour
{
    // ------------------------------------------
    // メンバー変数
    // ------------------------------------------
    // エディター上で設定可能変数
    [Header("プレイヤーの基本設定")]
    [Tooltip("移動速度")]
    [SerializeField] private float m_MoveSpeed = 5f;
    [Tooltip("跳躍力")]
    [SerializeField] private float m_JumpForce = 5f;
    [Tooltip("最大跳躍数")]
    [SerializeField] private int m_MaxJumpCount = 1;

    [Header("地面との当たり判定設定")]
    [Tooltip("地面判定レイヤー")]
    [SerializeField] private LayerMask m_GroundLayer;
    [Tooltip("地面との判定相対位置")]
    [SerializeField] private Vector3 m_CheckPos = new Vector3(0.0f, -0.5f, 0.0f);
    [Tooltip("地面との判定半径")]
    [SerializeField] private float m_CheckRadius = 0.1f;

    // プライベート変数
    // コンポーネントのキャッシュ用変数
    private Rigidbody2D m_Rigidbody2D;
    private PlayerInput m_PlayerInput;
    // アニメーション制御用変数
    private Animator m_Animator;
    private SpriteRenderer m_SpriteRenderer;

    // Input情報取得変数
    private float m_MoveInput;
    private bool m_JumpInput;

    // 変数
    // 跳躍回数
    private int m_JumpCount;
    // 前フレームの地面接触状態
    private bool m_WasGrounded;


    // ------------------------------------------
    // オブジェクト作成時の処理
    // ------------------------------------------
    private void Awake()
    {
        // コンポーネントのキャッシュ
        m_Rigidbody2D = GetComponent<Rigidbody2D>();
        m_PlayerInput = GetComponent<PlayerInput>();
        m_Animator = GetComponent<Animator>();
        m_SpriteRenderer = GetComponent<SpriteRenderer>();

        // InputAction に関数を登録
        m_PlayerInput.actions["Move"].performed += MoveInput;
        m_PlayerInput.actions["Move"].canceled += MoveInput;

        m_PlayerInput.actions["Jump"].performed += JumpInput;


#if UNITY_EDITOR
        // エラーチェック
        if (m_Rigidbody2D == null)
        {
            Debug.LogError("Rigidbody2D コンポーネントが見つかりませんでした。");
            return;
        }
        if (m_PlayerInput == null)
        {
            Debug.LogError("PlayerInput コンポーネントが見つかりませんでした。");
            return;
        }
#endif
    }


    // ------------------------------------------
    // 初期化
    // ------------------------------------------
    void Start()
    {
        // 跳躍回数の初期化
        m_JumpCount = m_MaxJumpCount;
    }


    // ------------------------------------------
    // 物理演算更新
    // ------------------------------------------
    void FixedUpdate()
    {
        // 移動処理
        m_Rigidbody2D.linearVelocity = new Vector2(m_MoveSpeed * m_MoveInput, m_Rigidbody2D.linearVelocity.y);

        // 跳躍処理
        if (m_JumpInput)
        {
            if (m_JumpCount > 0)
            {
                // ジャンプ処理
                m_Rigidbody2D.linearVelocityY = m_JumpForce;

                m_JumpCount--;
            }

            // フラグ更新
            m_JumpInput = false;
        }

        // 着地判定処理
        bool isGrounded = Physics2D.OverlapCircle(transform.position + m_CheckPos, m_CheckRadius, m_GroundLayer);

        if (isGrounded && !m_WasGrounded)
        {
            // 地面に接触している場合、跳躍回数をリセット
            m_JumpCount = m_MaxJumpCount;
        }

        // アニメーション制御
        if (m_Animator != null)
        {
            // 移動しているか
            m_Animator.SetBool("isWalking", m_MoveInput != 0);

            // 地面にいるか
            m_Animator.SetBool("isGrounded", isGrounded);
        }

        // 向きの反転処理
        if (m_SpriteRenderer != null)
        {
            if (m_MoveInput < 0)
            {
                // 左向き
                m_SpriteRenderer.flipX = true;
            }
            else if (m_MoveInput > 0)
            {
                // 右向き
                m_SpriteRenderer.flipX = false;
            }
        }

        // フラグ更新
        m_WasGrounded = isGrounded;


    }


    // ------------------------------------------
    // 入力情報受け取り
    // ------------------------------------------
    // 移動
    public void MoveInput(InputAction.CallbackContext context)
    {
        m_MoveInput = context.ReadValue<float>();
    }
    // 跳躍
    public void JumpInput(InputAction.CallbackContext context)
    {
        m_JumpInput = true;
    }


    // ------------------------------------------
    // デバッグ用: 地面との当たり判定の可視化
    // ------------------------------------------
    private void OnDrawGizmosSelected()
    {
        // 赤色表示
        Gizmos.color = Color.red;
        // プレイヤーの位置 + オフセットに円を描画
        Gizmos.DrawWireSphere(transform.position + m_CheckPos, m_CheckRadius);
    }
}
