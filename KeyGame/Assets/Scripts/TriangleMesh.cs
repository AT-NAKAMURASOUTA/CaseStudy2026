using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(PolygonCollider2D))]
public class TriangleMesh : MonoBehaviour
{
    private void OnValidate()
    {
        CreateMesh();
    }

    private void Awake()
    {
        CreateMesh();
    }

    private void CreateMesh()
    {
        // レイヤー自動設定
        gameObject.layer = LayerMask.NameToLayer("Floor");

        Mesh mesh = new Mesh();

        mesh.vertices = new Vector3[]
        {
            new Vector3(0, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(0, 1, 0)
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
            new Vector2(0,0),
            new Vector2(1,0),
            new Vector2(0,1)
        };
    }
}
