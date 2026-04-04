using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(CircleCollider2D))]
public sealed class SawBlade : MonoBehaviour
{
    [Header("刃の回転速度")]
    [SerializeField]
    private float rotationSpeed = 240f;

    [Header("刃の半径")]
    [SerializeField]
    private float bladeRadius = 0.55f;

    [Header("刃の色")]
    [SerializeField]
    private Color bladeColor = new Color(0.8f, 0.82f, 0.86f, 1f);

    [Header("中心部の色")]
    [SerializeField]
    private Color hubColor = new Color(0.24f, 0.18f, 0.12f, 1f);

    [Header("支柱の色")]
    [SerializeField]
    private Color supportColor = new Color(0.42f, 0.33f, 0.26f, 1f);

    [Header("見た目全体のスケール")]
    [SerializeField]
    private float visualScale = 1f;

    private Transform m_BladeVisualRoot;
    private SpriteRenderer m_BladeRenderer;
    private SpriteRenderer m_HubRenderer;
    private CircleCollider2D m_SolidCollider;
    private CircleCollider2D m_TriggerCollider;

    private void Reset()
    {
        SetupColliders();
        EnsureVisuals();
    }

    private void Awake()
    {
        SetupColliders();
        EnsureVisuals();
    }

    private void OnEnable()
    {
        SetupColliders();
        EnsureVisuals();
    }

    private void OnValidate()
    {
        SetupColliders();
    }

    private void Update()
    {
        if (m_BladeVisualRoot == null)
        {
            return;
        }

        float delta = Application.isPlaying ? Time.deltaTime : Time.unscaledDeltaTime;
        m_BladeVisualRoot.Rotate(0f, 0f, -rotationSpeed * delta);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null)
        {
            return;
        }

        // プレイヤーが触れたらミス
        PlayerRespawn playerRespawn = other.GetComponent<PlayerRespawn>();
        if (playerRespawn != null)
        {
            playerRespawn.TriggerMiss();
            return;
        }

        // 文字オブジェクトに触れた場合は切断処理を行う
        AlphabetCuttable cuttable = other.GetComponent<AlphabetCuttable>();
        if (cuttable == null)
        {
            cuttable = other.GetComponentInParent<AlphabetCuttable>();
        }

        if (cuttable == null)
        {
            return;
        }

        // 接触位置と刃の接線方向を求めて切る向きを渡す
        Vector2 cutPoint = other.ClosestPoint(transform.position);
        Vector2 radial = cutPoint - (Vector2)transform.position;
        if (radial.sqrMagnitude <= 0.0001f)
        {
            radial = Vector2.right;
        }
        radial.Normalize();

