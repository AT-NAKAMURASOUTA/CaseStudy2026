using UnityEngine;

[CreateAssetMenu(fileName = "OutLinProperty", menuName = "Scriptable Objects/OutLinProperty")]
public class OutLinProperty : ScriptableObject
{
    [Header("使用するマテリアル")]
    [SerializeField]
    private Material outlineMaterial;

    public Material OutlineMaterial => outlineMaterial;

    [Header("アウトラインの色")]
    [SerializeField]
    private Color outlineColor = Color.black;

    public Color OutlineColor => outlineColor;

    [Header("アウトラインの太さ")]
    [SerializeField]
    private float outlineThickness = 1f;

    public float OutlineThickness => outlineThickness;
}
