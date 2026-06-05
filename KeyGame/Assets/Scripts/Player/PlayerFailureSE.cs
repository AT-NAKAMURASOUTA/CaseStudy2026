using UnityEngine;

/* * Playerが失敗したときのSEを鳴らすクラス
 */

[RequireComponent(typeof(AudioSource))]
public class PlayerFailureSE : MonoBehaviour
{
    // 失敗したときのSE
    [SerializeField] private AudioClip m_FailureSE;
    // SEを鳴らすためのAudioSource
    private AudioSource m_AudioSource;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_AudioSource = GetComponent<AudioSource>();
        if(m_FailureSE == null)
        {
            Debug.LogError("PlayerFailureSE: 失敗SEが設定されていません。");
            return;
        }
    }

    // ==============================
    // 実行
    // ==============================
    public void FailurePlayerSE()
    {
        m_AudioSource.PlayOneShot(m_FailureSE);
    }
}
