using UnityEngine;

[System.Serializable]
public class ButtonData
{
    [Tooltip("ボタン画像")]
    public Texture2D Texture;
    [Tooltip("遷移先シーン")]
    public SCENETYPE NextScene;
}
