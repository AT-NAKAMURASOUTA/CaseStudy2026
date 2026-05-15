using UnityEngine;


/*  * ボタンを配置するためのスクリプト
 *  * ボタンの配置、サイズ、間隔などを管理
 */

public class BookLayout : MonoBehaviour
{
    // ===========================================
    // メンバー変数
    // ===========================================
    [Header("本のページ設定")]
    [Tooltip("ページサイズ")]
    public Vector2 m_PageSize = new Vector2(750, 700);
    [Tooltip("左ページの中心位置")]
    public Vector2 m_PageCenter = new Vector2(-425, 0);
    [Tooltip("ページ内のボタン横最大数")]
    [SerializeField] private int m_ButtonMaxCountX = 3;

    [Header("ボタン設定")]
    [Tooltip("ボタンを押したときのシーン設定配列")]
    [SerializeField] private SCENETYPE[] m_NextScene;
    [Tooltip("ボタンサイズ")]
    [SerializeField] private Vector2 m_ButtonSize = new Vector2(100, 50);

    [Header("Gizmos 設定")]
    [Tooltip("左ページ描画")]
    [SerializeField] private bool m_DrawLeftPage = true;
    [Tooltip("右ページ描画")]
    [SerializeField] private bool m_DrawRightPage = true;


    // ===========================================
    // 更新
    // ===========================================
    [ContextMenu("更新")]
    public void Refresh()
    {
        // エラーチェック
        if(m_PageSize.x < m_ButtonMaxCountX * m_ButtonSize.x)
        {
            UnityEngine.Debug.LogError("ページサイズがボタンの合計幅より小さいです。");
            return;
        }
        if(m_NextScene.Length == 0)
        {
            UnityEngine.Debug.LogError("シーン設定が空です。");
            return;
        }


    }

    // ===========================================
    // ボタンの位置を計算する関数
    // ===========================================
    private Vector3 ButtonPosition(int index)
    {
        // ページ内のローカルインデックスを計算
        int col = index % m_ButtonMaxCountX;
        int row = index / m_ButtonMaxCountX;

        int rows = Mathf.CeilToInt((float)m_NextScene.Length / m_ButtonMaxCountX);

        // ボタンの横配置に必要なスペースを計算
        float freeSpaceX = m_PageSize.x - m_ButtonMaxCountX * m_ButtonSize.x;
        float spacingX = freeSpaceX / (m_ButtonMaxCountX - 1);

        // ボタンの縦配置に必要なスペースを計算
        float freeSpaceY = m_PageSize.y - m_ButtonSize.y;
        float spacingY = freeSpaceY * 0.5f;

        // ページの左上を基準にして、ボタンの開始位置を計算
        float startX = m_PageCenter.x - m_PageSize.x * 0.5f;
        float startY = m_PageCenter.y + m_PageSize.y * 0.5f;

        float x = startX
            + spacingX * (col + 1)
            + m_ButtonSize.x * col;

        float y = startY - row * m_ButtonSize.y;

        return new Vector3(x, y, 0);
    }

    // ===========================================
    // ギズモ描画 (ボタン位置を可視化)
    // ===========================================
    private void OnDrawGizmos()
    {
        // キャンバスのRectTransformを取得
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null) return;

        // キャンバスのローカル座標の中心をワールド座標に変換
        Vector3 center = rectTransform.TransformPoint(Vector3.zero);

        // 本の境界線を描画
        DrawBookBoundary(center);
        
        // 本のページ範囲を描画
        if(m_DrawLeftPage) DrawLeftPageBoundary(center);
        if(m_DrawRightPage) DrawRightPageBoundary(center);
    }

    // ===========================================
    // 本の境界線を描画する関数
    // ===========================================
    private void DrawBookBoundary(Vector3 center)
    {
        // 境界線ライン描画
        Vector3 top = center + Vector3.up * 1000f;
        Vector3 bottom = center + Vector3.down * 1000f;

        // 描画
        Gizmos.color = Color.red;
        Gizmos.DrawLine(top, bottom);
    }

    // ===========================================
    // 本の左ページ範囲を描画する関数
    // ===========================================
    private void DrawLeftPageBoundary(Vector3 center)
    {
        // 左ページの中心位置を計算
        Vector3 leftcenter = center + new Vector3(m_PageCenter.x, m_PageCenter.y, 0);

        // サイズを取得
        Vector3 size = new Vector3(m_PageSize.x, m_PageSize.y, 0);

        // 描画
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(leftcenter, size);
    }

    // ==========================================
    // 本の右ページ範囲を描画する関数
    // ==========================================
    private void DrawRightPageBoundary(Vector3 center)
    {
        // 右ページの中心位置を計算
        Vector3 rightcenter = center + new Vector3(-m_PageCenter.x, m_PageCenter.y, 0);

        // サイズを取得
        Vector3 size = new Vector3(m_PageSize.x, m_PageSize.y, 0);

        // 描画
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(rightcenter, size);
    }
}
