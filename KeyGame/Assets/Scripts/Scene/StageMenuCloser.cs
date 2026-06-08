using UnityEngine;
using UnityEngine.InputSystem;

public sealed class StageMenuCloser : MonoBehaviour
{
    private const string RestartStageButtonName = "RestartStage_Button";
    private const string StageSelectButtonName = "StageSelect_Button";
    private const string ReturnTitleButtonName = "ReturnTitle_Button";
    private const float ButtonSpacing = 160f;

    private int m_OpenFrame;

    private void Awake()
    {
        m_OpenFrame = Time.frameCount;
        UpdateButtonVisibility();
    }

    private static void UpdateButtonVisibility()
    {
        bool openedFromStageSelect = StageMenuState.RestartStage == SCENETYPE.STAGESELECT;

        GameObject restartButton = FindButton(RestartStageButtonName);
        GameObject stageSelectButton = FindButton(StageSelectButtonName);
        GameObject returnTitleButton = FindButton(ReturnTitleButtonName);

        SetButtonVisible(restartButton, !openedFromStageSelect);
        SetButtonVisible(stageSelectButton, !openedFromStageSelect);
        SetButtonVisible(returnTitleButton, true);

        LayoutVisibleButtons(restartButton, stageSelectButton, returnTitleButton);
    }

    private static GameObject FindButton(string buttonName)
    {
        GameObject button = GameObject.Find(buttonName);
        if (button == null)
        {
            Debug.Log("Menu button was not found: " + buttonName);
        }

        return button;
    }

    private static void SetButtonVisible(GameObject button, bool visible)
    {
        if (button == null)
        {
            return;
        }

        button.SetActive(visible);
    }

    private static void LayoutVisibleButtons(params GameObject[] buttons)
    {
        int visibleCount = 0;
        foreach (GameObject button in buttons)
        {
            if (button != null && button.activeSelf)
            {
                visibleCount++;
            }
        }

        int visibleIndex = 0;
        foreach (GameObject button in buttons)
        {
            if (button == null || !button.activeSelf)
            {
                continue;
            }

            RectTransform rectTransform = button.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                Vector2 anchoredPosition = rectTransform.anchoredPosition;
                anchoredPosition.y = (visibleCount - 1) * ButtonSpacing * 0.5f - visibleIndex * ButtonSpacing;
                rectTransform.anchoredPosition = anchoredPosition;
            }

            visibleIndex++;
        }
    }

    private void Update()
    {
        if (Time.frameCount == m_OpenFrame)
        {
            return;
        }

        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            return;
        }

        SceneTransitionManager.GetInstance().SceneTransition(StageMenuState.RestartStage);
    }
}
