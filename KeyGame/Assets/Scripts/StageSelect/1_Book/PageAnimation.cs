using System;
using System.Drawing;
using UnityEngine;
using UnityEngine.UI;

/*  * PageAnimationのスクリプト
 */

[RequireComponent(typeof(AudioSource))]
public class PageAnimation : MonoBehaviour
{
    // ========================================
    // 列挙型
    // ========================================
    enum PageSide
    {
        Right,
        Left
    }

    // ========================================
    // Stateパターンの実装
    // ========================================
    private interface IState
    {
        void Enter(PageAnimation page);
        void Update(PageAnimation page);
        void Exit(PageAnimation page);
    }

    private class IdleState : IState
    {
        public void Enter(PageAnimation page) 
        {
            page.m_IsAnimationEnd = false;
        }

        public void Update(PageAnimation page)
        {
        }

        public void Exit(PageAnimation page) 
        {
            page.m_IsAnimationEnd = true;
        }
    }

    // ========================================
    // 右ページをめくるアニメーション
    // ========================================
    private class PlayRightState : IState
    {
        // 経過時間
        private float t;

        public void Enter(PageAnimation page)
        {
            t = 0;
            page.SetPageSprite(PageSide.Right);
            page.m_AudioSource.PlayOneShot(page.m_FlipSE);
        }

        public void Update(PageAnimation page)
        {
            // 時間を更新
            t += Time.deltaTime;
            ;
            // 進行度を計算
            float progress = Mathf.Clamp01(t / 1.0f / page.m_AnimationSpeed);

            // 0度から180度まで回転
            float angle =
                Mathf.SmoothStep(
                    0,
                    180,
                    progress);

            // 回転を適用
            page.m_RectTransform.localRotation =
                Quaternion.Euler(0, angle, 0);

            // 90度から180度までは左ページを表示
            if (progress > 0.5f)
            {
                page.OnCrossCenter.Invoke();
                page.SetPageSprite(PageSide.Left);

                // 表のボタンを無効化、裏のボタンを有効化
                for (int i = 0; i < page.m_FrontButton.Length; i++)
                {
                    page.m_FrontButton[i].SetActive(false);
                }
                for (int i = 0; i < page.m_BackButton.Length; i++)
                {
                    page.m_BackButton[i].SetActive(true);
                }
            }
            // 180度到達
            if (progress >= 1f)
            {
                // IDLE状態に遷移
                page.ChangeState(new IdleState());

                page.OnFlipComplete.Invoke();
            }
        }

        public void Exit(PageAnimation page)
        {
        }
    }

    // ========================================
    // 左ページをめくるアニメーション
    // ========================================
    private class PlayLeftState : IState
    {
        // 経過時間
        private float t;

        public void Enter(PageAnimation page)
        {
            t = 0;
            page.SetPageSprite(PageSide.Left);
            page.m_AudioSource.PlayOneShot(page.m_FlipSE);
        }

        public void Update(PageAnimation page)
        {
            // 時間を更新
            t += Time.deltaTime;

            // 進行度を計算
            float progress = Mathf.Clamp01(t / 1.0f / page.m_AnimationSpeed);

            // 0度から180度まで回転
            float angle =
                Mathf.SmoothStep(
                    180,
                    0,
                    progress);

            // 回転を適用
            page.m_RectTransform.localRotation =
                Quaternion.Euler(0, angle, 0);

            // 90度から180度までは右ページを表示
            if (progress > 0.5f)
            {
                page.OnCrossCenter.Invoke();
                page.SetPageSprite(PageSide.Right);

                // 表のボタンを有効化、裏のボタンを無効化
                for (int i = 0; i < page.m_FrontButton.Length; i++)
                {
                    page.m_FrontButton[i].SetActive(true);
                }
                for (int i = 0; i < page.m_BackButton.Length; i++)
                {
                    page.m_BackButton[i].SetActive(false);
                }
            }
            // 180度到達
            if (progress >= 1f)
            {
                // IDLE状態に遷移
                page.ChangeState(new IdleState());

                page.OnFlipComplete.Invoke();
            }
        }

        public void Exit(PageAnimation page)
        {
        }
    }

    // ========================================
    // メンバー変数
    // ========================================
    // ページが本の中心を通過したときのイベント
    public event Action OnCrossCenter;
    // ページがめくり終わったときのイベント
    public event Action OnFlipComplete;

    [Header("ページ設定")]
    [Tooltip("右ページのスプライト")]
    [SerializeField] private Sprite m_RightPageSprite;
    [Tooltip("左ページのスプライト")]
    [SerializeField] private Sprite m_LeftPageSprite;

    [Header("アニメーション設定")]
    [SerializeField] private float m_AnimationSpeed = 0.5f;

    [Header("SE設定")]
    [SerializeField] private AudioClip m_FlipSE;

    // 非公開のメンバー変数
    // 表にあるボタンオブジェクト
    [SerializeField, HideInInspector] private GameObject[] m_FrontButton;
    // 裏にあるボタンオブジェクト
    [SerializeField, HideInInspector] private GameObject[] m_BackButton;
    // 本の中心位置
    [SerializeField, HideInInspector] private float m_BookCenterX = 0;

