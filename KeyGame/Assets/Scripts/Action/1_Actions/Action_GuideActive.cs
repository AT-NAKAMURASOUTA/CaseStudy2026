using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEditor;
using UnityEngine;

public class Action_GuideActive : BaseAction
{
    [SerializeField] private GameObject m_GidePrefab;
    private GameObject m_Gide;
    private Action_GuideInactive m_Action;

    public void CreateGidePrefab(MenuManager _manager)
    {
        // メニューを作成
        m_Gide = Instantiate(m_GidePrefab);
        m_Action = m_Gide.GetComponentInChildren<Action_GuideInactive>();
        m_Action.Init(_manager, m_Gide);
        m_Gide.SetActive(false);

        Debug.Log("ガイドプレハブを作成");
    }

    public override UniTask Execute(CancellationToken token)
    {
        m_Gide.SetActive(true);
        m_Action.GuideActive();

        Debug.Log("ガイドを表示");

        return UniTask.CompletedTask;
    }
}
