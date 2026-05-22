#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

/*  * ボタンレイアウトにつけるボタン
 */

[CustomEditor(typeof(BookLayout))]
public class BookLayoutEditor : Editor
{
    // ===========================================
    // メンバー変数
    // ===========================================
    private BookLayout m_BookLayout;


    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        // スクリプト取得
        BookLayout script = (BookLayout)target;

        // スペースを追加
        GUILayout.Space(10);

        // ボタン追加
        if (GUILayout.Button("レイアウト更新"))
        {
            script.Refresh();
        }
    }

}

#endif
