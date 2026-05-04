#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

/*  * ボタンレイアウトにつけるボタン
 */

[CustomEditor(typeof(ButtonLayout))]
public class ButtonLayoutEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        // スクリプト取得
        ButtonLayout script = (ButtonLayout)target;

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
