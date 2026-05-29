using System.Collections.Generic;
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
    private Collider2D m_Collider2D;
    private GenerateAlphabet m_Owner;
    private bool m_WasCut;

    private void Awake()
    {
        m_SpriteRenderer = GetComponent<SpriteRenderer>();
        m_Rigidbody2D = GetComponent<Rigidbody2D>();
        m_Collider2D = GetComponent<Collider2D>();
    }

    public void SetOwner(GenerateAlphabet owner)
    {
        m_Owner = owner;
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
        float splitLocalX = Mathf.Lerp(spriteBounds.min.x, spriteBounds.max.x, normalizedSplit);
        GameObject leftFragment = CreateFragment("AlphabetFragment_Left", sourceSprite, leftSprite, new Vector2(leftCenterLocalX, spriteBounds.center.y), splitLocalX, true, -1f, worldCutPoint, bladeMotionDirection);
        GameObject rightFragment = CreateFragment("AlphabetFragment_Right", sourceSprite, rightSprite, new Vector2(rightCenterLocalX, spriteBounds.center.y), splitLocalX, false, 1f, worldCutPoint, bladeMotionDirection);
        m_Owner?.ReplaceAlphabetWithFragments(gameObject, leftFragment, rightFragment);

        // 文字数は減らさず、元の一体だけを置き換える
        Destroy(gameObject);
    }

    private GameObject CreateFragment(string objectName, Sprite sourceSprite, Sprite fragmentSprite, Vector2 localCenter, float splitLocalX, bool keepLeftSide, float directionSign, Vector2 worldCutPoint, Vector2 bladeMotionDirection)
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
        PolygonCollider2D collider2D = fragmentObject.AddComponent<PolygonCollider2D>();
        collider2D.autoTiling = false;
        if (m_Collider2D != null)
        {
            collider2D.sharedMaterial = m_Collider2D.sharedMaterial;
        }
        SetColliderToClippedPhysicsShape(collider2D, sourceSprite, fragmentSprite, localCenter, splitLocalX, keepLeftSide);

        Rigidbody2D fragmentBody = fragmentObject.AddComponent<Rigidbody2D>();
        CopyRigidbodySettings(fragmentBody);
        fragmentBody.linearVelocity = m_Rigidbody2D != null ? m_Rigidbody2D.linearVelocity : Vector2.zero;
        fragmentBody.angularVelocity = directionSign * fragmentAngularVelocity;
        CopyAreaComponents(fragmentObject);

        // 風の影響を受けるようにする
        fragmentObject.AddComponent<AlphabetRigidbody>();

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
        return fragmentObject;
    }

    private void CopyAreaComponents(GameObject fragmentObject)
    {
        if (fragmentObject == null)
        {
            return;
        }

        AlphabetSpecialAreaInUpdate sourceSpecialArea = GetComponent<AlphabetSpecialAreaInUpdate>();
        if (sourceSpecialArea != null)
        {
            fragmentObject.AddComponent<AlphabetSpecialAreaInUpdate>()
                .SetScriptableObject(sourceSpecialArea.GetScriptableObject());
        }

        if (GetComponent<AlphabetRigidbody>() != null)
        {
            fragmentObject.AddComponent<AlphabetRigidbody>();
        }
    }

    private void CopyRigidbodySettings(Rigidbody2D fragmentBody)
    {
        if (fragmentBody == null)
        {
            return;
        }

        if (m_Rigidbody2D == null)
        {
            fragmentBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            return;
        }

        fragmentBody.bodyType = m_Rigidbody2D.bodyType;
        fragmentBody.simulated = m_Rigidbody2D.simulated;
        fragmentBody.useAutoMass = m_Rigidbody2D.useAutoMass;
        fragmentBody.mass = m_Rigidbody2D.mass;
        fragmentBody.gravityScale = m_Rigidbody2D.gravityScale;
        fragmentBody.collisionDetectionMode = m_Rigidbody2D.collisionDetectionMode;
        fragmentBody.sleepMode = m_Rigidbody2D.sleepMode;
        fragmentBody.interpolation = m_Rigidbody2D.interpolation;
        fragmentBody.constraints = m_Rigidbody2D.constraints;
        fragmentBody.freezeRotation = m_Rigidbody2D.freezeRotation;
    }

    private static void SetColliderToClippedPhysicsShape(PolygonCollider2D collider2D, Sprite sourceSprite, Sprite fragmentSprite, Vector2 fragmentLocalCenter, float splitLocalX, bool keepLeftSide)
    {
        if (collider2D == null || sourceSprite == null || fragmentSprite == null)
        {
            return;
        }

        int shapeCount = sourceSprite.GetPhysicsShapeCount();
        if (shapeCount <= 0)
        {
            SetColliderToSpriteBounds(collider2D, fragmentSprite);
            return;
        }

        List<Vector2[]> clippedPaths = new List<Vector2[]>();
        List<Vector2> sourcePath = new List<Vector2>();
        for (int i = 0; i < shapeCount; i++)
        {
            sourcePath.Clear();
            sourceSprite.GetPhysicsShape(i, sourcePath);

            List<Vector2> clippedPath = ClipPathByVerticalLine(sourcePath, splitLocalX, keepLeftSide);
            if (clippedPath.Count < 3)
            {
                continue;
            }

            for (int j = 0; j < clippedPath.Count; j++)
            {
                clippedPath[j] -= fragmentLocalCenter;
            }

            clippedPaths.Add(clippedPath.ToArray());
        }

        if (clippedPaths.Count <= 0)
        {
            SetColliderToSpriteBounds(collider2D, fragmentSprite);
            return;
        }

        collider2D.pathCount = clippedPaths.Count;
        for (int i = 0; i < clippedPaths.Count; i++)
        {
            collider2D.SetPath(i, clippedPaths[i]);
        }
    }

    private static List<Vector2> ClipPathByVerticalLine(List<Vector2> sourcePath, float splitLocalX, bool keepLeftSide)
    {
        List<Vector2> clippedPath = new List<Vector2>();
        if (sourcePath == null || sourcePath.Count <= 0)
        {
            return clippedPath;
        }

        Vector2 previous = sourcePath[sourcePath.Count - 1];
        bool previousInside = IsInsideClipSide(previous.x, splitLocalX, keepLeftSide);

        for (int i = 0; i < sourcePath.Count; i++)
        {
            Vector2 current = sourcePath[i];
            bool currentInside = IsInsideClipSide(current.x, splitLocalX, keepLeftSide);

            if (currentInside != previousInside)
            {
                clippedPath.Add(GetVerticalLineIntersection(previous, current, splitLocalX));
            }

            if (currentInside)
            {
                clippedPath.Add(current);
            }

            previous = current;
            previousInside = currentInside;
        }

        RemoveDuplicatePoints(clippedPath);
        return clippedPath;
    }

    private static bool IsInsideClipSide(float x, float splitLocalX, bool keepLeftSide)
    {
        return keepLeftSide ? x <= splitLocalX : x >= splitLocalX;
    }

    private static Vector2 GetVerticalLineIntersection(Vector2 from, Vector2 to, float x)
    {
        float denominator = to.x - from.x;
        if (Mathf.Approximately(denominator, 0f))
        {
            return new Vector2(x, from.y);
        }

        float t = Mathf.Clamp01((x - from.x) / denominator);
        return Vector2.Lerp(from, to, t);
    }

    private static void RemoveDuplicatePoints(List<Vector2> points)
    {
        const float duplicateDistanceSqr = 0.000001f;
        for (int i = points.Count - 1; i > 0; i--)
        {
            if ((points[i] - points[i - 1]).sqrMagnitude <= duplicateDistanceSqr)
            {
                points.RemoveAt(i);
            }
        }

        if (points.Count > 1 && (points[0] - points[points.Count - 1]).sqrMagnitude <= duplicateDistanceSqr)
        {
            points.RemoveAt(points.Count - 1);
        }
    }

    private static void SetColliderToSpriteBounds(PolygonCollider2D collider2D, Sprite sprite)
    {
        Bounds bounds = sprite.bounds;
        Vector2 min = bounds.min;
        Vector2 max = bounds.max;

        collider2D.pathCount = 1;
        collider2D.SetPath(0, new[]
        {
            new Vector2(min.x, min.y),
            new Vector2(min.x, max.y),
            new Vector2(max.x, max.y),
            new Vector2(max.x, min.y)
        });
    }
}