        // 時計回り回転に合わせた接線方向
        Vector2 tangential = new Vector2(radial.y, -radial.x);
        cuttable.Cut(cutPoint, tangential);
    }

    private void SetupColliders()
    {
        CircleCollider2D[] circleColliders = GetComponents<CircleCollider2D>();
        m_SolidCollider = null;
        m_TriggerCollider = null;

        // 既存のCircleCollider2Dから通常判定用とTrigger用を探す
        foreach (CircleCollider2D collider2D in circleColliders)
        {
            if (collider2D.isTrigger && m_TriggerCollider == null)
            {
                m_TriggerCollider = collider2D;
                continue;
            }

            if (!collider2D.isTrigger && m_SolidCollider == null)
            {
                m_SolidCollider = collider2D;
            }
        }

        // 通常の当たり判定用Collider
        if (m_SolidCollider == null)
        {
            m_SolidCollider = gameObject.AddComponent<CircleCollider2D>();
        }
        m_SolidCollider.isTrigger = false;
        m_SolidCollider.radius = bladeRadius;

        // 接触検知用のTriggerCollider
        if (m_TriggerCollider == null)
        {
            m_TriggerCollider = gameObject.AddComponent<CircleCollider2D>();
        }
        m_TriggerCollider.isTrigger = true;
        m_TriggerCollider.radius = bladeRadius;
    }

    private void EnsureVisuals()
    {
        // 支柱部分
        Transform support = GetOrCreateChild("Support");
        SpriteRenderer supportRenderer = GetOrCreateRenderer(support);
        support.localPosition = new Vector3(0f, -0.95f, 0f) * visualScale;
        support.localRotation = Quaternion.identity;
        support.localScale = new Vector3(0.14f, 1.1f, 1f) * visualScale;
        supportRenderer.color = supportColor;

        // 回転する刃全体の親
        m_BladeVisualRoot = GetOrCreateChild("BladeVisual");
        m_BladeVisualRoot.localPosition = Vector3.zero;
        m_BladeVisualRoot.localRotation = Quaternion.identity;
        m_BladeVisualRoot.localScale = Vector3.one * visualScale;

        // 刃本体
        Transform core = GetOrCreateChildUnderRoot("Core", m_BladeVisualRoot);
        m_BladeRenderer = GetOrCreateRenderer(core);
        core.localPosition = Vector3.zero;
        core.localRotation = Quaternion.identity;
        core.localScale = Vector3.one;
        m_BladeRenderer.sprite = BladeSpriteFactory.GetBladeSprite();
        m_BladeRenderer.color = bladeColor;

        // 中心部分
        Transform hub = GetOrCreateChildUnderRoot("Hub", m_BladeVisualRoot);
        m_HubRenderer = GetOrCreateRenderer(hub);
        hub.localPosition = Vector3.zero;
        hub.localRotation = Quaternion.identity;
        hub.localScale = new Vector3(bladeRadius * 0.78f, bladeRadius * 0.78f, 1f);
        m_HubRenderer.sprite = WhiteSprite.Value;
        m_HubRenderer.color = hubColor;
    }

    private Transform GetOrCreateChild(string childName)
    {
        Transform child = transform.Find(childName);

        if (child != null)
        {
            return child;
        }

        // なければ新しく子オブジェクトを作る
        GameObject childObject = new GameObject(childName, typeof(SpriteRenderer));
        child = childObject.transform;
        child.SetParent(transform, false);
        return child;
    }

    private Transform GetOrCreateChildUnderRoot(string childName, Transform parent)
    {
        Transform child = parent.Find(childName);

        if (child != null)
        {
            return child;
        }

        // 指定した親の下になければ作る
        GameObject childObject = new GameObject(childName, typeof(SpriteRenderer));
        child = childObject.transform;
        child.SetParent(parent, false);
        return child;
    }

    private SpriteRenderer GetOrCreateRenderer(Transform visualTransform)
    {
        SpriteRenderer renderer = visualTransform.GetComponent<SpriteRenderer>();

        if (renderer == null)
        {
            renderer = visualTransform.gameObject.AddComponent<SpriteRenderer>();
        }

        // デフォルトでは白スプライトを使って色で見た目を作る
        renderer.sprite = WhiteSprite.Value;
        renderer.sortingOrder = 6;
        return renderer;
    }

    private static class WhiteSprite
    {
        private static Sprite s_Value;

        public static Sprite Value
        {
            get
            {
                if (s_Value == null)
                {
                    s_Value = Sprite.Create(
                        Texture2D.whiteTexture,
                        new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
                        new Vector2(0.5f, 0.5f),
                        100f);
                }

                return s_Value;
            }
        }
    }

    private static class BladeSpriteFactory
    {
        private static Sprite s_BladeSprite;

        public static Sprite GetBladeSprite()
        {
            if (s_BladeSprite != null)
            {
                return s_BladeSprite;
            }

            const int size = 256;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;

            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float baseOuterRadius = size * 0.405f;
            float innerRadius = 0f;
            float toothTipRadius = size * 0.458f;
            int teethCount = 22;
            float toothSpan = Mathf.PI * 2f / teethCount;
            float toothStartRatio = 0.12f;
            float toothPeakRatio = 0.34f;
            float toothEndRatio = 0.84f;
            float shoulderRadius = size * 0.432f;
            float valleyRadius = size * 0.39f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 point = new Vector2(x + 0.5f, y + 0.5f) - center;
                    float radius = point.magnitude;
                    float angle = Mathf.Atan2(point.y, point.x);
                    if (angle < 0f)
                    {
                        angle += Mathf.PI * 2f;
                    }

                    float toothIndex = Mathf.Floor(angle / toothSpan);
                    float localAngle = angle - toothIndex * toothSpan;
                    float localRatio = localAngle / toothSpan;

                    float toothRadius = valleyRadius;
                    if (localRatio < toothStartRatio)
                    {
                        toothRadius = Mathf.Lerp(valleyRadius, baseOuterRadius, localRatio / toothStartRatio);
                    }
                    else if (localRatio < toothPeakRatio)
                    {
                        float t = Mathf.InverseLerp(toothStartRatio, toothPeakRatio, localRatio);
                        toothRadius = Mathf.Lerp(baseOuterRadius, toothTipRadius, t);
                    }
                    else if (localRatio < toothEndRatio)
                    {
                        float t = Mathf.InverseLerp(toothPeakRatio, toothEndRatio, localRatio);
                        toothRadius = Mathf.Lerp(toothTipRadius, shoulderRadius, t);
                    }
                    else
                    {
                        float t = Mathf.InverseLerp(toothEndRatio, 1f, localRatio);
                        toothRadius = Mathf.Lerp(shoulderRadius, valleyRadius, t);
                    }

                    bool insideBlade = radius <= toothRadius && radius >= innerRadius;
                    if (!insideBlade)
                    {
                        texture.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    
                    float normalizedRadius = toothTipRadius > 0f ? radius / toothTipRadius : 0f;
                    float radialShade = Mathf.Lerp(0.84f, 0.8f, normalizedRadius);
                    float rimHighlight = Mathf.SmoothStep(0f, 1f, 1f - Mathf.Abs(normalizedRadius - 0.82f) / 0.12f) * 0.025f;
                    float centerHighlight = Mathf.SmoothStep(0f, 1f, 1f - normalizedRadius / 0.45f) * 0.012f;
                    float colorValue = Mathf.Clamp01(radialShade + rimHighlight + centerHighlight);
                    texture.SetPixel(x, y, new Color(colorValue, colorValue, colorValue, 1f));
                }
            }

            texture.Apply();

            s_BladeSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.Tight);

            return s_BladeSprite;
        }
    }
}
