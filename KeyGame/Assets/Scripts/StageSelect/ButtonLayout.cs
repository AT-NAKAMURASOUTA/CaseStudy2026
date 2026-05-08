using UnityEngine;
using UnityEngine.UI;


/*  * ボタンを配置するためのスクリプト
 */

public class ButtonLayout : MonoBehaviour
{
    // ===========================================
    // メンバー変数
    // ===========================================
    [Header("余白")]
    [SerializeField] private Vector2 m_Padding = new Vector2(10, 10);
    [Header("ボタンサイズ")]
    [SerializeField] private Vector2 m_ButtonSize = new Vector2(100, 50);
    [Header("ボタン間の間隔")]
    [SerializeField] private float m_Spacing = 10f;
    [Header("ボタン最大数")]
    [SerializeField] private int m_ButtonMaxCount = 3;
    [Header("ボタンPrefab")]
    [SerializeField] private Button m_ButtonPrefab;


    // ===========================================
    // 更新
    // ===========================================
    [ContextMenu("更新")]
    public void Refresh()
    {
        // 子オブジェクト削除
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }

        // LayoutGroup取得 or 追加
        var layout = GetComponent<GridLayoutGroup>();
        if (layout == null)
            layout = gameObject.AddComponent<GridLayoutGroup>();

        layout.cellSize = m_ButtonSize;
        layout.spacing = new Vector2(m_Spacing, m_Spacing);
        layout.padding = new RectOffset(
            (int)m_Padding.x,
            (int)m_Padding.x,
            (int)m_Padding.y,
            (int)m_Padding.y
        );

        // ボタン生成
        for (int i = 0; i < m_ButtonMaxCount; i++)
        {
            var btn = Instantiate(m_ButtonPrefab, transform);
            btn.name = $"Button_{i}";
        }
    }
}
