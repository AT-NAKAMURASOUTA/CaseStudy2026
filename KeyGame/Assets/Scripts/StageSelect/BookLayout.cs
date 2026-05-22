using NUnit.Framework;
using UnityEngine;


/*  * ボタンをの位置を計算するスクリプト
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
    [Tooltip("1ページ内のボタン横最大数")]
    [SerializeField] private int m_ButtonMaxCountX = 3;
    [Tooltip("1ページ内のボタンの最大数")]
    [SerializeField] private int m_ButtonMaxNumber = 6;

    [Header("ボタン設定")]
    [Tooltip("ボタンサイズ")]
    [SerializeField] private Vector2 m_ButtonSize = new Vector2(100, 50);

    // ボタンの位置リスト
    private System.Collections.Generic.List<Vector2> m_ButtonPositions = new();

    enum PageSide
    {
        Left = -1,
        Right = 1
    }

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

        // ボタンの位置計算
        CalculateButtonPositions();
    }

    // ===========================================
    // 値が変更されたときに自動で更新
    // ===========================================
    private void OnValidate()
    {
        Refresh();
    }

    // ===========================================
    // ボタンの位置を計算する関数
    // ===========================================
    private void CalculateButtonPositions()
    {
        // 初期化
        m_ButtonPositions.Clear();

        // 行数計算
        int rowCount = Mathf.CeilToInt((float)m_ButtonMaxNumber / m_ButtonMaxCountX);
        // 横間隔
        float spacingX =
            (m_PageSize.x - (m_ButtonSize.x * m_ButtonMaxCountX)) / (m_ButtonMaxCountX - 1);
        // 縦間隔
        float spacingY =
            (m_PageSize.y - (m_ButtonSize.y * rowCount)) / (rowCount - 1);

        // 位置計算
        for (int i = 0; i < m_ButtonMaxNumber; i++)
        {
            // Button添え字を取得
            int col = i % m_ButtonMaxCountX;
            int row = i / m_ButtonMaxCountX;

            // 左上基準
            float startX = -m_PageSize.x / 2;
            float startY = m_PageSize.y / 2;

            float x =
                startX
                + (m_ButtonSize.x / 2)
                + col * (m_ButtonSize.x + spacingX);

            float y =
                startY
                - (m_ButtonSize.y / 2)
                - row * (m_ButtonSize.y + spacingY);

            Vector2 pos = new Vector2(x, y);

            m_ButtonPositions.Add(pos);
        }
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
        DrawPageBoundary(center, PageSide.Left);
        DrawPageBoundary(center, PageSide.Right);

        // ボタン位置描画
        DrawButtonPosition(PageSide.Left);
        DrawButtonPosition(PageSide.Right);
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
    private void DrawPageBoundary(Vector3 center, PageSide _side)
    {
        // 左ページの中心位置を計算
        Vector3 pageCenter = center + new Vector3(m_PageCenter.x * (int)_side, m_PageCenter.y, 0);

        // サイズを取得
        Vector3 size = new Vector3(m_PageSize.x, m_PageSize.y, 0);

        // 描画
        if(_side == PageSide.Left)
        {
             Gizmos.color = Color.blue;
        }
        else
        {
             Gizmos.color = Color.green;
        }
        Gizmos.DrawWireCube(pageCenter, size);
    }

    // ============================================
    // ボタンの位置を計算
    // ============================================
    private void DrawButtonPosition(PageSide _side)
    {
        Gizmos.color = Color.yellow;

        Vector2 startPos = new(m_PageCenter.x * (int)_side, m_PageCenter.y);

        foreach (Vector2 pos in m_ButtonPositions)
        {
            Gizmos.DrawWireCube(
                transform.position + (Vector3)pos + (Vector3)startPos,
                m_ButtonSize);
        }
    }

    // ===========================================
    // ゲッター
    // ===========================================
    // ボタンの位置リストを取得
    public System.Collections.Generic.List<Vector2> GetButtonPosition()
    {
        return m_ButtonPositions;
    }
    // ボタンの最大数を取得
    public int GetButtonMaxNumber()
    {
        return m_ButtonMaxNumber;
    }
}
