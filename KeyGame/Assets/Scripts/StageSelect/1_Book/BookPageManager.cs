using NUnit.Framework;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

/*  * Book のページ管理をするスクリプト
 */

[RequireComponent(typeof(PlayerInput))]
public class BookPageManager : MonoBehaviour
{
    // ===========================================
    // 構造体
    // ===========================================
    // このステージを表示するときに必要なObjectをまとめる
    private struct StageView
    {
        // ページ
        public PageAnimation[] pages;
        // オブジェクト
        public GameObject[] buttons;
    }

    // ===========================================
    // メンバー変数
    // ===========================================
    // 全てのページ
    [SerializeField, HideInInspector] private PageAnimation[] m_Pages;
    // BaseBookの最初のボタン
    [SerializeField, HideInInspector] private GameObject[] m_FirstButton;
    // BaseBookの最後のボタン
    [SerializeField, HideInInspector] private GameObject[] m_LastButton;

    // ステージ表示
    private StageView[] m_StageViews;

    // 現在のページ
    private int m_CurrentStageIndex = 0;
    private int m_MaxCurrentStagePage = 0;
    private PlayerInput m_PlayerInput;
    // めくっている途中か判定
    bool m_IsPlaying = false;

    // ===========================================
    // 初期化
    // ===========================================
    void Start()
    {
        m_PlayerInput = GetComponent<PlayerInput>();

        // 入力イベントの登録
        m_PlayerInput.actions["NextPage"].performed += ctx => NextPage();
        m_PlayerInput.actions["PreviousPage"].performed += ctx => PreviousPage();

        // 変数初期化
        m_CurrentStageIndex = 0;
        m_MaxCurrentStagePage = m_Pages.Length + 1;
        m_IsPlaying = false;

        // Stage を作成
        CreateStageView();

        // Page に終了関数登録
        foreach(var page in m_Pages)
        {
            Debug.Log("ページ数 : " + m_Pages.Length);
            Debug.Log($"ページ名 : {page.gameObject.name}");

            // ページが本の中心を通過したときのイベントに、UIを手前に描画する関数を登録
            page.OnCrossCenter += VisualizeUI;
            // ページめくり完了イベントに、現在のステージを更新する関数を登録
            page.OnFlipComplete += UpdateCurrentStage;
        }

        // Debugログ表示
        for (int i = 0; m_StageViews.Length > i; i++)
        {
            // Stage表示
            Debug.Log($"Stagge{i + 1}");

            // ページ名表示
            foreach (var page in m_StageViews[i].pages)
            {
                Debug.Log($"ページ名 : {page.gameObject.name}");
            }

            foreach (var button in m_StageViews[i].buttons)
            {
                Debug.Log($"ボタン名 : {button.gameObject.name}");
            }
        }

        // すべて非表示
        AllInactiveStage();
        // 最初のステージを表示
        ActiveStage(m_CurrentStageIndex);
    }

    // ===========================================
    // 次のページへ
    // ===========================================
    void NextPage()
    {
        // 範囲チェック
        if (m_CurrentStageIndex + 1 >= m_MaxCurrentStagePage)
        {
            return;
        }

        if (m_IsPlaying) { return; }
        m_IsPlaying = true;

        // ステージをTrueに
        ActiveStage(m_CurrentStageIndex + 1);

        // UIを手前に描画
        VisualizeUI();

        // ページをめくる
        m_Pages[m_CurrentStageIndex].FlipPage();

        // 現在ページ更新
        m_CurrentStageIndex++;
    }

    // ============================================
    // 前のページへ
    // ============================================
    void PreviousPage()
    {
        // 範囲チェック
        if (m_CurrentStageIndex - 1 < 0)
        {
            return;
        }

        if (m_IsPlaying) { return; }
        m_IsPlaying = true;

        // Stage をTrue
        ActiveStage(m_CurrentStageIndex - 1);

        // UIを手前に描画
        VisualizeUI();

        // ページを戻す
        m_Pages[m_CurrentStageIndex - 1].FlipPage();

        // 更新
        m_CurrentStageIndex--;
    } 

    // =============================================
    // StageView を作成
    // =============================================
    private void CreateStageView()
    {
        // StageViewを作成
        m_StageViews = new StageView[m_Pages.Length + 1];

        // Stage1 を作成
        m_StageViews[0] = new StageView()
        {
            pages = new PageAnimation[] { m_Pages[0] },
            buttons = m_FirstButton
        };

        for (int i = 1; i < m_Pages.Length; i++)
        {
            m_StageViews[i] = new StageView()
            {
                // Page を代入
                pages = new PageAnimation[]
                {
                m_Pages[i - 1],
                m_Pages[i]
                },

                // 空を代入
                buttons = System.Array.Empty<GameObject>()
            };
        }

        // 最終Stageを作成
        m_StageViews[m_Pages.Length] = new StageView()
        {
            pages = new PageAnimation[] { m_Pages[m_Pages.Length - 1] },
            buttons = m_LastButton
        };
    }

    // ===========================================
    // 現在のステージを表示
    // ===========================================
    private void UpdateCurrentStage()
    {
        // すべて非表示
        AllInactiveStage();

        // ステージを表示
        ActiveStage(m_CurrentStageIndex);

        m_IsPlaying = false;
    }

    // ===========================================
    // 引数のステージをActiveにする
    // ===========================================
    private void ActiveStage(int _activeStageIndex)
    {
        // ステージを表示
        foreach (var page in m_StageViews[_activeStageIndex].pages)
        {
            page.ActivePage(true);
        }
        foreach (var button in m_StageViews[_activeStageIndex].buttons)
        {
            button.SetActive(true);
        }
    }

    // ============================================
    // UI を手前に描画する
    // ============================================
    private void VisualizeUI()
    {
        // UIを手前に描画
        foreach (var page in m_StageViews[m_CurrentStageIndex].pages)
        {
            page.transform.SetAsLastSibling();
        }
        foreach (var button in m_StageViews[m_CurrentStageIndex].buttons)
        {
            button.transform.SetAsLastSibling();
        }
    }

    // ===========================================
    // 全てのステージを非Activeにする
    // ===========================================
    private void AllInactiveStage()
    {
        // 全てのステージをFalseに
        foreach (var stageView in m_StageViews)
        {
            foreach (var page in stageView.pages)
            {
                page.ActivePage(false);
            }
            foreach (var button in stageView.buttons)
            {
                button.SetActive(false);
            }
        }
    }

    // ===========================================
    // セッター
    // ===========================================
    // ページセッター
    public void SetPages(PageAnimation[] _pages)
    {
        m_Pages = _pages;
    }

    // ボタンセッター
    public void SetFirstButtons(GameObject[] _firstButtons)
    {
        m_FirstButton = _firstButtons;
    }
    public void SetLastButtons(GameObject[] _lastButtons)
    {
        m_LastButton = _lastButtons;
    }
}
