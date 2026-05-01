using UnityEngine;

/*  * シーン遷移設定用のオブジェクト
 *  * リリース時と、デバッグ時で、シーン遷移の挙動を変えるための設定オブジェクト
 */

// ===========================================
// ビルドモード
// ===========================================
public enum BuildMode
{
    DEBUG,
    RELEASE
}

[CreateAssetMenu(menuName = "Game/Scene Config")]
public class SceneConfig : ScriptableObject
{
    [SerializeField] private BuildMode buildMode;
    
    [SerializeField] private SceneData debugData;
    [SerializeField] private SceneData releaseData;

    public SceneData GetSceneData()
    {
        return buildMode == BuildMode.DEBUG ? debugData : releaseData;
    }
}
