using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/*  * ボタンを生成するスクリプト
 */

[RequireComponent(typeof(BookLayout))]
public class ButtonCreate : MonoBehaviour
{
    // ===========================================
    // メンバー変数
    // ===========================================
    [Header("ボタン設定")]
    [Tooltip("ボタンのプレハブ")]
    [SerializeField] private GameObject m_ButtonPrefab;
    [Tooltip("ボタンのデータ")]
    [SerializeField] private List<StageButtonData> m_ButtonData;

    // ボタンの位置リスト
    private List<Vector2> m_ButtonPositions;
    // ボタンの数
    private int m_ButtonCount;
    // ボタンのサイズ
    private Vector2 m_ButtonSize;
    // 次のボタンの位置
    private Vector2 m_NextButtonPosition;
    // 生成ボタンにつけるタグ
    private const string m_ButtonTag = "StageSelectButton";

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
        m_ButtonCount = layout.GetButtonMaxNumber();
        m_ButtonSize = layout.GetButtonSize();
        m_NextButtonPosition = layout.GetNextButtonPosition();

        // エラーチェック
        if (m_ButtonPrefab == null)
        {
            UnityEngine.Debug.LogError("ボタンのプレハブが設定されていません。");
            return;
        }
        if (m_ButtonData.Count == 0)
        {
            UnityEngine.Debug.LogError("ボタンのデータが設定されていません。");
            return;
        }
        if (m_ButtonPositions.Count == 0)
        {
            UnityEngine.Debug.LogError("ボタンの位置が設定されていません。");
            return;
        }
        if (m_ButtonData.Count != m_ButtonCount)
        {
            UnityEngine.Debug.LogWarning("ボタンのデータと位置の数が一致していません。\nボタンデータ数のみ生成します");
        }

        // ボタンの生成
        ButtonsCreate();
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
    private void ButtonsCreate()
    {
        // Stage1 のデータを取得
        List<ButtonData> buttonData = m_ButtonData[0].stageDataList;

        for (int i = 0; i < buttonData.Count; i++)
        {
            // ボタンの生成
            GameObject button = Instantiate(m_ButtonPrefab, transform);
            button.transform.localPosition = m_ButtonPositions[i];
            // ボタンのサイズ設定
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.sizeDelta = m_ButtonSize;
            // ボタンの名前設定
            button.gameObject.name = $"Stage{i + 1}_Button";
            // ボタンのタグ設定
            button.gameObject.tag = m_ButtonTag;

            // ボタンのデータを設定
            Action_LoadTargetScene loadScene = button.GetComponent<Action_LoadTargetScene>();
            if (loadScene != null)
            {
                loadScene.Init(buttonData[i].NextScene);
            }
            else
            {
                UnityEngine.Debug.LogWarning("ボタンにLoadSceneがアタッチされていません。");
            }

            // 画像設定
            Image image = button.GetComponent<Image>();

            if (buttonData[i].Texture != null)
            {
                image.sprite = buttonData[i].Texture;
            }
            else
            {
                TMP_Text text = button.GetComponentInChildren<TMP_Text>();
                text.text = $"Stage {i + 1}";
                Debug.LogWarning("ボタンにImageがアタッチされていません。");
            }
        }

        // 次のボタン生成
        GameObject nextButton = Instantiate(m_ButtonPrefab, transform);
        nextButton.transform.localPosition = m_NextButtonPosition;
        // ボタンのサイズ設定
        RectTransform nextRect = nextButton.GetComponent<RectTransform>();
        nextRect.sizeDelta = m_ButtonSize;
        // ボタンの名前設定
        nextButton.gameObject.name = $"NextStage_Button";
        // ボタンのタグ設定
        nextButton.gameObject.tag = m_ButtonTag;    
    }
}
