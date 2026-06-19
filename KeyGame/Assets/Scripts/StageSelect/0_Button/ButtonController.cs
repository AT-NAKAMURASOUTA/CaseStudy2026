using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/*
 * ボタンの音を鳴らすスクリプト
 */

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(Button))]
[RequireComponent(typeof(ActionsManager))]
public class ButtonController : MonoBehaviour, IPointerEnterHandler
{
    // ======================================
    // メンバー変数
    // =======================================
    // 鳴らす音
    [Header("音")]
    [Tooltip("選択音")]
    [SerializeField] private AudioClip m_SelectSound;
    [Tooltip("決定音")]
    [SerializeField] private AudioClip m_DecisionSound;


    // 音を鳴らすためのAudioSource
    private AudioSource m_AudioSource;
    // アクション管理マネージャー
    private ActionsManager m_ActionsManager;
    // ボタン
    private Button m_Button;
    // キャンバスグループ
    private CanvasGroup m_CGroup;
    // PlayerInput取得
    private PlayerInput m_PlayerInput = null;

    // ======================================
    // 初期化
    // =======================================
    void Start()
    {
        // AudioSourceコンポーネントを取得
        m_AudioSource = GetComponent<AudioSource>();
        if (m_AudioSource == null)
        {
            Debug.LogWarning("AudioSourceコンポーネントが見つかりませんでした。");
        }
        // キャンバスグループを取得
        m_CGroup = GetComponentInParent<CanvasGroup>();
        if (m_CGroup == null)
        {
            Debug.LogWarning("CanvasGroupが設定されていません。");
        }

        // PlayerInputコンポーネントを取得
        m_PlayerInput = GetComponentInParent<PlayerInput>();
        if (m_PlayerInput == null)
        {
            Debug.LogWarning("PlayerInputコンポーネントが見つかりませんでした。");
        }

        m_Button = GetComponent<Button>();
        if (m_Button == null)
        {
            Debug.LogError("ボタンが見つかりませんでした");
            return;
        }
        if (m_SelectSound == null || m_DecisionSound == null)
        {
            Debug.LogError("音が設定されていません。");
            return;
        }


        // 念のためデコードしておく
        m_SelectSound.LoadAudioData();
        m_DecisionSound.LoadAudioData();

        // アクション管理マネージャーを取得
        m_ActionsManager = GetComponent<ActionsManager>();
        if (m_ActionsManager == null)
        {
            Debug.LogError("ActionsManagerコンポーネントが見つかりませんでした。");
            return;
        }

        // 自分の処理を登録
        m_Button.onClick.AddListener(OnClick);

    }

    // ======================================
    // カーソルがボタンに乗ったときの処理
    // ======================================
    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log("選択サウンド再生");
        m_AudioSource.PlayOneShot(m_SelectSound);
    }
    // ======================================
    // ボタンが押されたときの処理
    // ======================================
    public async void OnClick()
    {
        Debug.Log("決定サウンド再生");

        if(m_CGroup != null)
        {
            // ボタンを押したときにキャンバスグループのインタラクションを無効にする
            m_CGroup.interactable = false;
            // ボタンを押したときにキャンバスグループのレイキャストを無効にする
            m_CGroup.blocksRaycasts = false;
        }

        // ボタンを押したときにMenu アクションを無効にする
        if (m_PlayerInput != null)
        {
            m_PlayerInput.actions["Menu"].Disable();
        }

        // 音を鳴らす
        await PlayDecisionSoundAsync();

        // アクション管理マネージャーの処理を呼び出す
        if (m_ActionsManager != null)
        {
            m_ActionsManager.ExecuteAction();
        }
    }

    // ======================================
    // 決定音を鳴らす処理
    // ======================================
    public async UniTask PlayDecisionSoundAsync()
    {
        m_AudioSource.PlayOneShot(m_DecisionSound);

        await UniTask.Delay(System.TimeSpan.FromSeconds(m_DecisionSound.length));

    }

}
