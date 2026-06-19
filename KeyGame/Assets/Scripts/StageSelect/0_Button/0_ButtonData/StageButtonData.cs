using System.Collections.Generic;
using UnityEngine;

// １ステージボタン
[CreateAssetMenu(
    fileName = "Stage1_Data",
    menuName = "Stage Button Data")]
public class StageButtonData : ScriptableObject
{
    // ステージのデータリスト
    public List<ButtonData> stageDataList;

#if UNITY_EDITOR
    // ==========================
    // 不具合検証
    // ==========================
    private void OnValidate()
    {
        if (stageDataList == null || stageDataList.Count == 0) return;

        // 重複と連番のチェック
        HashSet<SCENETYPE> usedScenes = new();
        for (int i = 0; i < stageDataList.Count; i++)
        {
            SCENETYPE scene = stageDataList[i].NextScene;

            // 重複チェック
            if (!usedScenes.Add(scene))
            {
                Debug.LogError($"{name}: {scene} が重複しています。", this);
                return;
            }

            // 連番チェック
            int expected = (int)stageDataList[0].NextScene + i;
            int actual = (int)scene;
            if (actual != expected)
            {
                Debug.LogWarning(
                    $"{name}: {scene} が連番になっていません。確認してください " +
                    $"期待値={(SCENETYPE)expected}",
                    this);
            }
        }
    }
#endif
}
