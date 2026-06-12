using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class Action_ZoomCamera: BaseAction
{
    // ===========================================
    // メンバー変数
    // ===========================================
    [Header("ズーム設定")]
    [Tooltip("ズーム時間")]
    [SerializeField] private float m_ZoomTime;
    [Tooltip("ズーム補間率")]
    [SerializeField, Range(0f, 1f)]
    private float m_ApproachRate = 0.8f;

    private Camera m_Camera; 
    private Vector3 m_TargetPos;

    // ===========================================
    // 初期化
    // ===========================================
    public void Start()
    {
        // カメラの取得
        m_Camera = Camera.main;
    }

    // ===========================================
    // アクション実行
    // ===========================================
    public override async UniTask Execute(CancellationToken token)
    {
        // 開始状態を取得
        Vector3 startPos = m_Camera.transform.position;

        // カメラからターゲットのベクトルを計算
        Vector3 toTarget = transform.position - m_Camera.transform.position;

        // 最終位置
        float targetZ =
            Mathf.Lerp(
                m_Camera.transform.position.z,
                transform.position.z,
                m_ApproachRate);

        m_TargetPos = new Vector3(
            transform.position.x,
            transform.position.y,
            targetZ);

        Debug.Log($"Zoom Start: Position={startPos}");
        // 時間
        float elapsed = 0f;

        while (elapsed < m_ZoomTime)
        {
            token.ThrowIfCancellationRequested();

            // 経過時間の更新
            elapsed += Time.deltaTime;

            // 0~1の範囲で補間値を計算
            float t = Mathf.Clamp01(elapsed / m_ZoomTime);

            // カメラのサイズと位置を補間して更新
            m_Camera.transform.position =
                Vector3.Lerp(startPos, m_TargetPos, t);

            // 次のフレームまで待機
            await UniTask.Yield(token);
        }

        // 最終的な状態を確実に設定
        m_Camera.transform.position = m_TargetPos;
    }
}
