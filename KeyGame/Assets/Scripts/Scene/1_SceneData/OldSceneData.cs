using UnityEngine;

/*
 * 前のシーンを保存しておくクラス
 * シーンセレクトのページ初期化で使用予定
 */

public class OldSceneData
{
    private static SCENETYPE m_OldSceneType = SCENETYPE.TITLE;

    static public void SetOldScene(SCENETYPE _type)
    {
        m_OldSceneType = _type;
    }

    static public SCENETYPE GetOldScene()
    {
        return m_OldSceneType;
    }
}
