using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// プロトタイプ用プレイヤーの移動と基本セットアップを担当する。
/// シーン上の位置やスケールは手作業で調整できるようにしつつ、
/// 表示と物理挙動に必要なコンポーネントだけを補完する方針にしている。
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public sealed class PrototypePlayerController : MonoBehaviour
{
    // 水平方向の移動速度。プロトタイプでは微調整しやすいように Inspector から変更できる。
    [SerializeField] private float moveSpeed = 6f;

    // ジャンプ時に与える上向き速度。Rigidbody2D の速度を直接更新して挙動を単純化する。
    [SerializeField] private float jumpForce = 9f;

    // 接地判定に使うレイヤー。既定値ではすべてのレイヤーを対象にする。
    [SerializeField] private LayerMask groundMask = ~0;

    // 足元判定の余白。値を大きくしすぎると壁や段差を地面と誤認しやすくなる。
    [SerializeField] private float groundCheckDistance = 0.08f;

    private Rigidbody2D _rigidbody2D;
    private BoxCollider2D _collider2D;
    private float _moveInput;

    private void OnEnable()
    {
        EnsureComponents();
    }

    private void OnValidate()
    {
        EnsureComponents();
    }

    private void Awake()
    {
        EnsureComponents();
        _rigidbody2D = GetComponent<Rigidbody2D>();
        _collider2D = GetComponent<BoxCollider2D>();
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        // 入力は Update で読み取り、物理更新に使う値だけを保持する。
        _moveInput = ReadHorizontalInput();

        // ジャンプ開始は接地中のみ許可し、多重ジャンプが混ざらないようにする。
        if (ReadJumpPressed() && IsGrounded())
        {
            _rigidbody2D.linearVelocity = new Vector2(_rigidbody2D.linearVelocity.x, jumpForce);
        }
    }

    private void FixedUpdate()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        // 水平方向だけを制御し、垂直方向の速度は重力やジャンプ処理に任せる。
        _rigidbody2D.linearVelocity = new Vector2(_moveInput * moveSpeed, _rigidbody2D.linearVelocity.y);
    }

    private void EnsureComponents()
    {
        // プレイヤーの見た目と物理設定を最低限ここでそろえる。
        var renderer = GetOrAddComponent<SpriteRenderer>();
        renderer.sprite = Resources.Load<Sprite>("PrototypeSquare");
        renderer.color = new Color(0.16f, 0.67f, 0.89f);
        renderer.sortingOrder = 5;

        _collider2D = GetOrAddComponent<BoxCollider2D>();
        _rigidbody2D = GetOrAddComponent<Rigidbody2D>();
        _rigidbody2D.gravityScale = 3f;
        _rigidbody2D.freezeRotation = true;
    }

    private bool IsGrounded()
    {
        // 自身のコライダー下端から短い BoxCast を飛ばし、地面との接触を判定する。
        var bounds = _collider2D.bounds;
        var origin = new Vector2(bounds.center.x, bounds.min.y);
        var hit = Physics2D.BoxCast(origin, new Vector2(bounds.size.x * 0.9f, 0.05f), 0f, Vector2.down, groundCheckDistance, groundMask);
        return hit.collider != null && hit.collider.gameObject != gameObject;
    }

    private static float ReadHorizontalInput()
    {
        if (Keyboard.current != null)
        {
            var leftPressed = Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed;
            var rightPressed = Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed;
            return (rightPressed ? 1f : 0f) - (leftPressed ? 1f : 0f);
        }

        return Input.GetAxisRaw("Horizontal");
    }

    private static bool ReadJumpPressed()
    {
        if (Keyboard.current != null)
        {
            return Keyboard.current.spaceKey.wasPressedThisFrame
                || Keyboard.current.wKey.wasPressedThisFrame
                || Keyboard.current.upArrowKey.wasPressedThisFrame;
        }

        return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);
    }

    private T GetOrAddComponent<T>() where T : Component
    {
        var component = GetComponent<T>();
        if (component == null)
        {
            component = gameObject.AddComponent<T>();
        }

        return component;
    }
}
