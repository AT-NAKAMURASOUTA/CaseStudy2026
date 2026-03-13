using UnityEngine;
using UnityEngine.InputSystem;

// プロトタイプ用プレイヤーの移動と、最低限の見た目・物理設定を管理する。
[ExecuteAlways]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public sealed class PrototypePlayerController : MonoBehaviour
{
    // 水平方向の移動速度。
    [SerializeField] private float moveSpeed = 6f;

    // ジャンプ開始時に与える上向き速度。
    [SerializeField] private float jumpForce = 9f;

    // 接地判定で参照するレイヤー。
    [SerializeField] private LayerMask groundMask = ~0;

    // 足元判定をどれだけ下方向へ伸ばすか。
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

        // 入力は Update で取得し、FixedUpdate で使う値だけを保持する。
        _moveInput = ReadHorizontalInput();

        // 多重ジャンプを避けるため、接地中のみジャンプ開始を許可する。
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

        // 垂直方向の速度は重力とジャンプに任せ、水平方向だけを更新する。
        _rigidbody2D.linearVelocity = new Vector2(_moveInput * moveSpeed, _rigidbody2D.linearVelocity.y);
    }

    private void EnsureComponents()
    {
        // 手動でオブジェクトを編集しても壊れないよう、必須設定だけを補完する。
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
        // コライダー下端から短い BoxCast を飛ばし、地面との接触を判定する。
        var bounds = _collider2D.bounds;
        var origin = new Vector2(bounds.center.x, bounds.min.y);
        var hit = Physics2D.BoxCast(
            origin,
            new Vector2(bounds.size.x * 0.9f, 0.05f),
            0f,
            Vector2.down,
            groundCheckDistance,
            groundMask);

        return hit.collider != null && hit.collider.gameObject != gameObject;
    }

    private static float ReadHorizontalInput()
    {
        // New Input System が使える場合はそちらを優先する。
        if (Keyboard.current != null)
        {
            var leftPressed = Keyboard.current.leftArrowKey.isPressed;
            var rightPressed = Keyboard.current.rightArrowKey.isPressed;
            return (rightPressed ? 1f : 0f) - (leftPressed ? 1f : 0f);
        }

        // フォールバックとして旧 Input Manager にも対応しておく。
        var horizontal = 0f;

        if (Input.GetKey(KeyCode.LeftArrow))
        {
            horizontal -= 1f;
        }

        if (Input.GetKey(KeyCode.RightArrow))
        {
            horizontal += 1f;
        }

        return horizontal;
    }

    private static bool ReadJumpPressed()
    {
        // アルファベット生成で A-Z を使うため、ジャンプは Space と上矢印に限定する。
        if (Keyboard.current != null)
        {
            return Keyboard.current.spaceKey.wasPressedThisFrame
                || Keyboard.current.upArrowKey.wasPressedThisFrame;
        }

        return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow);
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
