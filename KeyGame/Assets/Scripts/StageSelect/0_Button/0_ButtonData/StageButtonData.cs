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
}
