using UnityEngine;

[ExecuteInEditMode]
public class ScaleToTiledSize : MonoBehaviour
{
    [Header("X方向は固定")]
    [SerializeField]
    private float xScale = 0.43f;

    private SpriteRenderer sr;
    private BoxCollider2D col;

    void Update()
    {
        sr ??= GetComponent<SpriteRenderer>();
        col ??= GetComponent<BoxCollider2D>();

        // DrawModeがTiledでないなら何もしない
        if (sr == null || sr.drawMode != SpriteDrawMode.Tiled) return;

        // Scaleが1以外（＝エディターでいじられた）なら
        if (transform.localScale != Vector3.one)
        {
            // Scaleの値をSizeに加算・乗算して反映
            Vector2 newSize = sr.size;
            newSize.x = xScale;// transform.localScale.x;
            newSize.y *= transform.localScale.y;
            sr.size = newSize;

            // コライダーも同様にサイズを変更
            if (col != null)
            {
                //Vector2 newColSize = col.size;
                //newColSize.x = xScale;// transform.localScale.x;
                //newColSize.y *= transform.localScale.y;
                col.size = newSize;
            }

            // Scaleそのものは1に戻す（これで円が伸びなくなる）
            transform.localScale = Vector3.one;
        }
    }
}
