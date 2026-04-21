using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class AlphabetCutFragment : MonoBehaviour
{
    [Header("地面接触として扱う法線Y")]
    [SerializeField]
    private float groundNormalYThreshold = 0.55f;

    [Header("切断直後に接地判定を無視する時間")]
    [SerializeField]
    private float landingIgnoreDuration = 0.08f;

    [Header("落下し続けた時の保険削除Y")]
    [SerializeField]
    private float destroyY = -12f;

    [Header("Landing Layers")]
    [SerializeField]
    private LayerMask landingLayers;

    private float m_SpawnTime;

    private void Awake()
    {
        m_SpawnTime = Time.time;

        // Landing用レイヤーが未設定ならFloorレイヤーを使う
        if (landingLayers.value == 0)
        {
            int floorLayer = LayerMask.NameToLayer("Floor");
            if (floorLayer >= 0)
            {
                landingLayers = 1 << floorLayer;
            }
        }
    }

    private void Update()
    {
        // 落ちすぎた破片は消す
        if (transform.position.y < destroyY)
        {
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 切れた直後はすぐ消えないように少しだけ接地判定を無視する
        if (Time.time < m_SpawnTime + landingIgnoreDuration)
        {
            return;
        }

        if (collision == null || collision.contactCount <= 0)
        {
            return;
        }

        // 指定したレイヤー以外との接触は無視
        int otherLayerMask = 1 << collision.gameObject.layer;
        if ((landingLayers.value & otherLayerMask) == 0)
        {
            return;
        }

        // 地面っぽい面に当たったら破片を消す
        for (int i = 0; i < collision.contactCount; i++)
        {
            if (collision.GetContact(i).normal.y >= groundNormalYThreshold)
            {
                Destroy(gameObject);
                return;
            }
        }
    }
}
