using UnityEngine;

public class RetinueFariy : MonoBehaviour
{
    [Header("付きまとう対象")]
    [SerializeField]
    private Transform target;

    [Header("距離感")]
    [SerializeField]
    private Vector2 distance = new Vector2(-1.0f,0.0f);

    [Header("追尾遅延速度")]
    [SerializeField]
    private float followSpeed = 0.9f;

    [Header("ふよふよ速度")]
    [SerializeField]
    private float floatSpeed = 1.0f;

    [Header("ふよふよ幅")]
    [SerializeField]
    private float floatWidth = 0.5f;

    private float floatTimer = 0.0f;

    private SpriteRenderer targetSprite;
    private SpriteRenderer mySprite;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        targetSprite = target.GetComponent<SpriteRenderer>();
        mySprite = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (target == null) return;

        Vector2 dist = distance;
        if (targetSprite != null)
        {
            dist.x = targetSprite.flipX ? -dist.x : dist.x;
        }

        // ふよふよの計算
        floatTimer += Time.fixedDeltaTime * floatSpeed;
        float floatOffset = Mathf.Sin(floatTimer) * floatWidth;

        // 目標位置の計算
        Vector2 targetPosition = target.position + new Vector3(dist.x, dist.y + floatOffset, 0);

        Vector3 sca = transform.localScale;
        sca.x = Mathf.Abs(sca.x);
        sca.x = targetPosition.x - transform.position.x < 0.0f ? -sca.x : sca.x;
        transform.localScale = sca;

        this.transform.position = Vector2.Lerp(transform.position, targetPosition, followSpeed);
    }
}
