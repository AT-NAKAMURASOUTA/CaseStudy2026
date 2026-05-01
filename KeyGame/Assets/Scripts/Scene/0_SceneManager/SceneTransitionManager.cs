using Cysharp.Threading.Tasks;
using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;


/* @file シーン遷移を管理するマネージャー
 * @brief シーン遷移
 * @momo Stateパターンで実装
 *       シングルトンで制作、シーンを跨いでも削除されないように制作
 *       現在は、Dictionaryを使っているため少し重いかもしれない、今後はEnumを添え字に使う可能性あり
*/

public class SceneTransitionManager : MonoBehaviour
{
    // 実態
    private static SceneTransitionManager m_Instance;
    // シーンデータを取得
    private Dictionary<SCENETYPE,string> m_SceneData = new Dictionary<SCENETYPE, string>();
    // 今のシーンの状態
    private SCENETYPE m_CurrentSceneType;
    // チェンジフラグ
    private bool m_IsTransitioning = false;


    // ===============================
    // 初期化設定
    // ===============================
    public void Init(SceneData _sceneData)
    {
#if UNITY_EDITOR
        bool IsInitDataNull = true;
#endif

        // Dictionaryを初期化
        m_SceneData.Clear();

        // ListからDictionaryに変換
        foreach (SceneInfo sceneInfo in _sceneData.sceneInfos)
        {
            m_SceneData.Add(sceneInfo.sceneType, sceneInfo.scene.name);

#if UNITY_EDITOR
            // 現在のシーンを設定
            String sceneName = SceneManager.GetActiveScene().name;
            if (sceneInfo.scene.name == sceneName)
            {
                m_CurrentSceneType = sceneInfo.sceneType;
                IsInitDataNull = false;

                UnityEngine.Debug.Log($"現在のシーン: {sceneName} に対応するシーンタイプ: {sceneInfo.sceneType} が設定されました");
            }
#endif
        }

#if UNITY_EDITOR
        // シーン初期化データが見つからない場合はエラー
        if (IsInitDataNull)
        {
            UnityEngine.Debug.LogError("シーン初期化データが見つかりません");
        }
#else
        // タイトルシーンを設定
        m_CurrentSceneType = SCENETYPE.TITLE;
#endif
    }

    // ===============================
    // シーン遷移処理
    // ===============================
    public bool SceneTransition(SCENETYPE _sceneType)
    {
        // シーン遷移中だと行わないようにする
        if (m_IsTransitioning) { return false; }

        // コルーチン開始
        SceneTransitionRoutine(_sceneType).Forget();

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
    private async UniTask SceneTransitionRoutine(SCENETYPE _nextScene)
    {
        // シーン作成のフラグ
        m_IsTransitioning = true;

        // 現在のシーンを設定
        m_CurrentSceneType = _nextScene;

        // シーン文字列を取得
        string nextSceneName = m_SceneData[_nextScene];

        // シーンをロード開始
        AsyncOperation async = SceneManager.LoadSceneAsync(nextSceneName, LoadSceneMode.Single);

        // ロードが終わるのを待つ
        while (!async.isDone)
        {
            await UniTask.Yield();  
        }

        // フラグ解除
        m_IsTransitioning = false;
    }

    // ==================================
    // リターン処理
    // ==================================
    public void ReturenScene()
    {
        SceneTransition(m_CurrentSceneType);
    }

}
