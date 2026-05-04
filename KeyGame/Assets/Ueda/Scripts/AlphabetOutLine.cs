using UnityEngine;

public class AlphabetOutLine : MonoBehaviour
{
    public OutLinProperty outLinProperty;

    private void Start()
    {
        Debug.Log("Start: " + gameObject.name);

        if (!TryGetComponent(out SpriteRenderer spriteRenderer))
        {
            Debug.LogWarning("SpriteRendererが見つかりませんでした。");
            return;
        }

        var mat = spriteRenderer.material;

        if (outLinProperty != null)
        {
            mat = Instantiate(outLinProperty.OutlineMaterial); // マテリアルのインスタンスを作成して変更を適用

            Debug.Log("MAterial NAme: " + mat.name);

            mat.SetTexture("_MainTex", spriteRenderer.sprite.texture);
            mat.SetFloat("_Offset", outLinProperty.OutlineThickness);

            spriteRenderer.material = mat;
        }
        else
        {
            Debug.LogWarning("OutLinPropertyが設定されていません。");
        }
    }
}
