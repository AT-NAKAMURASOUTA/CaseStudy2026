using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ButtonCreate))]
public class ButtonCreateEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ButtonCreate buttonCreate = (ButtonCreate)target;
        if (GUILayout.Button("Create Buttons"))
        {
            buttonCreate.Create();
        }
    }
} 
