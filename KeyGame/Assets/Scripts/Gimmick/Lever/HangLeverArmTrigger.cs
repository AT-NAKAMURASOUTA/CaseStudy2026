using UnityEngine;

public sealed class HangLeverArmTrigger : MonoBehaviour
{
    // このTriggerが通知する先のレバー本体
    private HangLever m_Owner;

    public void SetOwner(HangLever owner)
    {
        m_Owner = owner;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 文字がTriggerに入ったことをレバー側に知らせる
        m_Owner?.RegisterHangCandidate(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // 文字がTriggerから出たことをレバー側に知らせる
        m_Owner?.UnregisterHangCandidate(other);
    }
}
