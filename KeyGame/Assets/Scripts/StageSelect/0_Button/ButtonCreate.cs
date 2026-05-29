using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEditor;

/*  * ボタンやページを生成するスクリプト
 */

[RequireComponent(typeof(BookLayout))]
[RequireComponent(typeof(BookPageManager))]

public class ButtonCreate : MonoBehaviour
{
    // ===========================================
    // メンバー変数
    // ===========================================
    [Header("ボタン設定")]
    [Tooltip("ボタンのプレハブ")]
    [SerializeField] private GameObject m_ButtonPrefab;
    [Tooltip("ボタンのデータ")]
    [SerializeField] private StageButtonData[] m_ButtonData;
    
    [Header("ページ設定")]
    [Tooltip("ページのプレハブ")]
    [SerializeField] private GameObject m_PagePrefab;

    // ボタンの位置リスト
    private List<Vector2> m_ButtonPositions;
    // 見開きボタンの数
    private int m_ButtonMaxCount;
    // 片ページのボタン数
    private int m_PageMaxCountX;
    private int m_PageMaxCountY;
    // ボタンのサイズ
    private Vector2 m_ButtonSize;
    // 生成ボタンにつけるタグ
    private const string m_ButtonTag = "StageSelect";
    // ページ数を数える
    private int m_PageCount = 0;
    // ページのセンター
    private Vector2 m_BookCenter= Vector2.zero;
    // BookPageManager
    private BookPageManager m_BookPageManager;

    // ===========================================
    // 作成
    // ===========================================
    [ContextMenu("作成")]
    public void Create()
    {
        // 既存のボタンを削除
        ClearButtons();

        // ボタン位置情報取得
        BookLayout layout = GetComponent<BookLayout>();
        m_ButtonPositions = layout.GetButtonPosition();
        m_ButtonMaxCount = layout.GetButtonMaxNumber();
        m_ButtonSize = layout.GetButtonSize();
        m_PageMaxCountX = layout.GetButtonMaxCountX();
        m_PageMaxCountY = layout.GetButtonMaxCountY();
        m_PageCount = m_ButtonData.Length - 1;
        m_BookCenter = layout.GetCenterPosition();
        // BookPageManagerの取得
        m_BookPageManager = GetComponent<BookPageManager>();

        // エラーチェック
        if (m_ButtonPrefab == null)
        {
            UnityEngine.Debug.LogError("ボタンのプレハブが設定されていません。");
            return;
        }
        if (m_ButtonData.Length == 0)
        {
            UnityEngine.Debug.LogError("ボタンのデータが設定されていません。");
            return;
        }
        if (m_ButtonPositions.Count == 0)
        {
            UnityEngine.Debug.LogError("ボタンの位置が設定されていません。");
            return;
        }
        if (m_ButtonData[0].stageDataList.Count > m_ButtonMaxCount)
        {
            UnityEngine.Debug.LogError($"ボタンのデータ数が見開きの最大数を超えています。最大数: {m_ButtonMaxCount}");
            return;
        }

        // ボタンの生成
        BookCreate();

#if UNITY_EDITOR
        // 変更前を一時的に保存
        Undo.RecordObject(m_BookPageManager, "Update Book Page Manager");

        // 変更したことをUnityに伝える
        EditorUtility.SetDirty(m_BookPageManager);
        // 今保存させる
        AssetDatabase.SaveAssets();
#endif
    }

