using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public sealed class SampleGroundAuthoring : MonoBehaviour
{
    private const float GroundSpritePixelsPerUnit = 32f;

    private void Reset()
    {
        var renderer = GetOrAddComponent<SpriteRenderer>();
        renderer.sprite = SampleAssetLoader.LoadSprite("Common/PrototypeSquare.png", GroundSpritePixelsPerUnit);
        renderer.color = new Color(0.19f, 0.24f, 0.31f);
        renderer.sortingOrder = 0;

        var collider2D = GetOrAddComponent<BoxCollider2D>();
        collider2D.size = Vector2.one;
    }

    private T GetOrAddComponent<T>() where T : Component
    {
        var component = GetComponent<T>();
        if (component == null)
        {
            component = gameObject.AddComponent<T>();
        }

        return component;
    }
}
