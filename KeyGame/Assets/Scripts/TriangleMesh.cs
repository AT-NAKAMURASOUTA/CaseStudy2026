using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(PolygonCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]

public class TriangleMesh : MonoBehaviour
{
    [SerializeField]
    private Vector2[] points =
    {
        new Vector2(0, 0),
        new Vector2(1, 0),
        new Vector2(0, 1)
    };

    public void SetPoints(Vector2 first, Vector2 second, Vector2 third)
    {
        points = new[]
        {
            first,
            second,
            third
        };

        CreateMesh();
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        // 安全なタイミングで、関数を呼び出す
        EditorApplication.delayCall += DelayCreateMesh;
#endif
    }

    private void Awake()
    {
        CreateMesh();
    }

    private void CreateMesh()
    {
        if (points == null || points.Length != 3)
        {
            points = new[]
            {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(0, 1)
            };
        }

        // レイヤー自動設定
        gameObject.layer = LayerMask.NameToLayer("Floor");

        Mesh mesh = new Mesh();

        mesh.vertices = new Vector3[]
        {
            new Vector3(points[0].x, points[0].y, 0),
            new Vector3(points[1].x, points[1].y, 0),
            new Vector3(points[2].x, points[2].y, 0)
        };

        mesh.triangles = new int[]
        {
            0, 2, 1
        };

        mesh.RecalculateNormals();

        MeshFilter filter = GetComponent<MeshFilter>();
        filter.sharedMesh = mesh;

        MeshRenderer renderer = GetComponent<MeshRenderer>();

        if (renderer.sharedMaterial == null)
        {
            renderer.sharedMaterial =
                new Material(Shader.Find("Sprites/Default"));
        }

        PolygonCollider2D collider =
            GetComponent<PolygonCollider2D>();

        collider.points = new Vector2[]
        {
            points[0],
            points[1],
            points[2]
        };

        // 動かないように追加
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
    }

#if UNITY_EDITOR
    // メッシュの作成を遅延させる関数
    private void DelayCreateMesh()
    {
        if (this == null) return;

        CreateMesh();
    }
#endif
}
