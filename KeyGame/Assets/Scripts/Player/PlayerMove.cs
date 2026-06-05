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
    [Tooltip("地面との判定四角形サイズ")]
    [SerializeField] private Vector2 m_CheckBoxSize = new Vector2(0.1f, 0.1f);
    [Tooltip("地面との判定する斜辺の角度")]
    [SerializeField] private float m_CheckBoxAngle = 45f;

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
    private SpecialAreaVelocityUpdate m_SpecialAreaVelocityUpdate;//特殊エリアの速度増減効果の処理をまとめたもの

    // アニメーション制御用変数
    private Animator m_Animator;
    private SpriteRenderer m_SpriteRenderer;
    private Collider2D[] m_PlayerColliders;
    private PhysicsMaterial2D m_RuntimeNoFrictionMaterial;
    private static readonly int JumpStateHash = Animator.StringToHash("Base Layer.Jump");

    // SE再生用変数
    [Header("効果音")]
    [SerializeField] private AudioClip m_WalkSE;
    [SerializeField] private AudioClip m_JumpSE;

    private AudioSource m_WalkAudioSource;
    private AudioSource m_AudioSource;

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


    // 風エリア用調整変数
    private float m_WindAreaMoveSpeedModifier = 0f;

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
        m_SpecialAreaVelocityUpdate = new SpecialAreaVelocityUpdate();//追加
        ApplyNoFrictionMaterial();

        // InputAction に関数を登録
        m_PlayerInput.actions["Move"].performed += MoveInput;
        m_PlayerInput.actions["Move"].canceled += MoveInput;

        m_PlayerInput.actions["Jump"].performed += JumpInput;

        // --- SE用 AudioSource（ジャンプなどの短い効果音）を確実に用意 ---
        m_AudioSource = GetComponent<AudioSource>();
        if (m_AudioSource == null)
            m_AudioSource = gameObject.AddComponent<AudioSource>();
        m_AudioSource.playOnAwake = false;
        m_AudioSource.loop = false;
        m_AudioSource.spatialBlend = 0f; // 2Dサウンド
        m_AudioSource.volume = 1.0f;
        m_AudioSource.clip = null; // PlayOneShot を使うので clip は空にしておく

        // --- 歩行音用の別 AudioSource を必ず新規作成して競合を避ける ---
        m_WalkAudioSource = gameObject.AddComponent<AudioSource>();
        m_WalkAudioSource.playOnAwake = false;
        m_WalkAudioSource.loop = true;
        m_WalkAudioSource.spatialBlend = 0f;
        m_WalkAudioSource.volume = 1.0f;
        m_WalkAudioSource.clip = m_WalkSE;


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
        m_WasGrounded = CheckIsGrounded();
        m_JumpCount = m_WasGrounded ? m_MaxJumpCount : Mathf.Max(0, m_MaxJumpCount - 1);
    }


    // ------------------------------------------
    // 物理演算更新
    // ------------------------------------------
    void FixedUpdate()
    {
        // 移動処理
        Vector2 nowMoveSpeed = new Vector2(m_MoveSpeed * m_MoveInput, m_Rigidbody2D.linearVelocity.y);

        //特殊エリアの効果を反映
        nowMoveSpeed = m_SpecialAreaVelocityUpdate.SpecialAreaUpdate(
            nowMoveSpeed, 
            m_SpecialAreaCollision,
            m_SpecialAreaAsset);
        

        //移動                                      + 風エリアの補正 by 植田
        m_Rigidbody2D.linearVelocity = nowMoveSpeed + new Vector2(m_WindAreaMoveSpeedModifier, 0);

        // 着地判定処理
        bool isGrounded = CheckIsGrounded();
        if (isGrounded && !m_WasGrounded)
        {
            // 地面に接触している場合、跳躍回数をリセット
            m_JumpCount = m_MaxJumpCount;
            m_IsJumpAnimating = false;
            m_JumpAnimationElapsed = 0f;
        }
        else if (!isGrounded && m_WasGrounded)
        {
            // ジャンプせずに足場から落ちた場合、地上ジャンプ分は消費済みにする
            m_JumpCount = Mathf.Max(0, m_MaxJumpCount - 1);
        }

        // 跳躍処理
        if (m_JumpInput)
        {
            if (m_JumpCount > 0)
            {
                // ジャンプ処理
                m_Rigidbody2D.linearVelocityY = m_JumpForce;
                Debug.Log("Jump! JumpCount: " + m_JumpCount);

                m_JumpCount--;
                m_IsJumpAnimating = true;
                m_JumpAnimationElapsed = 0f;

                if (m_Animator != null)
                {
                    m_Animator.Play(JumpStateHash, 0, 0f);
                    m_Animator.Update(0f);
                }

                // ジャンプSE再生
                if (m_AudioSource == null)
                {
                    Debug.LogWarning("m_AudioSource is null - cannot play jump SE");
                }
                if (m_JumpSE == null)
                {
                    Debug.LogWarning("m_JumpSE is null - assign clip in Inspector");
                }
                if (m_AudioSource != null && m_JumpSE != null)
                {
                    Debug.Log("Play Jump SE");
                    m_AudioSource.PlayOneShot(m_JumpSE);
                }
            }

            // フラグ更新
            m_JumpInput = false;
        }

        // 歩行SE制御: 地面にいる && 左右入力がある とき再生、そうでなければ停止
        bool isWalking = (m_MoveInput != 0f) && isGrounded;
        if (m_WalkSE != null && m_WalkAudioSource != null)
        {
            if (isWalking)
            {
                if (!m_WalkAudioSource.isPlaying)
                {
                    m_WalkAudioSource.Play();
                }
            }
            else
            {
                if (m_WalkAudioSource.isPlaying)
                {
                    m_WalkAudioSource.Stop();
                }
            }
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

    private bool CheckIsGrounded()
    {
        // プレイヤーの位置 + オフセットを判定の起点とする
        Vector2 origin = (Vector2)transform.position + (Vector2)m_CheckPos;
        // 判定の距離は、四角形の半分のサイズ + 少し余裕を持たせる
        float castDistance = 0.05f;

        // BoxCastを使って、地面レイヤーとの当たり判定を行う
        RaycastHit2D hit = Physics2D.BoxCast(
            origin,
            m_CheckBoxSize,
            0f,
            Vector2.down,
            castDistance,
            m_GroundLayer);

        // 当たっていない場合は地面にいないと判断
        if (!hit.collider) { return false; }

        // 当たっている場合、傾斜の角度を計算して、地面とみなすか判断
        float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);

        // 傾斜が指定した角度以下なら地面とみなす
        return slopeAngle <= m_CheckBoxAngle;
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
        float castDistance = m_CheckBoxSize.y + m_GroundProbeExtraDistance;

        RaycastHit2D hit = Physics2D.BoxCast(
            origin,
            m_CheckBoxSize,
            0f,
            Vector2.down,
            castDistance,
            m_GroundLayer
        );

        if (!hit.collider)
        {
            groundDistance = 0f;
            return false;
        }

        groundDistance = Mathf.Max(0f, hit.distance - m_CheckBoxSize.y / 2);
        return true;
    }

    //--------------------------------------------
    // 風エリアに入ったときの速度の調整　（何かあれば植田まで）
    //--------------------------------------------
    public void InWindArea(float windStrength)
    {
        m_WindAreaMoveSpeedModifier = windStrength * m_MoveSpeed;
    }

    public void ExitWindArea()
    {
        m_WindAreaMoveSpeedModifier = 0.0f;
    }

    // ------------------------------------------
    // デバッグ用: 地面との当たり判定の可視化
    // ------------------------------------------
    private void OnDrawGizmosSelected()
    {
        // 赤色表示
        Gizmos.color = Color.red;
        // プレイヤーの位置 + オフセットに四角形を描画
        Gizmos.DrawWireCube(transform.position + m_CheckPos, m_CheckBoxSize);
    }
}
