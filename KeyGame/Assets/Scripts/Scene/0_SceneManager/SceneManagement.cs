using UnityEngine;
//using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;

/* @file シーン遷移を管理するマネージャー
*/

public class SceneManagement : MonoBehaviour
{
    [Header("シーン管理配列")]
    [SerializeField] private BaseSceneOS[] m_Scenes;

    [Header("今のシーン デバッグ用")]
    [SerializeField] private int m_NumScenes;

    // 今のシーン
    private int m_CurentSceneNumber = 0;
    // 実態を入れる
    private static SceneManagement m_Instance;


    // ===================================
    // 初期化
    // ===================================
    private void Start()
    {
        // 今のシーン初期化
        m_CurentSceneNumber = 0;

#if UNITY_EDITOR
        // デバッグ用
        m_CurentSceneNumber = m_NumScenes;
        // シーン遷移
        SceneTransitionManager.GetInstance().SetStartScene(m_Scenes[m_CurentSceneNumber]);
#else
        // シーン遷移
        SceneTransitionManager.GetInstance().SetStartScene(m_Scenes[m_CurentSceneNumber]);
#endif
    }


#if UNITY_EDITOR
    // ==================================
    // デバッグ用
    // ==================================
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {

            NextScene();
        }

        if(Input.GetKeyDown(KeyCode.F2))
        {
            SceneTransitionManager.GetInstance().ReturenScene();
        }
    }
#endif


    // ==================================
    // 次のシーン
    // ==================================
    public void NextScene()
    {
        int nextSceneNumber = (m_CurentSceneNumber + 1) % m_Scenes.Length;

        if (!SceneTransitionManager.GetInstance().SceneTransition(m_Scenes[nextSceneNumber]))
        {
            Debug.Log(m_CurentSceneNumber);
            Debug.LogWarning("シーン遷移中のため、中断しました");
            return;
        }

        // 適応
        m_CurentSceneNumber = nextSceneNumber;
        Debug.Log(m_CurentSceneNumber);
    }


    // ==================================
    // 特定のシーンに移る
    // ==================================
    public void SpecificNextScenes(int _nextSceneNumberIndex)
    {
        if (_nextSceneNumberIndex < 0 || _nextSceneNumberIndex >= m_Scenes.Length)
        {
            Debug.LogError("無効なシーンナンバーが渡されました");
            return;
        }


        if (!SceneTransitionManager.GetInstance().SceneTransition(m_Scenes[_nextSceneNumberIndex]))
        {
            Debug.Log(m_CurentSceneNumber);
            Debug.LogWarning("シーン遷移中のため、中断しました");
            return;
        }

        // 適応
        m_CurentSceneNumber = _nextSceneNumberIndex;
        Debug.Log(m_CurentSceneNumber);
    }


    // ================================
    //  自分の実態を渡すゲッター
    // ================================
    public static SceneManagement GetInstance()
    {
        // 実態がなければ生成
        if (m_Instance == null)
        {
            // Prefabをロード
            var prefab = Resources.Load<GameObject>("0_SceneManager/SceneManagerObj");
            // オブジェクトを生成
            var obj = Object.Instantiate(prefab);
            // アタッチされているコンポーネントを取得
            m_Instance = obj.GetComponent<SceneManagement>();

            // シーン遷移時に破棄されないようにする
            DontDestroyOnLoad(obj);

            UnityEngine.Debug.Log("シーンマネージャーが作成されました");

        }

        return m_Instance;
    }
}
