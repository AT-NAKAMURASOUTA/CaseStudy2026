using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Rigidbody2D))]
public sealed class AlphabetCuttable : MonoBehaviour
{
    [Header("切断後の左右の飛び散り")]
    [SerializeField]
    private float outwardImpulse = 0.9f;

    [Header("丸鋸の回転方向に引っ張る強さ")]
    [SerializeField]
    private float tangentialImpulse = 0.55f;

    [Header("切断後の回転")]
    [SerializeField]
    private float fragmentAngularVelocity = 240f;

    private SpriteRenderer m_SpriteRenderer;
    private Rigidbody2D m_Rigidbody2D;
    private bool m_WasCut;

    private void Awake()
    {
        m_SpriteRenderer = GetComponent<SpriteRenderer>();
        m_Rigidbody2D = GetComponent<Rigidbody2D>();
    }

    public void SetOwner(GenerateAlphabet _)
    {
        // 切断では文字数を減らさない。落下時のカウント管理はDestroyOnFallが担当する。
    }

    public void Cut(Vector2 worldCutPoint)
    {
        Cut(worldCutPoint, Vector2.zero);
    }

    public void Cut(Vector2 worldCutPoint, Vector2 bladeMotionDirection)
    {
        // すでに切断済みまたはスプライトが取れていない場合は何もしない
        if (m_WasCut || m_SpriteRenderer == null || m_SpriteRenderer.sprite == null)
        {
            return;
        }

        Sprite sourceSprite = m_SpriteRenderer.sprite;
        Rect sourceRect = sourceSprite.textureRect;

        // 幅が小さすぎるものは切らない
        if (sourceRect.width < 8f)
        {
            return;
        }

        Bounds spriteBounds = sourceSprite.bounds;
        const float normalizedSplit = 0.5f;

        int splitPixel = Mathf.RoundToInt(sourceRect.width * normalizedSplit);
        splitPixel = Mathf.Clamp(splitPixel, 4, Mathf.RoundToInt(sourceRect.width) - 4);

        int leftWidth = splitPixel;
        int rightWidth = Mathf.RoundToInt(sourceRect.width) - splitPixel;
        if (leftWidth < 4 || rightWidth < 4)
        {
            return;
        }

        m_WasCut = true;

        float leftNormalizedWidth = normalizedSplit;
        float rightNormalizedWidth = 1f - normalizedSplit;
        float leftCenterLocalX = Mathf.Lerp(spriteBounds.min.x, spriteBounds.max.x, leftNormalizedWidth * 0.5f);
        float rightCenterLocalX = Mathf.Lerp(spriteBounds.min.x, spriteBounds.max.x, normalizedSplit + rightNormalizedWidth * 0.5f);

        // 元のスプライトを分けたスプライトを作る
        Sprite leftSprite = Sprite.Create(
            sourceSprite.texture,
            new Rect(sourceRect.x, sourceRect.y, leftWidth, sourceRect.height),
            new Vector2(0.5f, 0.5f),
            sourceSprite.pixelsPerUnit,
            0,
            SpriteMeshType.Tight);

        Sprite rightSprite = Sprite.Create(
            sourceSprite.texture,
            new Rect(sourceRect.x + splitPixel, sourceRect.y, rightWidth, sourceRect.height),
            new Vector2(0.5f, 0.5f),
            sourceSprite.pixelsPerUnit,
            0,
            SpriteMeshType.Tight);

        // 左右それぞれの破片を作る
        CreateFragment("AlphabetFragment_Left", leftSprite, new Vector2(leftCenterLocalX, spriteBounds.center.y), -1f, worldCutPoint, bladeMotionDirection);
        CreateFragment("AlphabetFragment_Right", rightSprite, new Vector2(rightCenterLocalX, spriteBounds.center.y), 1f, worldCutPoint, bladeMotionDirection);

        // 文字数は減らさず、元の一体だけを置き換える
        Destroy(gameObject);
    }

    private void CreateFragment(string objectName, Sprite fragmentSprite, Vector2 localCenter, float directionSign, Vector2 worldCutPoint, Vector2 bladeMotionDirection)
    {
        GameObject fragmentObject = new GameObject(objectName);
        fragmentObject.tag = gameObject.tag;
        fragmentObject.layer = gameObject.layer;
        fragmentObject.transform.position = transform.TransformPoint(new Vector3(localCenter.x, localCenter.y, 0f));
        fragmentObject.transform.rotation = transform.rotation;
        fragmentObject.transform.localScale = transform.lossyScale;

        // 見た目を元の文字から引き継ぐ
        SpriteRenderer fragmentRenderer = fragmentObject.AddComponent<SpriteRenderer>();
        fragmentRenderer.sprite = fragmentSprite;
        fragmentRenderer.sortingLayerID = m_SpriteRenderer.sortingLayerID;
        fragmentRenderer.sortingOrder = m_SpriteRenderer.sortingOrder;
        fragmentRenderer.color = m_SpriteRenderer.color;
        fragmentRenderer.sharedMaterial = m_SpriteRenderer.sharedMaterial;

        // 左右の破片を別々の物理オブジェクトとして動かす
        Rigidbody2D fragmentBody = fragmentObject.AddComponent<Rigidbody2D>();
        fragmentBody.gravityScale = m_Rigidbody2D != null ? m_Rigidbody2D.gravityScale : 1f;
        fragmentBody.linearVelocity = m_Rigidbody2D != null ? m_Rigidbody2D.linearVelocity : Vector2.zero;
        fragmentBody.angularVelocity = directionSign * fragmentAngularVelocity;
        fragmentBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        PolygonCollider2D collider2D = fragmentObject.AddComponent<PolygonCollider2D>();
        collider2D.autoTiling = false;

        // 切れた位置から外側へ飛ぶ向きを求める
        Vector2 outwardDirection = ((Vector2)fragmentObject.transform.position - worldCutPoint).normalized;
        if (outwardDirection.sqrMagnitude <= 0.001f)
        {
            outwardDirection = directionSign > 0f ? Vector2.right : Vector2.left;
        }

        // 丸鋸の回転方向にも少し流されるようにする
        Vector2 tangentDirection = bladeMotionDirection.normalized;
        Vector2 impulse = outwardDirection * outwardImpulse;
        if (tangentDirection.sqrMagnitude > 0.001f)
        {
            impulse += tangentDirection * tangentialImpulse;
        }

        fragmentBody.AddForce(impulse, ForceMode2D.Impulse);
    }
}
