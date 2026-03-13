using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public sealed class PrototypePlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float jumpForce = 9f;
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float groundCheckDistance = 0.08f;

    private Rigidbody2D _rigidbody2D;
    private BoxCollider2D _collider2D;
    private float _moveInput;

    private void Awake()
    {
        _rigidbody2D = GetComponent<Rigidbody2D>();
        _collider2D = GetComponent<BoxCollider2D>();
    }

    private void Update()
    {
        _moveInput = ReadHorizontalInput();

        if (ReadJumpPressed() && IsGrounded())
        {
            _rigidbody2D.linearVelocity = new Vector2(_rigidbody2D.linearVelocity.x, jumpForce);
        }
    }

    private void FixedUpdate()
    {
        _rigidbody2D.linearVelocity = new Vector2(_moveInput * moveSpeed, _rigidbody2D.linearVelocity.y);
    }

    private bool IsGrounded()
    {
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
}
