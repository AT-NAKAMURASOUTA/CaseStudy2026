using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public sealed class StageMenuOpener : MonoBehaviour
{
    private SceneTransitionManager m_SceneManager;

    private void Start()
    {
        m_SceneManager = SceneTransitionManager.GetInstance();
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
        SceneTransitionManager.GetInstance().SceneTransition(SCENETYPE.MENU);
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
