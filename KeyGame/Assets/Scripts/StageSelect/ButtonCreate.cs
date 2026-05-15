using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
    [SerializeField] private System.Collections.Generic.List<ButtonData> m_ButtonData;

    // ボタンの位置リスト
    private System.Collections.Generic.List<Vector2> m_ButtonPositions;
    private int m_ButtonCount;
    // 生成したボタンのリスト
    private List<GameObject> m_CreatedButtons = new();

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

        // エラーチェック
        if (m_ButtonPrefab == null)
        {
            UnityEngine.Debug.LogError("ボタンのプレハブが設定されていません。");
            return;
        }
        if(m_ButtonData.Count == 0)
        {
            UnityEngine.Debug.LogError("ボタンのデータが設定されていません。");
            return;
        }
        if (m_ButtonPositions.Count == 0)
        {
            UnityEngine.Debug.LogError("ボタンの位置が設定されていません。");
            return;
        }        if (m_ButtonData.Count != m_ButtonPositions.Count)
        {
            UnityEngine.Debug.LogError("ボタンのデータと位置の数が一致していません。");
            return;
        }

        // ボタンの生成
        for (int i = 0; i < m_ButtonData.Count; i++)
        {
            // ボタンの生成
            GameObject button = Instantiate(m_ButtonPrefab, transform);
            button.transform.localPosition = m_ButtonPositions[i];

            m_CreatedButtons.Add(button);

            // ボタンのデータを設定
            Action_LoadTargetScene loadScene = button.GetComponent<Action_LoadTargetScene>();
            if(loadScene != null)
            {
                loadScene.Init(m_ButtonData[i].NextScene);
            }
            else
            {
                UnityEngine.Debug.LogWarning("ボタンにLoadSceneがアタッチされていません。");
            }

            // 画像設定
            Image image = button.GetComponent<Image>();

            if (image != null)
            {
                image.sprite = m_ButtonData[i].Texture;
            }
            else
            {
                Debug.LogWarning(
                    "ボタンにImageがアタッチされていません。");
            }
        }

    }

    // ===========================================
    // ボタン削除関数
    // ===========================================
    private void ClearButtons()
    {
        // 既存のボタンを削除
        foreach (var button in m_CreatedButtons)
        {
            if (button != null)
            {
                DestroyImmediate(button);
            }
        }
        // 生成したボタンのリストをクリア
        m_CreatedButtons.Clear();
    }
}
