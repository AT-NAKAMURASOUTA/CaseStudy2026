using UnityEditor;
using UnityEngine;

/*  * シーン情報クラス
 */

[System.Serializable]
public class SceneInfo
{
    public SCENETYPE sceneType;
    [HideInInspector] public string sceneName;

#if UNITY_EDITOR
    public SceneAsset scene;
#endif
}