    // 現在の状態
    private IState m_CurrentState;
    // RectTransformコンポーネント
    private RectTransform m_RectTransform;
    // Imageコンポーネント
    private Image m_Image;
    // 今のページがどちらにあるのか
    private PageSide m_CurrentPageSide = PageSide.Right;
    // アニメーション終了フラグ
    private bool m_IsAnimationEnd = false;
    // AudioSourceコンポーネント
    private AudioSource m_AudioSource;

    // ========================================
    // 初期化 (Awake)
    // ========================================
    public void Init(float _centerX)
    {
        m_BookCenterX = _centerX;

        // RectTransformを取得
        m_RectTransform = GetComponent<RectTransform>();
        m_RectTransform.pivot = new Vector2(0f, 0.5f);
        // 本の中心位置を計算
        m_RectTransform.localPosition = new Vector3(m_BookCenterX, m_RectTransform.localPosition.y, m_RectTransform.localPosition.z);
    }

    // ========================================
    // 初期化 (Start)
    // ========================================
    private void Start()
    {
        if (m_RightPageSprite == null || m_LeftPageSprite == null)
        {
            Debug.LogError("ページのスプライトが設定されていません。");
            return;
        }

        // 初期状態をIdleStateに設定
        m_CurrentState = new IdleState();
        m_CurrentState.Enter(this);

        // RectTransformを取得
        m_RectTransform = GetComponent<RectTransform>();
        m_RectTransform.pivot = new Vector2(0f, 0.5f);
        // 本の中心位置を計算
        m_RectTransform.localPosition = new Vector3(m_BookCenterX, m_RectTransform.localPosition.y, m_RectTransform.localPosition.z);

        // スプライトを設定
        m_Image = GetComponent<Image>();
        m_Image.sprite = m_RightPageSprite;

        // AudioSourceを取得
        m_AudioSource = GetComponent<AudioSource>();
        if(m_FlipSE == null)
        {
            Debug.LogError("ページめくりのSEが設定されていません。");
            return;
        }

        // 裏ボタンを180度回転させる
        for (int i = 0; i < m_BackButton.Length; i++)
        {
            m_BackButton[i].transform.localRotation = Quaternion.Euler(0, 180, 0);
            m_BackButton[i].SetActive(false);
        }
    }

    // ========================================
    // 更新
    // ========================================
    void Update()
    {
        m_CurrentState.Update(this);
    }

    // ========================================
    // 状態遷移
    // ========================================
    private void ChangeState(IState newState)
    {
        m_CurrentState.Exit(this);
        m_CurrentState = newState;
        m_CurrentState.Enter(this);
    }

    // ========================================
    // ページのスプライトを切り替える
    // ========================================
    private void SetPageSprite(PageSide _side)
    {
        // ページのスプライトを切り替える
        if (_side == PageSide.Right)
        {
            m_Image.sprite = m_RightPageSprite;
            m_CurrentPageSide = PageSide.Right;
        }
        else
        {
            m_Image.sprite = m_LeftPageSprite;
            m_CurrentPageSide = PageSide.Left;
        }
    }

    // ========================================
    // 右ページあるボタンのセッター (表ボタン)
    // ========================================
    public void SetFrontButtons(GameObject[] _frontButtons)
    {
        m_FrontButton = _frontButtons;
    }
    // =========================================
    // 左ページあるボタンのセッター (裏ボタン)
    // =========================================
    public void SetBackButtons(GameObject[] _backButtons)
    {
        m_BackButton = _backButtons;
    }

    // ========================================
    // ページをめくる
    // ========================================
    public void FlipPage()
    {
        if (m_CurrentState is IdleState)
        {
            if (m_CurrentPageSide == PageSide.Right)
            {
                ChangeState(new PlayRightState());
            }
            if (m_CurrentPageSide == PageSide.Left)
            {
                ChangeState(new PlayLeftState());
            }
        }
    }

    // =========================================
    // このページのActive
    // =========================================
    public void ActivePage(bool Active)
    {
        if (Active)
        {
            gameObject.SetActive(true);

            if (m_CurrentPageSide == PageSide.Left)
            {
                for (int i = 0; m_BackButton.Length > i; i++)
                {
                    m_BackButton[i].SetActive(true);
                }
            }
            else
            {
                for (int i = 0; m_FrontButton.Length > i; i++)
                {
                    m_FrontButton[i].SetActive(true);
                }
            }
        }
        else
        {
            for (int i = 0; m_FrontButton.Length > i; i++)
            {
                m_FrontButton[i].SetActive(false);
            }
            for (int i = 0; m_BackButton.Length > i; i++)
            {
                m_BackButton[i].SetActive(false);
            }

            gameObject.SetActive(false);
        }
    }

    // =========================================
    // アニメーション終了フラグ
    // =========================================
    public bool GetAnimation()
    {
        return m_IsAnimationEnd;
    }
}
