using UnityEngine;

[RequireComponent(typeof(SoundPlayer))]
public class ButtonSound : MonoBehaviour
{
    // ================================
    // メンバー変数
    // ================================
    private SoundPlayer m_SoundPlayer;
    private SwitchCollision m_SwitchCollision;
    // 前回のスイッチの状態を保存する変数
    private bool m_OldSwitchON = false;

    // ================================
    // 初期化
    // ================================
    void Start()
    {
        m_SoundPlayer = GetComponent<SoundPlayer>();
        m_SwitchCollision = GetComponentInChildren<SwitchCollision>();
        if(m_SwitchCollision == null)
        {
            Debug.LogError("SwitchCollisionコンポーネントが見つかりませんでした。");
            return;
        }
    }

    // ================================
    // 更新
    // ================================
    private void Update()
    {
        // スイッチが押された瞬間を検出して、SEを再生する
        if (m_SwitchCollision.GetCollisionFlag() && !m_OldSwitchON)
        {
            m_SoundPlayer.PlaySE();
        }

        // フラグ更新
        m_OldSwitchON = m_SwitchCollision.GetCollisionFlag();
    }
}
