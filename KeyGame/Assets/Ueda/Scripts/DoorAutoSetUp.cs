using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class DoorAutoSetUp : MonoBehaviour
{
    void Awake()
    {
        // 1. エディター上で設定された元のスケールを記録
        Vector3 originalScale = transform.localScale;

        // スケールが(1,1,1)なら処理する必要がないのでスキップ
        if (originalScale == Vector3.one) return;

        // 2. BoxCollider2D のサイズを現在のスケールに合わせて調整
        BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider != null)
        {
            // コライダーのサイズに親のスケールを掛け合わせる
            boxCollider.size = new Vector2(
                boxCollider.size.x * originalScale.x,
                boxCollider.size.y * originalScale.y
            );
            // オフセット（中心位置）もスケールに合わせて調整
            boxCollider.offset = new Vector2(
                boxCollider.offset.x * originalScale.x,
                boxCollider.offset.y * originalScale.y
            );
        }

        // 3. 自身についている SpriteRenderer を取得
        SpriteRenderer myRenderer = GetComponent<SpriteRenderer>();
        if (myRenderer != null)
        {
            // 新しい子オブジェクトを作成
            GameObject spriteChild = new GameObject("VisualSprite");
            spriteChild.transform.SetParent(transform);

            // 子オブジェクトの位置・回転・スケールを初期化（親の元のスケールを引き継ぐ）
            spriteChild.transform.localPosition = Vector3.zero;
            spriteChild.transform.localRotation = Quaternion.identity;
            spriteChild.transform.localScale = originalScale;

            // 子オブジェクトに SpriteRenderer を引っ越し
            SpriteRenderer childRenderer = spriteChild.AddComponent<SpriteRenderer>();
            childRenderer.sprite = myRenderer.sprite;
            childRenderer.color = myRenderer.color;
            childRenderer.material = myRenderer.material;
            childRenderer.sortingLayerID = myRenderer.sortingLayerID;
            childRenderer.sortingOrder = myRenderer.sortingOrder;
            childRenderer.flipX = myRenderer.flipX;
            childRenderer.flipY = myRenderer.flipY;

            // 元の（親の）SpriteRenderer は不要になったので削除
            Destroy(myRenderer);
        }

        // 4. 最後に親（このオブジェクト）のスケールを(1, 1, 1)にリセット
        // これにより、上に乗ったプレイヤーが変形するのを防ぎます
        transform.localScale = Vector3.one;
    }
}
