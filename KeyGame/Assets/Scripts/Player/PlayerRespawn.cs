using UnityEngine;

public sealed class PlayerRespawn : MonoBehaviour
{
    [Header("このY座標より下に落ちたらミス")]
    [SerializeField]
    private float missY = -10f;

    [Header("リスポーン地点")]
    [SerializeField]
    private Transform respawnPoint;

    private Rigidbody2D m_Rigidbody2D;
    private SpriteRenderer m_SpriteRenderer;

    private Vector3 m_StartPosition;
    private Quaternion m_StartRotation;
    private bool m_StartFlipX;
    private bool m_IsRespawning;

    private void Awake()
    {
        m_Rigidbody2D = GetComponent<Rigidbody2D>();
        m_SpriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        m_StartPosition = transform.position;
        m_StartRotation = transform.rotation;

        if (m_SpriteRenderer != null)
        {
            m_StartFlipX = m_SpriteRenderer.flipX;
        }
    }

    private void Update()
    {
        if (!m_IsRespawning && transform.position.y < missY)
        {
            TriggerMiss();
        }
    }

    public void TriggerMiss()
    {
        if (m_IsRespawning)
        {
            return;
        }

        m_IsRespawning = true;

        Vector3 targetPosition = respawnPoint != null ? respawnPoint.position : m_StartPosition;
        Quaternion targetRotation = respawnPoint != null ? respawnPoint.rotation : m_StartRotation;

        if (m_Rigidbody2D != null)
        {
            m_Rigidbody2D.linearVelocity = Vector2.zero;
            m_Rigidbody2D.angularVelocity = 0f;
        }

        transform.position = targetPosition;
        transform.rotation = targetRotation;

        if (m_SpriteRenderer != null)
        {
            m_SpriteRenderer.flipX = m_StartFlipX;
        }

        m_IsRespawning = false;
    }
}
