using Cysharp.Threading.Tasks;
using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;


/* @file シーン遷移を管理するマネージャー
 * @brief シーン遷移
 * @momo Stateパターンで実装する予定
 *       シングルトンで制作、シーンを跨いでも削除されないように制作
*/

public class SceneTransitionManager : MonoBehaviour
{
    // 実態
    private static SceneTransitionManager m_Instance;
    // 今のシーンの状態
    private BaseSceneOS m_CurrentState;
    // チェンジフラグ
    private bool m_IsTransitioning = false;
    // 初期化設定フラグ
    private bool m_IsInitialized = false;


    // ===============================
    // 最初のシーンを設定する
    // ===============================
    public void SetStartScene(BaseSceneOS _startScene, ISceneData _data = null)
    {
        if (m_IsInitialized) { return; }

        m_CurrentState = _startScene;
        m_CurrentState.Enter(_data);
    }


    // ===============================
    // シーン遷移処理
    // ===============================
    public bool SceneTransition(BaseSceneOS _scene, ISceneData _data = null)
    {
        if (m_IsTransitioning) { return false; } // シーン遷移中だと行わないようにする

        // コルーチン開始
        SceneTransitionRoutine(_scene, _data).Forget();

        return true;
    }


    // ================================
    //  自分の実態を渡すゲッター
    // ================================
    public static SceneTransitionManager GetInstance()
    {
        // 実態がなければ生成
        if (m_Instance == null)
        {
            // Emptyを作成
            var obj = new GameObject("SceneTransitionManager");
            // Emptyにアタッチする
            m_Instance = obj.AddComponent<SceneTransitionManager>();

            // シーン遷移時に破棄されないようにする
            DontDestroyOnLoad(obj);

            UnityEngine.Debug.Log("シーン遷移マネージャーが作成されました");

        }

        return m_Instance;
    }


    // =====================================
    // シーンを切り替える処理
    // =====================================
    private async UniTask SceneTransitionRoutine(BaseSceneOS _sceneType, ISceneData _data)
    {
        // 現在のシーンの終了処理
        if (m_CurrentState != null)
        {
            m_CurrentState.Exit();
        }

        // シーン作成のフラグ
        m_IsTransitioning = true;

        // 新しいシーン状態を生成
        m_CurrentState = _sceneType;

        // 次のシーンの名前を取得
        string nextSceneName = m_CurrentState.GetSceneName();
        // シーンをロード開始
        AsyncOperation async = SceneManager.LoadSceneAsync(nextSceneName, LoadSceneMode.Single);

        // ロードが終わるのを待つ
        while (!async.isDone)
        {
            await UniTask.Yield();  
        }

        // 新シーン開始処理
        m_CurrentState.Enter(_data);

        // フラグ解除
        m_IsTransitioning = false;
    }


    // ==================================
    // リターン処理
    // ==================================
    public void ReturenScene(ISceneData _data = null)
    {
        if (m_CurrentState != null)
        {
            // シーン変更
            SceneTransition(m_CurrentState, _data);
        }
        else
        {
            UnityEngine.Debug.Log("今のシーンに何も入っていない");
        }
    }

}
