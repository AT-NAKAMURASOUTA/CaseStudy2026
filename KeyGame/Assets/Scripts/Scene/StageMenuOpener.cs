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
                stageType = SCENETYPE.STAGE1;
                return true;
            case "Stage1-2":
                stageType = SCENETYPE.STAGE2;
                return true;
            case "stage1-3":
                stageType = SCENETYPE.STAGE3;
                return true;
            case "Stage1-4":
                stageType = SCENETYPE.STAGE4;
                return true;
            case "stage1-5":
                stageType = SCENETYPE.STAGE5;
                return true;
            case "Stage1-6":
                stageType = SCENETYPE.STAGE6;
                return true;
            default:
                stageType = default;
                return false;
        }
    }
}
