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
        m_PlayerInput.actions["MenuNext"].performed += _ => SelectRight();
        m_PlayerInput.actions["MenuPrevious"].performed += _ => SelectLeft();
        m_PlayerInput.actions["MenuUp"].performed += _ => SelectUp();
        m_PlayerInput.actions["MenuDown"].performed += _ => SelectDown();
    }

    private void SelectRight()
    {
        if (m_Buttons.Length == 0) { return; }

        m_CurrentColumn++;
        if (m_CurrentColumn >= m_Buttons[m_CurrentRow].Buttons.Length)
        {
            m_CurrentColumn = 0;
        }
        m_Buttons[m_CurrentRow].Buttons[m_CurrentColumn].Select();
    }
    private void SelectLeft()
    {
        if (m_Buttons.Length == 0) { return; }

        m_CurrentColumn--;
        if (m_CurrentColumn < 0)
        {
            m_CurrentColumn = m_Buttons[m_CurrentRow].Buttons.Length - 1;
        }
        m_Buttons[m_CurrentRow].Buttons[m_CurrentColumn].Select();
    }
    private void SelectUp()
    {
        if (m_Buttons.Length == 0) { return; }

        m_CurrentRow--;
        if (m_CurrentRow < 0)
        {
            m_CurrentRow = m_Buttons.Length - 1;
        }
        m_Buttons[m_CurrentRow].Buttons[m_CurrentColumn].Select();
    }
    private void SelectDown()
    {
        if (m_Buttons.Length == 0) { return; }

        m_CurrentRow++;
        if (m_CurrentRow >= m_Buttons.Length)
        {
            m_CurrentRow = 0;
        }
        m_Buttons[m_CurrentRow].Buttons[m_CurrentColumn].Select();
    }
}
