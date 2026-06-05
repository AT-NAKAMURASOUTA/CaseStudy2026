using UnityEngine;

public class LowGravity : MonoBehaviour
{
    [Header("BGM設定")]
    [SerializeField] private AudioSource m_BGMSource;

    [Header("エリア内での再生速度")]
    [SerializeField] private float m_PitchInArea = 0.5f;

    // 元の速度保存用
    private float m_DefaultPitch = 1.0f;

    private void Start()
    {
        // 未設定なら BGMManager から探す
        if (m_BGMSource == null)
        {
            GameObject bgmManager = GameObject.Find("BGMManager");

            if (bgmManager != null)
                m_BGMSource = bgmManager.GetComponent<AudioSource>();
        }

        if (m_BGMSource != null)
            m_DefaultPitch = m_BGMSource.pitch;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Playerタグだけ反応
        if (collision.CompareTag("Player"))
            m_BGMSource.pitch = m_PitchInArea;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        // Playerタグだけ反応
        if (collision.CompareTag("Player"))
            m_BGMSource.pitch = m_DefaultPitch;
    }
}
