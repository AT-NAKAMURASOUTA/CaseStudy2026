using NUnit.Framework;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI.Table;


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
    [Tooltip("ボタンの最大数")]
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

        // 配置順に応じて位置計算
        CalculateButtonPositionRight();
    }

    // ===========================================
    // 位置計算 (右)
    // ===========================================
    private void CalculateButtonPositionRight()
    {
        // 行数計算
        int rowCount = Mathf.CeilToInt((float)m_ButtonMaxNumber / (m_ButtonMaxCountX * 2));

        // １ページ内の横間隔
        float spacingX =
            (m_PageSize.x - (m_ButtonSize.x * m_ButtonMaxCountX)) / ((m_ButtonMaxCountX) + 1);

        // 縦間隔
        float spacingY =
            (m_PageSize.y - (m_ButtonSize.y * rowCount)) / (rowCount + 1);

        // ボタンの数
        int buttonNumber = m_ButtonMaxNumber;
        // ボタン計算終了フラグ
        bool end = false;

        // ボタンの開始位置
        float startX =
            (-m_PageSize.x / 2)
            + spacingX
            + (m_ButtonSize.x / 2);
        float startY =
            (m_PageSize.y / 2)
            - spacingY
            - (m_ButtonSize.y / 2);

        // 位置計算
        for (int j = 0; j < rowCount; j++)
        {
            // 縦位置
            float y = startY
                - j * (m_ButtonSize.y + spacingY);

            for (int i = 0; i < m_ButtonMaxCountX * 2; i++)
            {
                // インデックス
                int localIndex;
                // ページの中心位置
                Vector2 pageCentPos;
                if (i - m_ButtonMaxCountX < 0)
                {
                    pageCentPos = m_PageCenter;
                    localIndex = i;
                }
                else
                {
                    pageCentPos = new Vector2(-m_PageCenter.x, m_PageCenter.y);
                    localIndex = i - m_ButtonMaxCountX;
                }

                // 横位置
                float x = startX
                    + localIndex * (m_ButtonSize.x + spacingX);

                // ボタンの位置
                Vector2 pos = new Vector2(x, y) + pageCentPos;
                m_ButtonPositions.Add(pos);

                // ボタンの数が最大数に達したら終了
                buttonNumber--;
                if (buttonNumber == 0)
                {
                    end = true;
                }
            }

            // 終了
            if (end)
            {
                break;
            }
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

        // 本の境界線を描画
        DrawBookBoundary(rectTransform);

        // 本のページ範囲を描画
        DrawPageBoundary(rectTransform, PageSide.Left);
        DrawPageBoundary(rectTransform, PageSide.Right);

        // ボタン位置描画
        DrawButtonPosition(rectTransform);
    }

    // ===========================================
    // 本の境界線を描画する関数
    // ===========================================
    private void DrawBookBoundary(RectTransform rectTransform)
    {
        Gizmos.color = Color.red;


        Vector3 top =
            rectTransform.TransformPoint(
                new Vector3(0, 1000f, 0));

        Vector3 bottom =
            rectTransform.TransformPoint(
                new Vector3(0, -1000f, 0));

        // 描画
        Gizmos.DrawLine(top, bottom);
    }

    // ===========================================
    // 本の左ページ範囲を描画する関数
    // ===========================================
    private void DrawPageBoundary(RectTransform rectTransform, PageSide _side)
    {
        // ページの中心位置
        Vector2 localCenter =
                new Vector2(
                    m_PageCenter.x * (int)_side,
                    m_PageCenter.y);
        float halfX = m_PageSize.x * 0.5f;
        float halfY = m_PageSize.y * 0.5f;

        // ローカル座標で四隅作成
        Vector3 lt = new Vector3(
            localCenter.x - halfX,
            localCenter.y + halfY,
            0);
        Vector3 rt = new Vector3(
            localCenter.x + halfX,
            localCenter.y + halfY,
            0);
        Vector3 rb = new Vector3(
            localCenter.x + halfX,
            localCenter.y - halfY,
            0);
        Vector3 lb = new Vector3(
            localCenter.x - halfX,
            localCenter.y - halfY,
            0);

        // ワールド座標に変換
        lt = rectTransform.TransformPoint(lt);
        rt = rectTransform.TransformPoint(rt);
        rb = rectTransform.TransformPoint(rb);
        lb = rectTransform.TransformPoint(lb);

        // 色設定
        if (_side == PageSide.Left)
        {
            Gizmos.color = Color.blue;
        }
        else
        {
            Gizmos.color = Color.green;
        }

        // 2D四角形描画
        Gizmos.DrawLine(lt, rt);
        Gizmos.DrawLine(rt, rb);
        Gizmos.DrawLine(rb, lb);
        Gizmos.DrawLine(lb, lt);
    }

    // ============================================
    // ボタンの位置を計算
    // ============================================
    private void DrawButtonPosition(RectTransform rectTransform)
    {
        Gizmos.color = Color.yellow;

        foreach (Vector2 pos in m_ButtonPositions)
        {
            // ボタンの位置をワールド座標に変換
            Vector3 worldPos =
                rectTransform.TransformPoint(pos);
            Vector3 worldSize =
                rectTransform.TransformVector(m_ButtonSize);

            Gizmos.DrawWireCube(
                worldPos,
                worldSize);
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
    // ボタンのサイズを取得
    public Vector2 GetButtonSize()
    {
        return m_ButtonSize;
    }
}
