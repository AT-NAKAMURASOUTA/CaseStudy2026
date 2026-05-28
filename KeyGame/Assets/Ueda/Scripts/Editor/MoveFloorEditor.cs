using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

[CustomEditor(typeof(MoveFloor))]
public class MoveFloorEditor : Editor
{
    private ReorderableList reorderableList;
    private SerializedProperty movePositionProp;

    private void OnEnable()
    {
        // MovePositionのプロパティを取得
        movePositionProp = serializedObject.FindProperty("movePosition");

        // インスペクター上のリスト表示をカスタマイズ
        reorderableList = new ReorderableList(serializedObject, movePositionProp, true, true, true, true);

        // タイトルの描画
        reorderableList.drawHeaderCallback = (Rect rect) => {
            EditorGUI.LabelField(rect, "移動先の位置（複数の場合順番に移動する）");
        };

        // 各要素の描画
        reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
            var element = movePositionProp.GetArrayElementAtIndex(index);
            rect.y += 2;
            EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), element, new GUIContent($"Element {index}"));
        };

        // ★「＋」ボタンが押された時の処理を上書き
        reorderableList.onAddCallback = (ReorderableList list) => {
            int index = list.serializedProperty.arraySize;
            list.serializedProperty.InsertArrayElementAtIndex(index);

            // 追加された要素に、オブジェクト本体の現在座標を代入する
            var newElement = list.serializedProperty.GetArrayElementAtIndex(index);
            if (index == 0)
            {
                MoveFloor moveFloor = (MoveFloor)target;
                newElement.vector3Value = moveFloor.transform.position + Vector3.up;
            }
            else
            {
                var latestElement = list.serializedProperty.GetArrayElementAtIndex(index - 1);
                newElement.vector3Value = latestElement.vector3Value + Vector3.up;
            }
        };
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // MovePosition以外のプロパティ（targetTagsなど）を通常通り表示
        DrawPropertiesExcluding(serializedObject, "movePosition");

        // カスタマイズしたリストを表示
        reorderableList.DoLayoutList();

        serializedObject.ApplyModifiedProperties();
    }
}
