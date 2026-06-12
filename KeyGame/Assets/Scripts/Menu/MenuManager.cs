using UnityEngine;
using UnityEngine.InputSystem;

/*
 *  * メニューシーンの管理を行うクラス
 */

[RequireComponent(typeof(PlayerInput))]
public class MenuManager : MonoBehaviour
{
    // メニューキャンバス
    [SerializeField] private GameObject m_MenuPrefab;
    // Guide を最初に表示するか
    [SerializeField] private bool m_IsGuideActiveAtStart = false;
    // Group
    [SerializeField] CanvasGroup m_CanvasGroup;

    // PlayerInput
    private PlayerInput m_PlayerInput;
    // 作成したメニュー
    private GameObject m_Menu;
    // メニューのCG
    private CanvasGroup m_MenuCanvasGroup;
    // メニューがアクティブかどうか
    private bool m_IsMenuActive = false;
    // メニューを操作するかのフラグ
    private bool m_CanOperateMenu = true;

    // 初期化
    void Start()
    {
        m_PlayerInput = GetComponent<PlayerInput>();

        // メニューを作成
        m_Menu = Instantiate(m_MenuPrefab);
        m_Menu.SetActive(false);
        m_MenuCanvasGroup = m_Menu.GetComponent<CanvasGroup>();
        m_Menu.GetComponentInChildren<Action_GuideActive>().CreateGuidePrefab(this, m_IsGuideActiveAtStart);

        // PlayerInputのイベントに関数を登録
        m_PlayerInput.actions["Menu"].performed += _ => Menu();
    }

    void Menu()
    {
        Debug.Log("Menuボタンが押されました。");
        if (!m_CanOperateMenu) return;

        m_IsMenuActive = !m_IsMenuActive;

        // メニューの表示/非表示を切り替える
        m_Menu.SetActive(m_IsMenuActive);

        if (m_IsMenuActive)
        {
            // メニューがアクティブなときはPlayerInputのMoveとJumpを無効にする
            DisableInputSystem("Move");
            DisableInputSystem("Jump");
            DisableInputSystem("NextPage");
            DisableInputSystem("PreviousPage");
            if(m_CanvasGroup != null)
            {
                m_CanvasGroup.interactable = false;
                m_CanvasGroup.blocksRaycasts = false;
            }
        }
        else
        {
            // メニューが非アクティブなときはPlayerInputのMoveとJumpを有効にする
            EnableInputSystem("Move");
            EnableInputSystem("Jump");
            EnableInputSystem("NextPage");
            EnableInputSystem("PreviousPage");
            if(m_CanvasGroup != null)
            {
                m_CanvasGroup.interactable = true;
                m_CanvasGroup.blocksRaycasts = true;
            }
        }
    }

    void DisableInputSystem(string actionName)
    {
        m_PlayerInput.actions[actionName].Disable();
    }

    void EnableInputSystem(string actionName)
    {
        m_PlayerInput.actions[actionName].Enable();
    }

    // ===========================================
    // セッター
    // ===========================================
    public void SetCanOperateMenu(bool canOperate)
    {
        m_CanOperateMenu = canOperate;
    }

    // ===========================================
    // メニューを有効化する
    // ===========================================
    public void ActivateMenuButton()
    {
        m_MenuCanvasGroup.interactable = true;
        m_MenuCanvasGroup.blocksRaycasts = true;
    }
}
