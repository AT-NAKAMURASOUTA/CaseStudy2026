using Cysharp.Threading.Tasks;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading;
using Unity.VisualScripting;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/*  * Book のページ管理をするスクリプト
 */

[System.Serializable]
public class StageData
{
    public int SceneIndex = 0;
    public SCENETYPE[] SceneTypes;
}

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
        public Button[] buttons;

        // 全てのボタン インデックスがステージナンバー
        public Button[] AllStageButtons;
    }

    // =========================================
    // 列挙型
    // =========================================
    private enum PageFlipMode
    {
        NextPage,
        PreviousPage
    }

    // ========================================
    // Stateパターンの実装
    // ========================================
    private interface IState
    {
        UniTask Enter(BookPageManager page);
        void Update(BookPageManager page, PageFlipMode mode);
        void Exit(BookPageManager page);
    }

    // ===========================================
    // ワールドセレクト状態
    // ===========================================
    private class WorldState : IState
    {
        public async UniTask Enter(BookPageManager book)
        {
            Debug.Log("ワールドセレクトモード");
            book.m_Canvas.gameObject.SetActive(true);

            if (book.m_Camera.transform.position != book.m_CameraWorldModePos)
            {
                await book.ZoomCamera(CancellationToken.None, book.m_CameraWorldModePos);
            }
        }

        public void Update(BookPageManager book, PageFlipMode _mode)
        {
            if (book.m_IsZooming) { return; }
            book.m_StageNumber = 0;

            if (_mode == PageFlipMode.NextPage)
            {
                // 範囲チェック
                if (book.m_CurrentStageIndex + 1 >= book.m_MaxCurrentStagePage)
                {
                    return;
                }

                if (book.m_IsPlaying) { return; }
                book.m_IsPlaying = true;

                // ステージをTrueに
                book.ActiveStage(book.m_CurrentStageIndex + 1);

                // UIを手前に描画
                book.VisualizeUI();

                // ページをめくる
                book.m_Pages[book.m_CurrentStageIndex].FlipPage();

                // 現在ページ更新
                book.m_CurrentStageIndex++;
            }
            else
            {
                // 範囲チェック
                if (book.m_CurrentStageIndex - 1 < 0)
                {
                    return;
                }

                if (book.m_IsPlaying) { return; }
                book.m_IsPlaying = true;

                // Stage をTrue
                book.ActiveStage(book.m_CurrentStageIndex - 1);

                // UIを手前に描画
                book.VisualizeUI();

                // ページを戻す
                book.m_Pages[book.m_CurrentStageIndex - 1].FlipPage();

                // 更新
                book.m_CurrentStageIndex--;
            }
        }
        public void Exit(BookPageManager page) 
        { 
            page.m_Canvas.gameObject.SetActive(false);
        }
    }

    // ===========================================
    // ステージセレクト状態
    // ===========================================
    private class StageState : IState
    {
        StageView nowStage;

        public async UniTask Enter(BookPageManager book)
        {
            Debug.Log("ステージセレクトモード");
            book.m_CanvasGroup.blocksRaycasts = false;

            if (book.m_Camera.transform.position != book.m_CameraStageModePos)
            {
                await book.ZoomCamera(CancellationToken.None, book.m_CameraStageModePos);
            }

            nowStage = book.m_StageViews[book.m_CurrentStageIndex];
            nowStage.AllStageButtons[book.m_StageNumber].Select();
        }
        public void Update(BookPageManager book, PageFlipMode _mode)
        {
            if(book.m_IsZooming) { return; }

            if (_mode == PageFlipMode.NextPage)
            {
                if (book.m_StageNumber < 5)
                {
                    book.m_StageNumber++;
                    nowStage.AllStageButtons[book.m_StageNumber].Select();
                }
            }
            else
            {
                if (book.m_StageNumber > 0)
                {
                    book.m_StageNumber--;
                    nowStage.AllStageButtons[book.m_StageNumber].Select();
                }
            }
        }
        public void Exit(BookPageManager book)
        {
            // セレクト解除
            book.m_CanvasGroup.blocksRaycasts = true;
            EventSystem.current.SetSelectedGameObject(null);
        }
    }


    // ===========================================
    // メンバー変数
    // ===========================================
    [Header("カメラ設定")]
    [Tooltip("ステージセレクト時のカメラ位置")]
    [SerializeField] private Vector3 m_CameraStageModePos;
    [Tooltip("ズーム時間")]
    [SerializeField] private float m_ZoomTime;
    [Tooltip("イーズアウトの強さ")]
    [SerializeField] private float m_EaseOutPower = 3f;
    [Header("操作UI")]
    [Tooltip("表示キャンバス")]
    [SerializeField] private Canvas m_Canvas;

    // 全てのページ
    [SerializeField, HideInInspector] private PageAnimation[] m_Pages;
    // BaseBookの最初のボタン
    [SerializeField, HideInInspector] private Button[] m_FirstButton;
    // BaseBookの最後のボタン
    [SerializeField, HideInInspector] private Button[] m_LastButton;
    // ステージごとのシーン保持
    [SerializeField, HideInInspector] private StageData[] m_StageData;

    // ステージ表示
    private StageView[] m_StageViews;

    // 現在のページ
    private int m_CurrentStageIndex = 0;
    private int m_MaxCurrentStagePage = 0;
    // 選択ステージナンバー
    private int m_StageNumber = 0;
    // 現在の状態
    private IState m_CurrentState;
    private PlayerInput m_PlayerInput;
    // めくっている途中か判定
    bool m_IsPlaying = false;
    // カメラ
    private Camera m_Camera;
    private Vector3 m_CameraWorldModePos;
    private bool m_IsZooming = false;
    // キャンバスグループ
    private CanvasGroup m_CanvasGroup;

    // ===========================================
    // 初期化
    // ===========================================
    async void Start()
    {
        // 1フレーム待つ
        await UniTask.Yield();

        m_Camera = Camera.main;
        m_CameraWorldModePos = m_Camera.transform.position;
        m_IsZooming = false;

        m_CanvasGroup = GetComponent<CanvasGroup>();
        m_PlayerInput = GetComponent<PlayerInput>();

        // 入力イベントの登録
        m_PlayerInput.actions["NextPage"].performed += ctx => NextPage();
        m_PlayerInput.actions["PreviousPage"].performed += ctx => PreviousPage();
        m_PlayerInput.actions["WorldSelect"].performed += ctx => ChangeWorldSelect();
        m_PlayerInput.actions["StageSelect"].performed += ctx => ChangeStageSelect();
        m_PlayerInput.actions["Decision"].performed += ctx => DecisionButton();

        // 前のシーンタイプを取得
        if (m_StageData == null)
        {
            Debug.LogError("ステージデータが登録されていません");
            return;
        }

        // 変数初期化
        m_MaxCurrentStagePage = m_Pages.Length + 1;
        m_IsPlaying = false;

        // Stage を作成
        CreateStageView();

        // Page に終了関数登録
        foreach (var page in m_Pages)
        {
            // ページが本の中心を通過したときのイベントに、UIを手前に描画する関数を登録
            page.OnCrossCenter += VisualizeUI;
            // ページめくり完了イベントに、現在のステージを更新する関数を登録
            page.OnFlipComplete += UpdateCurrentStage;
        }

        // 現在のページを計算
        m_CurrentState = new WorldState();
        _ = m_CurrentState.Enter(this);

        m_CurrentStageIndex = 0;
        SCENETYPE oldScene = OldSceneData.GetOldScene();
        bool IsFind = false;
        for (int i = 0; i < m_StageData.Length; i++)
        {
            for (int j = 0; j < m_StageData[i].SceneTypes.Length; j++)
            {
                if (oldScene == m_StageData[i].SceneTypes[j])
                {
                    m_CurrentStageIndex = m_StageData[i].SceneIndex;
                    m_StageNumber = j;
                    ChangeStageSelect();
                    IsFind = true;

                    Debug.Log($"ステージナンバー : {m_StageNumber}");
                    break;
                }
            }
            if (IsFind) break;
        }

        // 初期化
        for (int i = 0; i < m_CurrentStageIndex; i++)
        {
            // ステージを表示
            foreach (var page in m_StageViews[i].pages)
            {
                page.SetInitPageSide(PageSide.Left);
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
        m_CurrentState.Update(this, PageFlipMode.NextPage);
    }

    // ============================================
    // 前のページへ
    // ============================================
    void PreviousPage()
    {
        m_CurrentState.Update(this, PageFlipMode.PreviousPage);
    }

    // =============================================
    // 決定
    // =============================================
    void DecisionButton()
    {
        m_StageViews[m_CurrentStageIndex].AllStageButtons[m_StageNumber].onClick.Invoke();
    }

    // =============================================
    // ワールドセレクトへ
    // =============================================
    public void ChangeWorldSelect()
    {
        if (m_CurrentState is WorldState) { return; }
        m_CurrentState.Exit(this);
        m_CurrentState = new WorldState();
        _ = m_CurrentState.Enter(this);
    }

    // =============================================
    // ステージセレクトへ
    // =============================================
    public void ChangeStageSelect()
    {
        if (m_CurrentState is StageState) { return; }

        m_CurrentState.Exit(this);
        m_CurrentState = new StageState();
        _ = m_CurrentState.Enter(this);
    }

    // =============================================
    // StageView を作成
    // =============================================
    private void CreateStageView()
    {
        // ページがない場合
        if (m_Pages == null || m_Pages.Length == 0)
        {
            m_StageViews = new StageView[1];

            Button[] buttons = new Button[m_FirstButton.Length + m_LastButton.Length];

            m_FirstButton.CopyTo(buttons, 0);
            m_LastButton.CopyTo(buttons, m_FirstButton.Length);

            m_StageViews[0] = new StageView()
            {
                pages = System.Array.Empty<PageAnimation>(),
                buttons = buttons
            };

            return;
        }


        // StageViewを作成
        m_StageViews = new StageView[m_Pages.Length + 1];

        // Stage1 を作成
        List<Button> buttonList = new List<Button>();
        for (int i = 0; i < m_FirstButton.Length; i++)
        {
            buttonList.Add(m_FirstButton[i]);
        }
        for(int i = 0; i < m_Pages[0].GetFrontButtons().Length;i++)
        {
            buttonList.Add(m_Pages[0].GetFrontButtons()[i]);
        }

        m_StageViews[0] = new StageView()
        {
            pages = new PageAnimation[] { m_Pages[0] },
            buttons = m_FirstButton,
            AllStageButtons = buttonList.ToArray()
        };

        for (int i = 1; i < m_Pages.Length; i++)
        {
            buttonList.Clear();
            for (int j = 0; j < m_Pages[i - 1].GetBackButton().Length; j++)
            {
                buttonList.Add(m_Pages[i - 1].GetBackButton()[j]);
            }
            for (int j = 0; j < m_Pages[i].GetFrontButtons().Length; j++)
            {
                buttonList.Add(m_Pages[i].GetFrontButtons()[j]);
            }
            
            m_StageViews[i] = new StageView()
            {
                // Page を代入
                pages = new PageAnimation[]
                {
                m_Pages[i - 1],
                m_Pages[i]
                },

                // 空を代入
                buttons = System.Array.Empty<Button>(),
                AllStageButtons = buttonList.ToArray()
            };
        }

        // 最終Stageを作成
        buttonList.Clear();
        for (int i = 0; i < m_Pages[m_Pages.Length - 1].GetBackButton().Length; i++)
        {
            buttonList.Add(m_Pages[m_Pages.Length - 1].GetBackButton()[i]);
        }
        for (int i = 0; i < m_LastButton.Length; i++)
        {
            buttonList.Add(m_LastButton[i]);
        }
        m_StageViews[m_Pages.Length] = new StageView()
        {
            pages = new PageAnimation[] { m_Pages[m_Pages.Length - 1] },
            buttons = m_LastButton,
            AllStageButtons = buttonList.ToArray()
        };

        // ステージ
        for (int i = 0; i < m_StageViews.Length; i++)
        {
            Debug.Log(i);
            for (int j = 0; j < m_StageViews[i].AllStageButtons.Length; j++)
            {
                Debug.Log(m_StageViews[i].AllStageButtons[j].gameObject.name);
            }
        }
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
            button.gameObject.SetActive(true);
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
                button.gameObject.SetActive(false);
            }
        }
    }

    // ===========================================
    // ズーム処理
    // ===========================================
    public async UniTask ZoomCamera(CancellationToken token, Vector3 targetPos)
    {
        m_IsZooming = true;

        // 開始状態を取得
        Vector3 startPos = m_Camera.transform.position;
        // 時間
        float elapsed = 0f;

        while (elapsed < m_ZoomTime)
        {
            token.ThrowIfCancellationRequested();

            // 経過時間の更新
            elapsed += Time.deltaTime;

            // 0~1の範囲で補間値を計算
            float t = Mathf.Clamp01(elapsed / m_ZoomTime);
            // イーズアウトの適用
            t = 1f - Mathf.Pow(1f - t, m_EaseOutPower);

            // カメラのサイズと位置を補間して更新
            m_Camera.transform.position =
                Vector3.Lerp(startPos, targetPos, t);

            // 次のフレームまで待機
            await UniTask.Yield(token);
        }

        // 最終的な状態を確実に設定
        m_Camera.transform.position = targetPos;
        m_IsZooming = false;
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
    public void SetFirstButtons(Button[] _firstButtons)
    {
        m_FirstButton = _firstButtons;
    }
    public void SetLastButtons(Button[] _lastButtons)
    {
        m_LastButton = _lastButtons;
    }
    public void SetSceneData(StageData[] _data)
    {
      m_StageData = _data;  
    }
}
