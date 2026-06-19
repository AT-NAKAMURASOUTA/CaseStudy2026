using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.U2D;
using UnityEngine.UI;

/*  * ボタンやページを生成するスクリプト
 */

[RequireComponent(typeof(BookLayout))]
[RequireComponent(typeof(BookPageManager))]

public class ButtonCreate : MonoBehaviour
{
    // ===========================================
    // 構造体
    // ===========================================
    [System.Serializable]
    public class ButtonTextureData
    {
        public string Key;
        public Sprite Sprite;
    }

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

    [Header("ボタンテクスチャ設定")]
    [Tooltip("ボタンのテクスチャ")]
    [SerializeField] private List<ButtonTextureData> m_ButtonTextures;

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
    private Vector2 m_BookCenter = Vector2.zero;
    // BookPageManager
    private BookPageManager m_BookPageManager;
    // Dict<Character, Sprite>に変換したボタンテクスチャデータ
    private Dictionary<char, Sprite> m_ButtonTextureDict = new Dictionary<char, Sprite>();
    // ButtonTexture の位置
    private Vector2[] m_ButtonTexturePos;
    // ButtonTexture　のサイズ
    private float m_ButtonTextureSize = 1.0f;

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

        // ボタンテクスチャのDict変換
        foreach (var data in m_ButtonTextures)
        {
            if (data.Key != default && data.Sprite != null)
            {
                char key = data.Key[0];

                if (!m_ButtonTextureDict.ContainsKey(key))
                {
                    m_ButtonTextureDict.Add(key, data.Sprite);
                }
                else
                {
                    Debug.LogWarning($"同じKeyが複数設定されています。Key: {key}");
                }
            }
            else
            {
                Debug.LogWarning("ボタンテクスチャデータのCharacterが空またはSpriteが設定されていません。");
                return;
            }
        }
        // ボタンテクスチャの位置をBookLayoutから取得
        m_ButtonTexturePos = layout.GetButtonTextureLocalPositions();
        // ボタンテクスチャのサイズをBookLayoutから取得
        m_ButtonTextureSize = layout.GetButtonTextureSize();

        // ボタンの生成
        BookCreate();

        // ステージごとにまとめる
        StageData[] sceneData = new StageData[m_ButtonData.Length];
        for (int i = 0; i < m_ButtonData.Length; i++)
        {
            sceneData[i] = new StageData();

            sceneData[i].SceneIndex = i;
            List<SCENETYPE> types = new List<SCENETYPE>();

            for (int j = 0; j < m_ButtonData[i].stageDataList.Count; j++)
            {
                types.Add(m_ButtonData[i].stageDataList[j].NextScene);
            }

            sceneData[i].SceneTypes = types.ToArray();
        }
        m_BookPageManager.SetSceneData(sceneData);

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
                
                TMP_Text text = button.GetComponentInChildren<TMP_Text>();
                if (text != null)
                {
                    // テキストを空にする
                    text.text = string.Empty;
                }

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
                    Debug.LogWarning("ボタンにImageがアタッチされていません。");
                }

                // ボタンテキストの生成
                CreateStageText(rect, 1, index + 1);

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

                // テキストを空にする
                TMP_Text text = button.GetComponentInChildren<TMP_Text>();
                if (text != null)
                {
                    // テキストを空にする
                    text.text = string.Empty;
                }

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
                    Debug.LogWarning("ボタンにImageがアタッチされていません。");
                }

                // ボタンテキストの生成
                CreateStageText(rect, m_ButtonData.Length, index + 1);

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

                    // テキストを空にする
                    TMP_Text text = button.GetComponentInChildren<TMP_Text>();
                    if (text != null)
                    {
                        // テキストを空にする
                        text.text = string.Empty;
                    }

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
                        Debug.LogWarning("ボタンにImageがアタッチされていません。");
                    }

                    // ボタンテキストの生成
                    CreateStageText(rect, pageCount + 1, index + 1);

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

                    // テキストを空にする
                    TMP_Text text = button.GetComponentInChildren<TMP_Text>();
                    if (text != null)
                    {
                        // テキストを空にする
                        text.text = string.Empty;
                    }

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
                        Debug.LogWarning("ボタンにImageがアタッチされていません。");
                    }

                    // ボタンテキストの生成
                    CreateStageText(rect, pageCount + 2, index + 1);

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

    // ===========================================
    // ステージテキストを生成する関数
    // ===========================================
    private void CreateStageText(RectTransform _parent, int _worldNumber, int _stageNumber)
    {
        // テキストグループを作成
        GameObject group = new GameObject($"Stage {_worldNumber}-{_stageNumber}_Group");
        RectTransform rect = group.AddComponent<RectTransform>();
        rect.SetParent(_parent, false);
        rect.localPosition = Vector3.zero;
        rect.localRotation = Quaternion.identity;

        // ステージテキストを作成
        string stageText = $"{_worldNumber}-{_stageNumber}";
        char[] chars = stageText.ToCharArray();

        for (int i = 0; i < m_ButtonTexturePos.Length; i++)
        {
            // テクスチャを生成
            CreateButtonTexture(rect, chars[i], m_ButtonTexturePos[i]);
        }
    }

    // ===========================================
    // Button Textureを生成する関数
    // ===========================================
    private void CreateButtonTexture(RectTransform _parent, char _key, Vector2 _position)
    {
        // オブジェクトを作成
        GameObject obj = new GameObject($"{_key}");

        // RectTransformとImageを追加
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.SetParent(_parent, false);
        rect.anchoredPosition = _position;
        rect.localScale = Vector3.one;
        Image image = obj.AddComponent<Image>();

        // Keyに対応するテクスチャを設定
        if (m_ButtonTextureDict.TryGetValue(_key, out Sprite sprite))
        {
            image.sprite = sprite;

            // テクスチャのサイズを取得
            Vector2 baseSize = new Vector2(
                sprite.textureRect.width,
                sprite.textureRect.height);
            // サイズをBookLayoutで設定したサイズに合わせる
            Vector2 size = baseSize * m_ButtonTextureSize;

            rect.sizeDelta = size;

            Debug.Log($"テクスチャを生成しました。 Key : {_key}, Size : {size}");
        }
        else
        {
            Debug.LogWarning($"テクスチャが見つかりませんでした。 Key : {_key}");
        }
    }
}
