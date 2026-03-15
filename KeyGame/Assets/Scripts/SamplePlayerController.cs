using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public sealed class SamplePlayerController : MonoBehaviour
{
    private const float PlayerSpritePixelsPerUnit = 32f;

    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float jumpForce = 9f;
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float groundCheckDistance = 0.12f;
    [SerializeField] private float coyoteTime = 0.08f;
    [SerializeField] private float jumpBufferTime = 0.1f;

    private readonly Collider2D[] _groundHits = new Collider2D[4];

    private Rigidbody2D _rigidbody2D;
    private BoxCollider2D _collider2D;
    private float _moveInput;
    private float _coyoteTimer;
    private float _jumpBufferTimer;

    private void Reset()
    {
        var renderer = GetOrAddComponent<SpriteRenderer>();
        renderer.sprite = SampleAssetLoader.LoadSprite("Characters/SamplePlayer/PlayerSquare.png", PlayerSpritePixelsPerUnit);
        renderer.color = Color.white;
        renderer.sortingOrder = 5;

        var collider2D = GetOrAddComponent<BoxCollider2D>();
        collider2D.size = Vector2.one;

        var rigidbody2D = GetOrAddComponent<Rigidbody2D>();
        rigidbody2D.gravityScale = 3f;
        rigidbody2D.freezeRotation = true;
    }

    private void Awake()
    {
        _rigidbody2D = GetComponent<Rigidbody2D>();
        _collider2D = GetComponent<BoxCollider2D>();
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        _moveInput = ReadHorizontalInput();

        if (ReadJumpPressed())
        {
            _jumpBufferTimer = jumpBufferTime;
        }
        else
        {
            _jumpBufferTimer = Mathf.Max(0f, _jumpBufferTimer - Time.deltaTime);
        }

        if (IsGrounded())
        {
            _coyoteTimer = coyoteTime;
        }
        else
        {
            _coyoteTimer = Mathf.Max(0f, _coyoteTimer - Time.deltaTime);
        }

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

        _rigidbody2D.linearVelocity = new Vector2(_moveInput * moveSpeed, _rigidbody2D.linearVelocity.y);
    }

    private bool IsGrounded()
    {
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
        if (Keyboard.current != null)
        {
            var leftPressed = Keyboard.current.leftArrowKey.isPressed;
            var rightPressed = Keyboard.current.rightArrowKey.isPressed;
            return (rightPressed ? 1f : 0f) - (leftPressed ? 1f : 0f);
        }

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
