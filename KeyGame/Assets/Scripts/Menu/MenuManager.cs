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

    // PlayerInput
    private PlayerInput m_PlayerInput;
    // 作成したメニュー
    private GameObject m_Menu;
    // メニューがアクティブかどうか
    private bool m_IsMenuActive = false;

    // 初期化
    void Start()
    {
        m_PlayerInput = GetComponent<PlayerInput>();

        // メニューを作成
        m_Menu = Instantiate(m_MenuPrefab);
        m_Menu.SetActive(false);

        // PlayerInputのイベントに関数を登録
        m_PlayerInput.actions["Menu"].performed += _ => Menu();
    }

    void Menu()
    {
        Debug.Log("Menuボタンが押されました。");

        m_IsMenuActive = !m_IsMenuActive;

        // メニューの表示/非表示を切り替える
        m_Menu.SetActive(m_IsMenuActive);

        if(m_IsMenuActive)
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
}