    // ===========================================
    // ボタン削除関数
    // ===========================================
    private void ClearButtons()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);

            if (child.CompareTag(m_ButtonTag))
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }

    // ===========================================
    // ボタンを下に配置する関数
    // ===========================================
    private void BookCreate()
    {
        // 最初のページのボタン作成
        FirstButtonCreate();
        // ボタンとページの生成
        CreateButtonsAndPages();
        // 最後のページのボタン作成
        LastButtonCreate();
    }

    // ===========================================
    // 最初のボタン 作成
    // ===========================================
    private void FirstButtonCreate()
    {
        // FirstStage のデータを取得
        List<ButtonData> buttonData = m_ButtonData[0].stageDataList;
        // ButtonData
        List<GameObject> firstButtonData = new List<GameObject>();

        // FirstButton の生成
        for (int x = 0; x < m_PageMaxCountX; x++)
        {
            for (int y = 0; y < m_PageMaxCountY; y++)
            {
                // 生成するボタンの添え字を計算
                int index = y * (m_PageMaxCountX * 2) + x;

                // データ数を超えたら終了
                if (index >= buttonData.Count) 
                {
                    Debug.LogWarning($"ステージ1データがインデックス以下だったためスキップしました。 Index : {index}");
                    continue;
                }

                // ボタンを作成
                GameObject button = Instantiate(m_ButtonPrefab, transform);
                button.transform.localPosition = m_ButtonPositions[index];
                // ボタンのサイズ設定
                RectTransform rect = button.GetComponent<RectTransform>();
                rect.sizeDelta = m_ButtonSize;
                // ボタンの名前設定
                button.gameObject.name = $"Stage 1-{index + 1}_Button";
                // ボタンのタグ設定
                button.gameObject.tag = m_ButtonTag;

                // ボタンのデータを設定
                Action_LoadTargetScene loadScene = button.GetComponent<Action_LoadTargetScene>();
                if (loadScene != null)
                {
                    loadScene.Init(buttonData[index].NextScene);
                }
                else
                {
                    UnityEngine.Debug.LogWarning("ボタンにLoadSceneがアタッチされていません。");
                }

                // 画像設定
                Image image = button.GetComponent<Image>();
                if (buttonData[index].Texture != null)
                {
                    image.sprite = buttonData[index].Texture;
                }
                else
                {
                    TMP_Text text = button.GetComponentInChildren<TMP_Text>();
                    text.text = $"Stage 1-{index + 1} Button";
                    Debug.LogWarning("ボタンにImageがアタッチされていません。");
                }

                firstButtonData.Add(button);
            }
        }

        // BookPageManagerに最初のページのボタンを設定
        m_BookPageManager.SetFirstButtons(firstButtonData.ToArray());
    }

    // ===========================================
    // 最後のページのボタンを作成する関数
    // ===========================================
    private void LastButtonCreate()
    {
        // 最後のページのデータを取得
        List<ButtonData> buttonData = m_ButtonData[m_ButtonData.Length - 1].stageDataList;
        List<GameObject> lastButtonData = new List<GameObject>();

        // LastButton の生成
        for (int x = 0; x < m_PageMaxCountX; x++)
        {
            for (int y = 0; y < m_PageMaxCountY; y++)
            {
                // 生成するボタンの添え字を計算
                int index = y * (m_PageMaxCountX * 2) + x + m_PageMaxCountX;

                // データ数を超えたら終了
                if (index >= buttonData.Count)
                {
                    Debug.LogWarning($"最終ステージデータがインデックス以下だったためスキップしました。 Index : {index}");
                    continue;
                }

                // ボタンを作成
                GameObject button = Instantiate(m_ButtonPrefab, transform);
                button.transform.localPosition = m_ButtonPositions[index];
                // ボタンのサイズ設定
                RectTransform rect = button.GetComponent<RectTransform>();
                rect.sizeDelta = m_ButtonSize;
                // ボタンの名前設定
                button.gameObject.name = $"Stage {m_ButtonData.Length}-{index + 1}_Button";
                // ボタンのタグ設定
                button.gameObject.tag = m_ButtonTag;

                // ボタンのデータを設定
                Action_LoadTargetScene loadScene = button.GetComponent<Action_LoadTargetScene>();
                if (loadScene != null)
                {
                    loadScene.Init(buttonData[index].NextScene);
                }
                else
                {
                    UnityEngine.Debug.LogWarning("ボタンにLoadSceneがアタッチされていません。");
                }
                // 画像設定
                Image image = button.GetComponent<Image>();
                if (buttonData[index].Texture != null)
                {
                    image.sprite = buttonData[index].Texture;
                }
                else
                {
                    TMP_Text text = button.GetComponentInChildren<TMP_Text>();
                    text.text = $"Stage {m_ButtonData.Length}-{index + 1} Button";
                    Debug.LogWarning("ボタンにImageがアタッチされていません。");
                }

                // 最後のページのボタンデータを保存
                lastButtonData.Add(button);
            }
        }

        // BookPageManagerに最後のページのボタンを設定
        m_BookPageManager.SetLastButtons(lastButtonData.ToArray());
    }

    // ===========================================
    // ボタンとページを生成する関数
    // ===========================================
    private void CreateButtonsAndPages()
    {
        List<PageAnimation> pageArray = new List<PageAnimation>();

        // ページ数回す
        for (int pageCount = 0; pageCount < m_PageCount; pageCount++)
        {
            // ページのデータを取得
            List<ButtonData> buttonData = m_ButtonData[pageCount].stageDataList;
            List<GameObject> buttons = new List<GameObject>();
            // 片ページのボタン数
            int OnePageNumber = m_PageMaxCountX * m_PageMaxCountY;

            // ページ作成
            GameObject page = Instantiate(m_PagePrefab, transform);
            PageAnimation pageAnimation = page.GetComponent<PageAnimation>();
            pageAnimation.Init(m_BookCenter.x);
            page.gameObject.name = $"Page_{pageCount + 1}";
            page.gameObject.tag = m_ButtonTag;

            // 表ページのボタン作成
            for (int x = 0; x < m_PageMaxCountX; x++)
            {
                // Yボタン数
                for (int y = 0; y < m_PageMaxCountY; y++)
                {
                    // 生成するボタンの添え字を計算
                    int index = y * (m_PageMaxCountX * 2) + x + m_PageMaxCountX;

                    // データ数を超えたら終了
                    if (index >= buttonData.Count)
                    {
                        Debug.LogWarning($"ステージ{pageCount + 1}データがインデックス以下だったためスキップしました。 Index : {index}");
                        continue;
                    }

                    // ボタンを作成
                    GameObject button = Instantiate(m_ButtonPrefab, transform);
                    button.transform.localPosition = m_ButtonPositions[index];
                    // ボタンのサイズ設定
                    RectTransform rect = button.GetComponent<RectTransform>();
                    rect.sizeDelta = m_ButtonSize;
                    // ボタンの名前設定
                    button.gameObject.name = $"Stage {pageCount + 1}-{index + 1}_Button";
                    // ボタンのタグ設定
                    button.gameObject.tag = m_ButtonTag;

                    // 親プレハブを設定
                    button.transform.SetParent(page.transform);

                    // ボタンのデータを設定
                    Action_LoadTargetScene loadScene = button.GetComponent<Action_LoadTargetScene>();
                    if (loadScene != null)
                    {
                        loadScene.Init(buttonData[index].NextScene);
                    }
                    else
                    {
                        UnityEngine.Debug.LogWarning("ボタンにLoadSceneがアタッチされていません。");
                    }
                    // 画像設定
                    Image image = button.GetComponent<Image>();
                    if (buttonData[index].Texture != null)
                    {
                        image.sprite = buttonData[index].Texture;
                    }
                    else
                    {
                        TMP_Text text = button.GetComponentInChildren<TMP_Text>();
                        text.text = $"Stage {pageCount + 1}-{index + 1} Button";
                        Debug.LogWarning("ボタンにImageがアタッチされていません。");
                    }

                    buttons.Add(button);
                }
            }

            // ページに登録
            pageAnimation.SetFrontButtons(buttons.ToArray());

            buttons.Clear();

            // NextStageを取得
            buttonData = m_ButtonData[pageCount + 1].stageDataList;

            // 裏ページのボタン作成
            for (int x = 0; x < m_PageMaxCountX; x++)
            {
                // Yボタン数
                for (int y = 0; y < m_PageMaxCountY; y++)
                {
                    // 生成するボタンの添え字を計算
                    int posIndex = y * (m_PageMaxCountX * 2) + (m_PageMaxCountX - 1 - x) + m_PageMaxCountX;
                    // ステージのインデックスを計算
                    int index = y * (m_PageMaxCountX * 2) + x;

                    // データ数を超えたら終了
                    if (index >= buttonData.Count)
                    {
                        Debug.LogWarning($"ステージ{pageCount + 1}データがインデックス以下だったためスキップしました。 Index : {index}");
                        continue;
                    }

                    // ボタンを作成
                    GameObject button = Instantiate(m_ButtonPrefab, transform);
                    button.transform.localPosition = m_ButtonPositions[posIndex];
                    // 裏のためボタンを180度回転させておく
                    button.transform.rotation = Quaternion.Euler(0, 180, 0);
                    // ボタンのサイズ設定
                    RectTransform rect = button.GetComponent<RectTransform>();
                    rect.sizeDelta = m_ButtonSize;
                    // ボタンの名前設定
                    button.gameObject.name = $"Stage {pageCount + 2}-{index + 1}_Button";
                    // ボタンのタグ設定
                    button.gameObject.tag = m_ButtonTag;

                    // 親プレハブを設定
                    button.transform.SetParent(page.transform);

                    // ボタンのデータを設定
                    Action_LoadTargetScene loadScene = button.GetComponent<Action_LoadTargetScene>();
                    if (loadScene != null)
                    {
                        loadScene.Init(buttonData[index].NextScene);
                    }
                    else
                    {
                        UnityEngine.Debug.LogWarning("ボタンにLoadSceneがアタッチされていません。");
                    }
                    // 画像設定
                    Image image = button.GetComponent<Image>();
                    if (buttonData[index].Texture != null)
                    {
                        image.sprite = buttonData[index].Texture;
                    }
                    else
                    {
                        TMP_Text text = button.GetComponentInChildren<TMP_Text>();
                        text.text = $"Stage {pageCount + 2}-{index + 1} Button";
                        Debug.LogWarning("ボタンにImageがアタッチされていません。");
                    }

                    buttons.Add(button);
                }
            }

            // 登録
            pageAnimation.SetBackButtons(buttons.ToArray());

            // 配列に保存
            pageArray.Add(pageAnimation);
        }

        // マネージャーに登録
        m_BookPageManager.SetPages(pageArray.ToArray());
    }
}
