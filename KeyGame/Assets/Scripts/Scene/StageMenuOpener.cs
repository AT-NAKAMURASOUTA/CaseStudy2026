using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public sealed class StageMenuOpener : MonoBehaviour
{
    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            return;
        }

        if (!TryGetCurrentStageType(out SCENETYPE stageType))
        {
            return;
        }

        StageMenuState.SetRestartStage(stageType);
        SceneTransitionManager.GetInstance().SceneTransition(SCENETYPE.MENU);
    }

    private static bool TryGetCurrentStageType(out SCENETYPE stageType)
    {
        switch (SceneManager.GetActiveScene().name)
        {
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
            case "StageSelectScene":
                stageType = SCENETYPE.STAGESELECT;
                return true;
            default:
                stageType = default;
                return false;
        }
    }
}
