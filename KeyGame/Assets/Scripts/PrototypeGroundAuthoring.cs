using UnityEngine;

// プロトタイプ用の地面に、共通の見た目と当たり判定を付与する。
[ExecuteAlways]
public sealed class PrototypeGroundAuthoring : MonoBehaviour
{
    private void Awake()
    {
        EnsureComponents();
    }

    private void OnEnable()
    {
        EnsureComponents();
    }

    private void OnValidate()
    {
        EnsureComponents();
    }

    private void EnsureComponents()
    {
        // 地面は共通の四角スプライトを使い、色だけを地面用に固定する。
        var renderer = GetOrAddComponent<SpriteRenderer>();
        renderer.sprite = Resources.Load<Sprite>("PrototypeSquare");
        renderer.color = new Color(0.19f, 0.24f, 0.31f);
        renderer.sortingOrder = 0;

        // 見た目と当たり判定の形を合わせるため、BoxCollider2D を必須にする。
        GetOrAddComponent<BoxCollider2D>();
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
