using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "SceneData")]

public class SceneData : ScriptableObject
{
    [Header("シーン情報")]
    public SceneInfo[] sceneInfos;

    // ====================================================
    // エディタ上でDataが更新されたときに呼ぶ処理
    // ・シーンタイプの重複チェック
    // ・DEBUGの手動登録禁止
    // ====================================================
#if UNITY_EDITOR
    private void OnValidate()
    {
        HashSet<SCENETYPE> usedTypes = new HashSet<SCENETYPE>();

        foreach (var info in sceneInfos)
        {
            // DEBUGは禁止
            if (info.sceneType == SCENETYPE.DEBUG)
            {
                Debug.LogError("DEBUG は手動登録できません。");
            }

            // 重複チェック
            if (!usedTypes.Add(info.sceneType))
            {
                Debug.LogError($"{info.sceneType} が重複しています。");
            }
        }
    }
#endif
}
