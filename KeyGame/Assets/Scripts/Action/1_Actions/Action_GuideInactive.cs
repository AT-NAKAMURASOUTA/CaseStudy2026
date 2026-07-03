using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Action_GuideInactive : BaseAction
{
    public UnityEvent m_OnInitialGuideHidden;

    // メニューマネージャー
    MenuManager m_MenuManager;
    GameObject m_GuideObject;
    CanvasGroup m_GuideCanvasGroup;
    PlayerInput m_PlayerInput;
    Button m_Button;

    public void Init(MenuManager _menu, GameObject _guide,PlayerInput _playerInput)
    {
        m_MenuManager = _menu;
        m_GuideObject = _guide;
        m_GuideCanvasGroup = m_GuideObject.GetComponent<CanvasGroup>();
        m_PlayerInput = _playerInput;
        m_PlayerInput.actions["GuideButton"].performed += OnDecision;
        m_Button = gameObject.GetComponent<Button>();
        if(m_Button == null )
        {
            Debug.LogError("ボタンが見つかりませんでした");
        }

        Debug.Log("Action_GuideInactiveの初期化");
    }

    public void GuideActive()
    {
        // ガイドを表示する処理
        m_MenuManager.SetCanOperateMenu(false);
        m_MenuManager.DeactivateMenuButton();
        m_GuideCanvasGroup.interactable = true;
        m_GuideCanvasGroup.blocksRaycasts = true;

        Debug.Log("ガイドを表示する処理");
    }

    public override UniTask Execute(CancellationToken token)
    {
        // ガイドを閉じる処理
        m_MenuManager.SetCanOperateMenu(true);
        m_MenuManager.ActivateMenuButton();
        m_GuideObject.SetActive(false);
        m_OnInitialGuideHidden?.Invoke();

        Debug.Log("ガイドを閉じる処理");

        return UniTask.CompletedTask;
    }

    private void OnEnable()
    {
        if (m_PlayerInput != null)
        {
            m_PlayerInput.actions["GuideButton"].performed += OnDecision;
            m_Button.Select();
            Debug.Log("ガイド有効");
        }
    }
    private void OnDisable()
    {
        if (m_PlayerInput != null)
        {
            m_PlayerInput.actions["GuideButton"].performed -= OnDecision;
            Debug.Log("ガイド無効");
        }
    }
    private void OnDecision(InputAction.CallbackContext context)
    {
        Select();
    }
    private void Select()
    {
        m_Button.Select();
        Debug.Log($"セレクト : {m_Button.gameObject.name}");
    }
}
