using UnityEditor.SceneManagement;
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
    [Header("ジャンプアニメ調整")]
    [SerializeField] private float m_TakeoffAnimationDuration = 0.16f;
    [SerializeField] private float m_TakeoffAnimationEndNormalizedTime = 0.22f;
    [SerializeField] private float m_AirborneAnimationNormalizedTime = 0.58f;
    [SerializeField] private float m_LandingAnimationStartNormalizedTime = 0.8f;
    [SerializeField] private float m_LandingPredictionTime = 0.1f;
    [SerializeField] private float m_MinLandingSpeed = 0.35f;
    [SerializeField] private float m_GroundProbeExtraDistance = 0.4f;

    [Header("加速度、低重力の量ScriptableObejct")]
    [SerializeField]ScriptableObject_SpecialAreaData m_SpecialAreaAsset;
    [SerializeField] private PhysicsMaterial2D m_PhysicsMaterialOverride;

    // プライベート変数
    // コンポーネントのキャッシュ用変数
    private Rigidbody2D m_Rigidbody2D;
    private PlayerInput m_PlayerInput;
    private SpecialAreaCollision m_SpecialAreaCollision;//okada:特殊エリアの当たり判定
    // アニメーション制御用変数
    private Animator m_Animator;
    private SpriteRenderer m_SpriteRenderer;
    private Collider2D[] m_PlayerColliders;
    private PhysicsMaterial2D m_RuntimeNoFrictionMaterial;
    private static readonly int JumpStateHash = Animator.StringToHash("Base Layer.Jump");

    // Input情報取得変数
    private float m_MoveInput;
    private bool m_JumpInput;

    // 変数
    // 跳躍回数
    private int m_JumpCount;
    // 前フレームの地面接触状態
    private bool m_WasGrounded;
    private bool m_IsJumpAnimating;
    private float m_JumpAnimationElapsed;


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
        m_PlayerColliders = GetComponents<Collider2D>();
        m_SpecialAreaCollision = this.gameObject.AddComponent<SpecialAreaCollision>();//追加
        ApplyNoFrictionMaterial();

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

    private void ApplyNoFrictionMaterial()
    {
        // Colliderが取れていないなら何もしない
        if (m_PlayerColliders == null || m_PlayerColliders.Length == 0)
        {
            return;
        }

        // Inspectorで指定されているものがあればそれを使用
        PhysicsMaterial2D material = m_PhysicsMaterialOverride;
        if (material == null)
        {
            // 未設定なら実行時用に摩擦なしマテリアルを作成
            m_RuntimeNoFrictionMaterial = new PhysicsMaterial2D("PlayerNoFrictionRuntime");

            // 摩擦をなくす
            m_RuntimeNoFrictionMaterial.friction = 0f;

            // 反発を0にする
            m_RuntimeNoFrictionMaterial.bounciness = 0f;

            material = m_RuntimeNoFrictionMaterial;
        }

        foreach (Collider2D playerCollider in m_PlayerColliders)
        {
            // 配列の中にnullがあれば飛ばす
            if (playerCollider == null)
            {
                continue;
            }

            // プレイヤーについているCollider全部に同じマテリアルを入れる
            playerCollider.sharedMaterial = material;
        }
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
        Vector2 nowMoveSpeed = new Vector2(m_MoveSpeed * m_MoveInput, m_Rigidbody2D.linearVelocity.y);
        
        //横移動の補正
        if(m_SpecialAreaCollision.GetAccelerationCollision())
        {//加速する

            //ScriptableObjectにSetしてある値（倍率）を使う
            nowMoveSpeed.x *= m_SpecialAreaAsset.GetAccelerationMagnification();
        }
        //縦移動
        if(m_SpecialAreaCollision.GetLowGravityCollision())
        {//落下速度遅く、上昇速度遅く

            //ScriptableObjectにSetしてある値（倍率）を使う
            nowMoveSpeed.y *= m_SpecialAreaAsset.GetLowGravityMagnification();
        }

        //移動
        m_Rigidbody2D.linearVelocity = nowMoveSpeed;

        // 跳躍処理
        if (m_JumpInput)
        {
            if (m_JumpCount > 0)
            {
                // ジャンプ処理
                m_Rigidbody2D.linearVelocityY = m_JumpForce;

                m_JumpCount--;
                m_IsJumpAnimating = true;
                m_JumpAnimationElapsed = 0f;

                if (m_Animator != null)
                {
                    m_Animator.Play(JumpStateHash, 0, 0f);
                    m_Animator.Update(0f);
                }
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
            m_IsJumpAnimating = false;
            m_JumpAnimationElapsed = 0f;
        }

        // アニメーション制御
        if (m_Animator != null)
        {
            // 移動しているか
            m_Animator.SetBool("isWalking", m_MoveInput != 0);

            // 実際にジャンプした時だけジャンプアニメを再生
            m_Animator.SetBool("isJumping", m_IsJumpAnimating);

            // 地面にいるか
            m_Animator.SetBool("isGrounded", isGrounded);

            UpdateJumpAnimationPlayback(isGrounded);
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

    private void UpdateJumpAnimationPlayback(bool isGrounded)
    {
        if (!m_IsJumpAnimating || m_Animator == null)
        {
            return;
        }

        m_JumpAnimationElapsed += Time.fixedDeltaTime;

        float normalizedTime;
        if (m_JumpAnimationElapsed < m_TakeoffAnimationDuration)
        {
            float takeoffT = m_TakeoffAnimationDuration > 0f
                ? Mathf.Clamp01(m_JumpAnimationElapsed / m_TakeoffAnimationDuration)
                : 1f;

            normalizedTime = Mathf.Lerp(0f, m_TakeoffAnimationEndNormalizedTime, takeoffT);
        }
        else if (TryGetLandingApproachRatio(isGrounded, out float landingT))
        {
            normalizedTime = Mathf.Lerp(
                m_LandingAnimationStartNormalizedTime,
                1f,
                landingT
            );
        }
        else
        {
            normalizedTime = m_AirborneAnimationNormalizedTime;
        }

        m_Animator.Play(JumpStateHash, 0, Mathf.Clamp01(normalizedTime));
        m_Animator.Update(0f);
    }

    private bool TryGetLandingApproachRatio(bool isGrounded, out float landingT)
    {
        landingT = 0f;

        if (isGrounded)
        {
            landingT = 1f;
            return true;
        }

        float downwardSpeed = -m_Rigidbody2D.linearVelocity.y;
        if (downwardSpeed <= m_MinLandingSpeed)
        {
            return false;
        }

        if (!TryGetGroundDistance(out float groundDistance))
        {
            return false;
        }

        float timeToGround = groundDistance / downwardSpeed;
        if (timeToGround > m_LandingPredictionTime)
        {
            return false;
        }

        landingT = 1f - Mathf.Clamp01(timeToGround / Mathf.Max(0.0001f, m_LandingPredictionTime));
        return true;
    }

    private bool TryGetGroundDistance(out float groundDistance)
    {
        Vector2 origin = (Vector2)transform.position + (Vector2)m_CheckPos;
        float castDistance = m_CheckRadius + m_GroundProbeExtraDistance;

        RaycastHit2D hit = Physics2D.CircleCast(
            origin,
            m_CheckRadius,
            Vector2.down,
            castDistance,
            m_GroundLayer
        );

        if (!hit.collider)
        {
            groundDistance = 0f;
            return false;
        }

        groundDistance = Mathf.Max(0f, hit.distance - m_CheckRadius);
        return true;
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
