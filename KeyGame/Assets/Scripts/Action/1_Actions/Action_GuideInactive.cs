using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

public class Action_GuideInactive : BaseAction
{
    // メニューマネージャー
    MenuManager m_MenuManager;
    GameObject m_GuideObject;
    CanvasGroup m_GuideCanvasGroup;

    public void Init(MenuManager _menu, GameObject _guide)
    {
        m_MenuManager = _menu;
        m_GuideObject = _guide;
        m_GuideCanvasGroup = m_GuideObject.GetComponent<CanvasGroup>();
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

        Debug.Log("ガイドを閉じる処理");

        return UniTask.CompletedTask;
    }
}
