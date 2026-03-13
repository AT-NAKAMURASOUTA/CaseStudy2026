using UnityEngine;

[ExecuteAlways]
public sealed class PrototypeBootstrap : MonoBehaviour
{
    private const float CameraHalfHeight = 5f;
    private const float TargetAspectRatio = 16f / 9f;

    private void OnEnable()
    {
        EnsureSceneObjects();
    }

    private void OnValidate()
    {
        EnsureSceneObjects();
    }

    private void EnsureSceneObjects()
    {
        if (Camera.main == null)
        {
            return;
        }

        EnsureCamera();
        RemoveIfExists("PrototypeGround");
        RemoveIfExists("PrototypePlayer");
        CreateGroundIfMissing();
        CreatePlayerIfMissing();
    }

    private static void EnsureCamera()
    {
        var camera = Camera.main;
        camera.orthographic = true;
        camera.orthographicSize = CameraHalfHeight;
        camera.transform.position = new Vector3(0f, 0f, -10f);
    }

    private static void CreateGroundIfMissing()
    {
        var existingGround = GameObject.Find("Ground");
        if (existingGround != null)
        {
            ApplyGroundVisual(existingGround);
            return;
        }

        var halfWidth = GetCameraHalfWidth();
        var visibleWidth = halfWidth * 2f;
        var groundHeight = 0.7f;
        var horizontalMargin = visibleWidth * 0.18f;
        var groundWidth = visibleWidth - (horizontalMargin * 2f);
        var bottomMargin = 0.35f;
        var groundY = -CameraHalfHeight + bottomMargin + (groundHeight * 0.5f);

        var ground = new GameObject("Ground");
        ground.transform.position = new Vector3(0f, groundY, 0f);
        ground.transform.localScale = new Vector3(groundWidth, groundHeight, 1f);

        ground.AddComponent<BoxCollider2D>();
        ApplyGroundVisual(ground);
    }

    private static void CreatePlayerIfMissing()
    {
        var existingPlayer = GameObject.Find("Player");
        if (existingPlayer != null)
        {
            ApplyPlayerVisual(existingPlayer);
            EnsurePlayerComponents(existingPlayer);
            return;
        }

        var groundHeight = 0.7f;
        var bottomMargin = 0.35f;
        var groundTopY = -CameraHalfHeight + bottomMargin + groundHeight;
        var playerHeight = 1.2f;
        var playerWidth = 0.8f;
        var playerY = groundTopY + (playerHeight * 0.5f);

        var player = new GameObject("Player");
        player.transform.position = new Vector3(0f, playerY, 0f);
        player.transform.localScale = new Vector3(playerWidth, playerHeight, 1f);

        EnsurePlayerComponents(player);
        ApplyPlayerVisual(player);
    }

    private static void EnsurePlayerComponents(GameObject player)
    {
        GetOrAddComponent<BoxCollider2D>(player);

        var rigidbody2D = GetOrAddComponent<Rigidbody2D>(player);
        rigidbody2D.gravityScale = 3f;
        rigidbody2D.freezeRotation = true;

        GetOrAddComponent<PrototypePlayerController>(player);
    }

    private static void ApplyGroundVisual(GameObject ground)
    {
        var renderer = GetOrAddComponent<SpriteRenderer>(ground);
        renderer.sprite = GetSquareSprite();
        renderer.color = new Color(0.19f, 0.24f, 0.31f);
        renderer.sortingOrder = 0;
    }

    private static void ApplyPlayerVisual(GameObject player)
    {
        var renderer = GetOrAddComponent<SpriteRenderer>(player);
        renderer.sprite = GetSquareSprite();
        renderer.color = new Color(0.16f, 0.67f, 0.89f);
        renderer.sortingOrder = 5;
    }

    private static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
    {
        var component = gameObject.GetComponent<T>();
        if (component == null)
        {
            component = gameObject.AddComponent<T>();
        }

        return component;
    }

    private static void RemoveIfExists(string objectName)
    {
        var existing = GameObject.Find(objectName);
        if (existing == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Object.Destroy(existing);
        }
        else
        {
            Object.DestroyImmediate(existing);
        }
    }

    private static float GetCameraHalfWidth()
    {
        var camera = Camera.main;
        if (camera != null)
        {
            return camera.orthographicSize * camera.aspect;
        }

        return CameraHalfHeight * TargetAspectRatio;
    }

    private static Sprite GetSquareSprite()
    {
        return Resources.Load<Sprite>("PrototypeSquare");
    }
}
