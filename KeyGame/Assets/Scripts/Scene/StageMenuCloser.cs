using UnityEngine;
using UnityEngine.InputSystem;

public sealed class StageMenuCloser : MonoBehaviour
{
    private const string RestartStageButtonName = "RestartStage_Button";

    private int m_OpenFrame;

    private void Awake()
    {
        m_OpenFrame = Time.frameCount;
        UpdateRestartButtonVisibility();
    }

    private static void UpdateRestartButtonVisibility()
    {
        GameObject restartButton = GameObject.Find(RestartStageButtonName);
        if (restartButton == null)
        {
            return;
        }

        restartButton.SetActive(StageMenuState.RestartStage != SCENETYPE.STAGESELECT);
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
