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
            m_PlayerInput.actions["Move"].Disable();
            m_PlayerInput.actions["Jump"].Disable();
        }
        else
        {
            // メニューが非アクティブなときはPlayerInputのMoveとJumpを有効にする
            m_PlayerInput.actions["Move"].Enable();
            m_PlayerInput.actions["Jump"].Enable();
        }
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
