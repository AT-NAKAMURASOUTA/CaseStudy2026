using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

// A-Z の入力に応じて、対応するアルファベットをプレイヤー前方へ生成する。
[ExecuteAlways]
public sealed class PrototypeLetterSpawner : MonoBehaviour
{
    // プレイヤー中心から見た前方への生成距離。
    [SerializeField] private float spawnDistance = 1.2f;

    // 地面へのめり込みを避けるための高さオフセット。
    [SerializeField] private float spawnHeightOffset = 0.2f;

    // 生成した文字スプライトの初期倍率。
    [SerializeField] private float spawnedLetterScale = 0.8f;

    // スプライト生成時の Pixels Per Unit。値を大きくすると同じ画像でも小さく表示される。
    [SerializeField] private float spawnedLetterPixelsPerUnit = 192f;

    // 直近の移動方向。停止中も前回向いていた側に文字を出すため保持する。
    private float _facingDirection = 1f;

    // 一度読み込んだスプライトはキャッシュし、連続生成時の再読込を避ける。
    private static readonly Dictionary<string, Sprite> LetterSpriteCache = new();

    // New Input System 用のキーと文字の対応表。
    private static readonly (Key key, char letter)[] LetterKeyPairs =
    {
        (Key.A, 'A'),
        (Key.B, 'B'),
        (Key.C, 'C'),
        (Key.D, 'D'),
        (Key.E, 'E'),
        (Key.F, 'F'),
        (Key.G, 'G'),
        (Key.H, 'H'),
        (Key.I, 'I'),
        (Key.J, 'J'),
        (Key.K, 'K'),
        (Key.L, 'L'),
        (Key.M, 'M'),
        (Key.N, 'N'),
        (Key.O, 'O'),
        (Key.P, 'P'),
        (Key.Q, 'Q'),
        (Key.R, 'R'),
        (Key.S, 'S'),
        (Key.T, 'T'),
        (Key.U, 'U'),
        (Key.V, 'V'),
        (Key.W, 'W'),
        (Key.X, 'X'),
        (Key.Y, 'Y'),
        (Key.Z, 'Z'),
    };

    private void Update()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        // 左右入力を見て向きを更新し、文字の生成方向を安定させる。
        var horizontal = ReadHorizontalInput();
        if (horizontal != 0f)
        {
            _facingDirection = Mathf.Sign(horizontal);
        }

        if (TryReadLetterKey(out var letter))
        {
            SpawnLetter(letter);
        }
    }

    private void SpawnLetter(char letter)
    {
        var sprite = LoadLetterSprite(letter, spawnedLetterPixelsPerUnit);
        if (sprite == null)
        {
            Debug.LogWarning($"Letter sprite not found for {letter}");
            return;
        }

        // 文字オブジェクトを新規生成し、位置と大きさを初期化する。
        var letterObject = new GameObject($"Letter_{letter}");
        letterObject.transform.position =
            transform.position + new Vector3(_facingDirection * spawnDistance, spawnHeightOffset, 0f);
        letterObject.transform.localScale = Vector3.one * spawnedLetterScale;

        // 見た目を設定する。
        var renderer = letterObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 4;

        // 落下や転がりを確認できるよう、簡易的な物理挙動を与える。
        var rigidbody2D = letterObject.AddComponent<Rigidbody2D>();
        rigidbody2D.gravityScale = 1.8f;

        // 文字形状に沿った当たり判定を持たせる。
        letterObject.AddComponent<PolygonCollider2D>();
    }

    private static float ReadHorizontalInput()
    {
        // New Input System が使える場合はそちらを優先する。
        if (Keyboard.current != null)
        {
            var leftPressed = Keyboard.current.leftArrowKey.isPressed;
            var rightPressed = Keyboard.current.rightArrowKey.isPressed;
            return (rightPressed ? 1f : 0f) - (leftPressed ? 1f : 0f);
        }

        // フォールバックとして旧 Input Manager にも対応しておく。
        var horizontal = 0f;

        if (Input.GetKey(KeyCode.LeftArrow))
        {
            horizontal -= 1f;
        }

        if (Input.GetKey(KeyCode.RightArrow))
        {
            horizontal += 1f;
        }

        return horizontal;
    }

    private static bool TryReadLetterKey(out char letter)
    {
        letter = default;

        // New Input System が使える場合は対応表から入力を判定する。
        if (Keyboard.current != null)
        {
            foreach (var (key, value) in LetterKeyPairs)
            {
                if (Keyboard.current[key].wasPressedThisFrame)
                {
                    letter = value;
                    return true;
                }
            }

            return false;
        }

        // フォールバックとして旧 Input Manager でも同じ入力を受ける。
        for (var value = 'A'; value <= 'Z'; value++)
        {
            if (Input.GetKeyDown(value.ToString().ToLowerInvariant()))
            {
                letter = value;
                return true;
            }
        }

        return false;
    }

    private static Sprite LoadLetterSprite(char letter, float pixelsPerUnit)
    {
        var cacheKey = GetCacheKey(letter, pixelsPerUnit);
        if (LetterSpriteCache.TryGetValue(cacheKey, out var cachedSprite))
        {
            return cachedSprite;
        }

        // PrototypeAssets/Alphabet 内の PNG を読み込み、文字スプライトを生成する。
        var path = Path.Combine(Application.dataPath, "PrototypeAssets", "Alphabet", $"{letter}.png");
        if (!File.Exists(path))
        {
            return null;
        }

        var bytes = File.ReadAllBytes(path);
        var texture = new Texture2D(2, 2, TextureFormat.ARGB32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.LoadImage(bytes);

        var sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            pixelsPerUnit);

        LetterSpriteCache[cacheKey] = sprite;
        return sprite;
    }

    private static string GetCacheKey(char letter, float pixelsPerUnit)
    {
        // PPU を変えたときに古いサイズのスプライトを再利用しないよう、キーに含める。
        return $"{letter}:{pixelsPerUnit}";
    }
}
