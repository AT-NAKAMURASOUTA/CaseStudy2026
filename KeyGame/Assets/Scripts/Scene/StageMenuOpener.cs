using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public sealed class StageMenuOpener : MonoBehaviour
{
    [SerializeField] private GameObject m_MenuPrefab;

    private SceneTransitionManager m_SceneManager;
    private CanvasGroup m_CanvasGroup;
    private GameObject m_MenuInstance;
    private bool m_IsOverlayMenuActive;

    private void Start()
    {
        m_SceneManager = SceneTransitionManager.GetInstance();
        m_CanvasGroup = GetComponent<CanvasGroup>();
    }

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            return;
        }

        SCENETYPE returnScene = TryGetCurrentStageType(out SCENETYPE sceneType)
            ? sceneType
            : m_SceneManager.GetCurrentSceneType();

        StageMenuState.SetRestartStage(returnScene);

        if (m_MenuPrefab != null)
        {
            ToggleOverlayMenu(returnScene);
            return;
        }

        m_SceneManager.SceneTransition(SCENETYPE.MENU);
    }

    private void ToggleOverlayMenu(SCENETYPE returnScene)
    {
        m_IsOverlayMenuActive = !m_IsOverlayMenuActive;

        if (m_MenuInstance == null)
        {
            m_MenuInstance = Instantiate(m_MenuPrefab);
            ConfigureOverlayMenu(m_MenuInstance, returnScene);
        }

        m_MenuInstance.SetActive(m_IsOverlayMenuActive);
        SetSourceInteraction(!m_IsOverlayMenuActive);
    }

    private void SetSourceInteraction(bool enabled)
    {
        if (m_CanvasGroup == null)
        {
            return;
        }

        m_CanvasGroup.blocksRaycasts = enabled;
    }

    private static void ConfigureOverlayMenu(GameObject menu, SCENETYPE returnScene)
    {
        if (menu == null)
        {
            return;
        }

        bool hidesStageButtons = returnScene == SCENETYPE.STAGESELECT || returnScene == SCENETYPE.CONFIG;
        GameObject restartButton = FindChild(menu.transform, "RestartStage_Button");
        GameObject stageSelectButton = FindChild(menu.transform, "StageSelect_Button");
        GameObject returnTitleButton = FindChild(menu.transform, "ReturnTitle_Button");

        SetButtonVisible(restartButton, !hidesStageButtons);
        SetButtonVisible(stageSelectButton, !hidesStageButtons);
        SetButtonVisible(returnTitleButton, true);
        LayoutVisibleButtons(restartButton, stageSelectButton, returnTitleButton);
    }

    private static GameObject FindChild(Transform root, string objectName)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == objectName)
        {
            return root.gameObject;
        }

        foreach (Transform child in root)
        {
            GameObject found = FindChild(child, objectName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static void SetButtonVisible(GameObject button, bool visible)
    {
        if (button != null)
        {
            button.SetActive(visible);
        }
    }

    private static void LayoutVisibleButtons(params GameObject[] buttons)
    {
        const float buttonSpacing = 160f;
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
                anchoredPosition.y = (visibleCount - 1) * buttonSpacing * 0.5f - visibleIndex * buttonSpacing;
                rectTransform.anchoredPosition = anchoredPosition;
            }

            visibleIndex++;
        }
    }

    private static bool TryGetCurrentStageType(out SCENETYPE stageType)
    {
        switch (SceneManager.GetActiveScene().name)
        {
            case "StageSelectScene":
                stageType = SCENETYPE.STAGESELECT;
                return true;
            case "Stage1-1":
                stageType = SCENETYPE.STAGE1_1;
                return true;
            case "Stage1-2":
                stageType = SCENETYPE.STAGE1_2;
                return true;
            case "stage1-3":
                stageType = SCENETYPE.STAGE1_3;
                return true;
            case "Stage1-4":
                stageType = SCENETYPE.STAGE1_4;
                return true;
            case "stage1-5":
                stageType = SCENETYPE.STAGE1_5;
                return true;
            case "Stage1-6":
                stageType = SCENETYPE.STAGE1_6;
                return true;
            case "Stage2-1":
                stageType = SCENETYPE.STAGE2_1;
                return true;
            case "Stage2-2":
                stageType = SCENETYPE.STAGE2_2;
                return true;
            case "Stage2-3":
                stageType = SCENETYPE.STAGE2_3;
                return true;
            case "Stage2-4":
                stageType = SCENETYPE.STAGE2_4;
                return true;
            case "Stage2-5":
                stageType = SCENETYPE.STAGE2_5;
                return true;
            case "Stage2-6":
                stageType = SCENETYPE.STAGE2_6;
                return true;
            default:
                stageType = default;
                return false;
        }
    }
}
