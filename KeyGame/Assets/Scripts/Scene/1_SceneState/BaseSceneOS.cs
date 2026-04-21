
/* @file 基底シーンの状態,受け渡す基底データ
 * @brief シーンの状態、受け渡すデータ基底インターフェイス
 * @memo これを派生させてシーン状態を作成する
 *       シーンが渡したいデータはISceneDataを継承して使用
 *       渡されたデータは、キャストして使用するようにしてください。
*/


// シーン間で渡すデータの基底
using UnityEngine;

public interface ISceneData { }


// シーンの状態基底
public abstract class BaseSceneOS : ScriptableObject
{
    // シーンの名前を渡す
    public abstract string GetSceneName();

    // 開始処理
    public abstract void Enter(ISceneData _data);
    // 終了処理
    public abstract void Exit();
}
