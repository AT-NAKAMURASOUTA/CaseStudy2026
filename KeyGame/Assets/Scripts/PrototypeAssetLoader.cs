using System.Collections.Generic;
using System.IO;
using UnityEngine;

// PrototypeAssets フォルダ配下の PNG を読み込み、実行時に使うスプライトへ変換する。
public static class PrototypeAssetLoader
{
    private const string PrototypeAssetsFolderName = "PrototypeAssets";

    // 同じ画像を繰り返し読み込まないよう、相対パスと PPU ごとにキャッシュする。
    private static readonly Dictionary<string, Sprite> SpriteCache = new();

    public static Sprite LoadSprite(string relativePath, float pixelsPerUnit)
    {
        var normalizedPath = relativePath.Replace('\\', '/');
        var cacheKey = $"{normalizedPath}:{pixelsPerUnit}";
        if (SpriteCache.TryGetValue(cacheKey, out var cachedSprite))
        {
            return cachedSprite;
        }

        var fullPath = Path.Combine(Application.dataPath, PrototypeAssetsFolderName, normalizedPath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(fullPath))
        {
            return null;
        }

        var bytes = File.ReadAllBytes(fullPath);
        var texture = new Texture2D(2, 2, TextureFormat.ARGB32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.LoadImage(bytes);

        var sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            pixelsPerUnit);

        SpriteCache[cacheKey] = sprite;
        return sprite;
    }
}
