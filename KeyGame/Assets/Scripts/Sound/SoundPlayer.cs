using UnityEngine;

/* * SEを鳴らすクラス
 */

[RequireComponent(typeof(AudioSource))]
public class SoundPlayer : MonoBehaviour
{
    // SE
    [SerializeField] private AudioClip m_SE;
    // SEを鳴らすためのAudioSource
    private AudioSource m_AudioSource;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_AudioSource = GetComponent<AudioSource>();
        if(m_SE == null)
        {
            Debug.LogError("PlayerSE: SEが設定されていません。");
            return;
        }
    }

    // ==============================
    // 実行
    // ==============================
    public void PlaySE()
    {
        m_AudioSource.PlayOneShot(m_SE);
    }
}
