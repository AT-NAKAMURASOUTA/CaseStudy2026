using UnityEngine;
using UnityEngine.InputSystem;

// プロトタイプ用プレイヤーの移動と、最低限の見た目・物理設定を管理する。
[ExecuteAlways]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public sealed class PrototypePlayerController : MonoBehaviour
{
    private const float PlayerSpritePixelsPerUnit = 32f;

    // 水平方向の移動速度。
    [SerializeField] private float moveSpeed = 6f;

    // ジャンプ開始時に与える上向き速度。
    [SerializeField] private float jumpForce = 9f;

    // 接地判定で参照するレイヤー。
    [SerializeField] private LayerMask groundMask = ~0;

    // 足元の接地判定をどれだけ下方向へ広げるか。
    [SerializeField] private float groundCheckDistance = 0.12f;

    // 接地していた直後の猶予時間。段差や着地直後の入力抜けを減らす。
    [SerializeField] private float coyoteTime = 0.08f;

    // ジャンプ入力を短時間だけ保持する時間。着地前後の取りこぼしを減らす。
    [SerializeField] private float jumpBufferTime = 0.1f;

    private readonly Collider2D[] _groundHits = new Collider2D[4];

    private Rigidbody2D _rigidbody2D;
    private BoxCollider2D _collider2D;
    private float _moveInput;
    private float _coyoteTimer;
    private float _jumpBufferTimer;

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

        // ジャンプ入力は少しだけ保持し、着地直後でも反応しやすくする。
        if (ReadJumpPressed())
        {
            _jumpBufferTimer = jumpBufferTime;
        }
        else
        {
            _jumpBufferTimer = Mathf.Max(0f, _jumpBufferTimer - Time.deltaTime);
        }

        // 接地中は猶予時間を毎フレーム更新し、空中では減衰させる。
        if (IsGrounded())
        {
            _coyoteTimer = coyoteTime;
        }
        else
        {
            _coyoteTimer = Mathf.Max(0f, _coyoteTimer - Time.deltaTime);
        }

        // 入力保持時間と接地猶予の両方が残っている間だけジャンプを発動する。
        if (_jumpBufferTimer > 0f && _coyoteTimer > 0f)
        {
            _rigidbody2D.linearVelocity = new Vector2(_rigidbody2D.linearVelocity.x, jumpForce);
            _jumpBufferTimer = 0f;
            _coyoteTimer = 0f;
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
        // プレイヤー画像も PrototypeAssets/Characters から読み込む。
        var renderer = GetOrAddComponent<SpriteRenderer>();
        renderer.sprite = PrototypeAssetLoader.LoadSprite("Characters/SamplePlayer/PlayerSquare.png", PlayerSpritePixelsPerUnit);
        renderer.color = Color.white;
        renderer.sortingOrder = 5;

        _collider2D = GetOrAddComponent<BoxCollider2D>();
        _rigidbody2D = GetOrAddComponent<Rigidbody2D>();
        _rigidbody2D.gravityScale = 3f;
        _rigidbody2D.freezeRotation = true;
    }

    private bool IsGrounded()
    {
        // プレイヤーの足元に薄い判定を置き、地面との重なりを直接調べる。
        var bounds = _collider2D.bounds;
        var checkCenter = new Vector2(bounds.center.x, bounds.min.y - groundCheckDistance * 0.5f);
        var checkSize = new Vector2(bounds.size.x * 0.8f, groundCheckDistance);

        var hitCount = Physics2D.OverlapBox(checkCenter, checkSize, 0f, new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = groundMask,
            useTriggers = false,
        }, _groundHits);

        for (var i = 0; i < hitCount; i++)
        {
            if (_groundHits[i] != null && _groundHits[i] != _collider2D)
            {
                return true;
            }
        }

        return false;
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
