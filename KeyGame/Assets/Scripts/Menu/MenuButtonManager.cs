using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/*  * メニュー用ボタンの管理を行うクラス
 */
[System.Serializable]
public class ButtonRow
{
    public Button[] Buttons;
}

public class MenuButtonManager : MonoBehaviour
{
    // ボタン配列
    [SerializeField] private ButtonRow[] m_Buttons;
    private PlayerInput m_PlayerInput;
    private int m_CurrentRow = 0, m_CurrentColumn = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void Init(PlayerInput _playerInput)
    {
        m_PlayerInput = _playerInput;
        m_PlayerInput.actions["MenuNext"].performed += OnMenuNext;
        m_PlayerInput.actions["MenuPrevious"].performed += OnMenuPrevious;
        m_PlayerInput.actions["MenuUp"].performed += OnMenuUp;
        m_PlayerInput.actions["MenuDown"].performed += OnMenuDown;
    }
    private void OnEnable()
    {
        if (m_PlayerInput == null) return;

        ActiveOperation();
    }

    private void OnDisable()
    {
        if (m_PlayerInput == null) return;

        FalseOperation();
    }

    private void OnMenuNext(InputAction.CallbackContext context)
    {
        Debug.Log("MenuNext");
        SelectRight();
    }

    private void OnMenuPrevious(InputAction.CallbackContext context)
    {
        Debug.Log("MenuPrevious");
        SelectLeft();
    }

    private void OnMenuUp(InputAction.CallbackContext context)
    {
        Debug.Log("MenuUp");
        SelectUp();
    }

    private void OnMenuDown(InputAction.CallbackContext context)
    {
        Debug.Log("MenuDown");
        SelectDown();
    }

    private void SelectRight()
    {
        if (m_Buttons.Length == 0) { return; }
        if (m_CurrentColumn + 1 >= m_Buttons[m_CurrentRow].Buttons.Length) { return; }

        m_CurrentColumn++;
        m_Buttons[m_CurrentRow].Buttons[m_CurrentColumn].Select();
    }
    private void SelectLeft()
    {
        if (m_Buttons.Length == 0) { return; }
        if (m_CurrentColumn - 1 < 0) { return; }

        m_CurrentColumn--;
        m_Buttons[m_CurrentRow].Buttons[m_CurrentColumn].Select();
    }
    private void SelectUp()
    {
        if (m_Buttons.Length == 0) { return; }
        if (m_CurrentRow - 1 < 0) { return; }

        m_CurrentRow--;
        m_Buttons[m_CurrentRow].Buttons[m_CurrentColumn].Select();
    }
    private void SelectDown()
    {
        if (m_Buttons.Length == 0) { return; }
        if (m_CurrentRow + 1 >= m_Buttons.Length) { return; }

        m_CurrentRow++;
        m_Buttons[m_CurrentRow].Buttons[m_CurrentColumn].Select();
    }

    public void ActiveOperation()
    {
        m_PlayerInput.actions["MenuNext"].performed += OnMenuNext;
        m_PlayerInput.actions["MenuPrevious"].performed += OnMenuPrevious;
        m_PlayerInput.actions["MenuUp"].performed += OnMenuUp;
        m_PlayerInput.actions["MenuDown"].performed += OnMenuDown;
    }
    public void FalseOperation()
    {
        m_PlayerInput.actions["MenuNext"].performed -= OnMenuNext;
        m_PlayerInput.actions["MenuPrevious"].performed -= OnMenuPrevious;
        m_PlayerInput.actions["MenuUp"].performed -= OnMenuUp;
        m_PlayerInput.actions["MenuDown"].performed -= OnMenuDown;
    }

}
