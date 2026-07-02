using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEditor;
using UnityEngine;

public class Action_GuideActive : BaseAction
{
    [SerializeField] private GameObject m_GuidePrefab;
    private GameObject m_Guide;
    private Action_GuideInactive m_Action;
    // ガイドを表示時にメニューの操作を制御するためのキャンバスグループ
    private CanvasGroup m_CanvasGroup;

    public void CreateGuidePrefab(MenuManager _manager, bool _isGuideActiveAtStart)
    {
        // メニューを作成
        m_Guide = Instantiate(m_GuidePrefab);
        m_Action = m_Guide.GetComponentInChildren<Action_GuideInactive>();
        m_Action.Init(_manager, m_Guide);

        if (!_isGuideActiveAtStart)
        {
            m_Guide.SetActive(false);
        }
        else
        {
            m_Guide.SetActive(true);
            m_Action.GuideActive();
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
}
