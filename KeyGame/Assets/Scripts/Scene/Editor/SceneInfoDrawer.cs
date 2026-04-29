#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using System.Linq;

/*  * インスペクター上のSceneInfoの表示をカスタマイズするためのクラス
 */

// SceneInfoのインスペクター上の表示を変更する
[CustomPropertyDrawer(typeof(SceneInfo))]

public class SceneInfoDrawer : PropertyDrawer
{
    // ================================================================================
    // インスペクター上のSceneInfoの表示をカスタマイズする
    // ================================================================================
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // プロパティの開始
        EditorGUI.BeginProperty(position, label, property);

        // sceneType と sceneName のプロパティを取得
        SerializedProperty sceneTypeProp = property.FindPropertyRelative("sceneType");
        SerializedProperty sceneNameProp = property.FindPropertyRelative("sceneName");

        // レイアウトの設定
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = 4f;

        // sceneType と sceneName の表示位置を設定
        Rect typeRect = new Rect(
            position.x,
            position.y,
            position.width,
            lineHeight);

        Rect nameRect = new Rect(
            position.x,
            position.y + lineHeight + spacing,
            position.width,
            lineHeight);

        // DEBUG を除いた enum 一覧を作成
        var values = Enum.GetValues(typeof(SCENETYPE))
            .Cast<SCENETYPE>()
            .Where(x => x != SCENETYPE.DEBUG)
            .ToArray();

        // enum の名前を文字列の配列に変換
        string[] names = values.Select(x => x.ToString()).ToArray();

        // 現在の sceneType のインデックスを取得
        int currentIndex = Array.IndexOf(values, (SCENETYPE)sceneTypeProp.enumValueIndex);
        if (currentIndex < 0) currentIndex = 0;

        // Popup を表示して、選択されたインデックスを取得
        int selectedIndex = EditorGUI.Popup(
            typeRect,
            "Scene Type",
            currentIndex,
            names);

        // 選択された値を保存
        sceneTypeProp.enumValueIndex = (int)values[selectedIndex];

        // sceneName を表示
        EditorGUI.PropertyField(nameRect, sceneNameProp);

        // プロパティの終了
        EditorGUI.EndProperty();
    }

    // ================================================================================
    // Unityに２行分のスペース(SceneType・SceneName)を確保するように指示する
    // ================================================================================
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight * 2 + 4.0f;
    }
}
#endif
