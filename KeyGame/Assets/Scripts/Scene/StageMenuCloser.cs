using UnityEngine;
using UnityEngine.InputSystem;

public sealed class StageMenuCloser : MonoBehaviour
{
    private int m_OpenFrame;

    private void Awake()
    {
        m_OpenFrame = Time.frameCount;
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
