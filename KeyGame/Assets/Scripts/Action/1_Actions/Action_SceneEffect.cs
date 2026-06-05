using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

/*  * シーンエフェクトのスクリプト
 *  * 現在は音を鳴らすだけの処理
 */

[RequireComponent(typeof(AudioSource))]
public class Action_SceneEffect : BaseAction
{
    // ======================================
    // メンバー変数
    // =======================================
    [SerializeField] private AudioClip m_EffectSound;

    // 音を鳴らすためのAudioSource
    private AudioSource m_AudioSource;

    public void Start()
    {
        if (m_EffectSound == null)
        {
            Debug.LogError("エフェクト音が設定されていません。");
            return;
        }
        // AudioSourceコンポーネントを取得
        m_AudioSource = GetComponent<AudioSource>();
        if(m_AudioSource == null)
        {
            Debug.LogError("AudioSourceコンポーネントが見つかりませんでした。");
            return;
        }
    }

    // ===========================================
    // アクション処理
    // ===========================================
    public override async UniTask Execute(CancellationToken token)
    {
        m_AudioSource.PlayOneShot(m_EffectSound);

        await UniTask.Delay(System.TimeSpan.FromSeconds(m_EffectSound.length));
    }
}
