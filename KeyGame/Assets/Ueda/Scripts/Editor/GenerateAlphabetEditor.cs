using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GenerateAlphabet))]
public class GenerateAlphabetEditor : Editor
{
    //ドロップ時追加するか否か
    bool isAdd = false;

    public override void OnInspectorGUI()
    {
        // 元のインスペクター（spriteListの表示など）を描画
        DrawDefaultInspector();

        GenerateAlphabet manager = (GenerateAlphabet)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("アルファベットのシートを入れると自動でリストに設定されます。", EditorStyles.boldLabel);
        isAdd = EditorGUILayout.ToggleLeft("画像ドロップ時、追加するならtrue、リセットするならfalse", isAdd);

        // ドロップ先のエリア（Texture2Dを受け付ける）
        Texture2D droppedTexture = (Texture2D)EditorGUILayout.ObjectField(
            "Sheet to Extract:", null, typeof(Texture2D), false);

        // テクスチャがドロップされたら処理開始
        if (droppedTexture != null)
        {
            ExtractSprites(manager, droppedTexture);
        }
    }

    private void ExtractSprites(GenerateAlphabet manager, Texture2D texture)
    {
        // テクスチャからスプライトを抽出
        string path = AssetDatabase.GetAssetPath(texture);
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        if (!isAdd)
            manager.alphabetSprites.Clear();
        foreach (Object asset in assets)
        {
            if (asset is Sprite sprite)
            {
                manager.alphabetSprites.Add(sprite);
            }
        }
        // インスペクターを更新
        EditorUtility.SetDirty(manager);
        AssetDatabase.SaveAssets();
    }
}
