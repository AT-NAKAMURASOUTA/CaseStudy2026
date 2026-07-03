using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class Action_GuideActive : BaseAction
{
    [SerializeField] private GameObject m_GuidePrefab;
    private GameObject m_Guide;
    private Action_GuideInactive m_Action;
    private PlayerInput m_PlayerInput;

    public void CreateGuidePrefab(MenuManager _manager, bool _isGuideActiveAtStart,PlayerInput _playerInput)
    {
        // メニューを作成
        m_Guide = Instantiate(m_GuidePrefab);
        m_Action = m_Guide.GetComponentInChildren<Action_GuideInactive>();
        m_Action.Init(_manager, m_Guide,_playerInput);
        m_PlayerInput = _playerInput;

        if (!_isGuideActiveAtStart)
        {
            m_Guide.SetActive(false);
        }
        else
        {
            m_Guide.SetActive(true);
            m_Action.GuideActive();
            m_PlayerInput.actions["Move"].Disable();
            m_PlayerInput.actions["Jump"].Disable();
            m_Action.m_OnInitialGuideHidden.AddListener(InitialGuideHidden);
        }

        Debug.Log("ガイドプレハブを作成");
    }

    public override UniTask Execute(CancellationToken token)
    {
        m_Guide.SetActive(true);
        m_Action.GuideActive();

        Debug.Log("ガイドを表示");

        return UniTask.CompletedTask;
    }

    private void InitialGuideHidden()
    {
        m_PlayerInput.actions["Move"].Enable();
        m_PlayerInput.actions["Jump"].Enable();
    }
}
