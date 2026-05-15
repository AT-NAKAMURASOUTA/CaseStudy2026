using UnityEngine;

/*  * ボタンにつけるデータクラス
 */

[System.Serializable]
public class ButtonData
{
    [Tooltip("ボタン画像")]
    public Sprite Texture;
    [Tooltip("遷移先シーン")]
    public SCENETYPE NextScene;
}
