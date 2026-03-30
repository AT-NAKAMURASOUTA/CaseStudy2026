using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public sealed class AlphabetWallReaction : MonoBehaviour
{
    // 壁に当たったときの反応を決める
    private enum AlphabetReactionType
    {
        StraightOnly, // まっすぐ落ちる
        CurvedOnly,   // 必ず横に跳ねる
        Mixed,        // 当たった位置によって変わる
    }

    [Header("Wall reaction settings")]
    [SerializeField] private string wallObjectNamePrefix = "Wall";
    [SerializeField] private float minimumWallHitHorizontalSpeed = 0.25f;
    [SerializeField] private float minHorizontalBounceSpeed = 3.8f;
    [SerializeField] private float bounceSpeedMultiplier = 1.05f;
    [SerializeField] private float curvedDownwardSpeed = 1.5f;
    [SerializeField] private float maxCurvedUpwardSpeed = 0.6f;
    [SerializeField] private float reactionLockDuration = 0.12f;
    [SerializeField] private float temporaryWallIgnoreDuration = 0.12f;
    [SerializeField] private float wallSeparationDistance = 0.05f;
    [SerializeField] private float supportNormalYThreshold = 0.55f;

    // 物理挙動の制御に使う
    private Rigidbody2D _rigidbody2D;

    // スプライトの大きさや当たった位置の判定に使う
    private SpriteRenderer _spriteRenderer;

    // 自分の当たり判定
    private Collider2D _collider2D;

    // 今の文字
    private char _alphabetCharacter = '?';

    // その文字がどの反応タイプか
    private AlphabetReactionType _reactionType = AlphabetReactionType.StraightOnly;

    // 元の重力値を保存しておく
    private float _defaultGravityScale;

    // 反応中の多重実行防止
    private bool _isReacting;

    private void Awake()
    {
        // 必要なコンポーネントを取得
        _rigidbody2D = GetComponent<Rigidbody2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _collider2D = GetComponent<Collider2D>();

        // 初期の重力を保存
        _defaultGravityScale = _rigidbody2D.gravityScale;
    }

    public void SetAlphabetCharacter(char alphabetCharacter)
    {
        // 文字を大文字にそろえて保存
        _alphabetCharacter = char.ToUpperInvariant(alphabetCharacter);

        // 文字ごとの反応タイプを決める
        _reactionType = GetReactionType(_alphabetCharacter);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // すでに反応中なら何もしない
        if (_isReacting)
        {
            return;
        }

        // 当たった相手が壁かどうかを確認
        Collider2D wallCollider = GetWallCollider(collision);
        if (wallCollider == null || collision.contactCount == 0)
        {
            return;
        }

        // ほとんど動いていない状態なら反応させない
        if (_rigidbody2D.linearVelocity.sqrMagnitude < minimumWallHitHorizontalSpeed * minimumWallHitHorizontalSpeed)
        {
            return;
        }

        // 床などに支えられている状態なら壁反応させない
        if (IsSupportedByNonWallSurface())
        {
            return;
        }

        // 接触点を取得
        ContactPoint2D contact = collision.GetContact(0);

        // 文字のどの位置に当たったかを -1～1 の範囲で求める
        if (!TryGetNormalizedLocalContactPoint(contact.point, out Vector2 normalizedLocalPoint))
        {
            return;
        }

        // 当たった位置に応じて反応を分ける
        if (ShouldBounce(normalizedLocalPoint))
        {
            StartCoroutine(ApplyCurvedReaction(wallCollider));
        }
        else
        {
            StartCoroutine(ApplyStraightReaction(wallCollider));
        }
    }

    private Collider2D GetWallCollider(Collision2D collision)
    {
        // 相手側のコライダーが壁ならそれを返す
        if (collision.collider != null && collision.collider.gameObject != gameObject &&
            collision.collider.gameObject.name.StartsWith(wallObjectNamePrefix))
        {
            return collision.collider;
        }

        // もう片方のコライダー側が壁ならそちらを返す
        if (collision.otherCollider != null && collision.otherCollider.gameObject != gameObject &&
            collision.otherCollider.gameObject.name.StartsWith(wallObjectNamePrefix))
        {
            return collision.otherCollider;
        }

        return null;
    }

    private bool TryGetNormalizedLocalContactPoint(Vector2 worldContactPoint, out Vector2 normalizedLocalPoint)
    {
        normalizedLocalPoint = Vector2.zero;

        // スプライトが取れないと文字内の位置判定ができない
        if (_spriteRenderer == null || _spriteRenderer.sprite == null)
        {
            return false;
        }

        // 接触点をローカル座標に変換
        Vector2 localContactPoint = transform.InverseTransformPoint(worldContactPoint);

        // スプライトの半分サイズ
        Vector2 extents = _spriteRenderer.sprite.bounds.extents;

        // サイズがほぼ0なら計算しない
        if (extents.x <= 0.0001f || extents.y <= 0.0001f)
        {
            return false;
        }

        // 文字の中心を0として、左右上下を -1～1 に正規化する
        normalizedLocalPoint = new Vector2(
            Mathf.Clamp(localContactPoint.x / extents.x, -1f, 1f),
            Mathf.Clamp(localContactPoint.y / extents.y, -1f, 1f)
        );

        return true;
    }

    private bool ShouldBounce(Vector2 point)
    {
        // 文字のタイプごとに、跳ねるかどうかを決める
        switch (_reactionType)
        {
            case AlphabetReactionType.CurvedOnly:
                return true;

            case AlphabetReactionType.Mixed:
                return IsCurvedImpact(point);

            default:
                return false;
        }
    }

    private static AlphabetReactionType GetReactionType(char alphabetCharacter)
    {
        // 文字の形に合わせて反応タイプを決める
        switch (alphabetCharacter)
        {
            case 'B':
            case 'D':
            case 'J':
            case 'P':
            case 'R':
            case 'U':
                return AlphabetReactionType.Mixed;

            case 'C':
            case 'G':
            case 'O':
            case 'Q':
            case 'S':
                return AlphabetReactionType.CurvedOnly;

            default:
                return AlphabetReactionType.StraightOnly;
        }
    }

    private bool IsCurvedImpact(Vector2 point)
    {
        float x = point.x;
        float y = point.y;

        // 文字ごとに「どの位置に当たったら曲面として扱うか」を決める
        switch (_alphabetCharacter)
        {
            case 'B':
                return x > 0f;

            case 'C':
                return true;

            case 'D':
                return x > -0.15f;

            case 'G':
                return true;

            case 'J':
                return y < -0.15f;

            case 'O':
                return true;

            case 'P':
                return x > 0f && y > -0.1f;

            case 'Q':
                return true;

            case 'R':
                return x > 0f && y > -0.15f;

            case 'S':
                return true;

            case 'U':
                return y < -0.2f;

            default:
                return _reactionType == AlphabetReactionType.CurvedOnly;
        }
    }

    private System.Collections.IEnumerator ApplyCurvedReaction(Collider2D wallCollider)
    {
        _isReacting = true;

        // 重力は元に戻しておく
        _rigidbody2D.gravityScale = _defaultGravityScale;

        // しばらく壁との衝突を無視して、壁から少し離す
        IgnoreWallCollision(wallCollider, true);
        PushAwayFromWall(wallCollider);

        // 壁の中心から見て、どちら向きに跳ね返すかを決める
        float horizontalDirection = Mathf.Sign(transform.position.x - wallCollider.bounds.center.x);

        // もし位置だけで決められないなら、今の移動方向を使う
        if (Mathf.Approximately(horizontalDirection, 0f))
        {
            horizontalDirection = Mathf.Sign(_rigidbody2D.linearVelocity.x);

            // それも0なら、とりあえず右向き
            if (Mathf.Approximately(horizontalDirection, 0f))
            {
                horizontalDirection = 1f;
            }
        }

        Vector2 currentVelocity = _rigidbody2D.linearVelocity;

        // 横方向はある程度しっかり跳ねるようにする
        float horizontalSpeed = Mathf.Max(Mathf.Abs(currentVelocity.x) * bounceSpeedMultiplier, minHorizontalBounceSpeed);

        // 上向きに飛びすぎないように制限
        float verticalSpeed = Mathf.Min(currentVelocity.y, maxCurvedUpwardSpeed);

        // 最低でも少し下向きには落とす
        verticalSpeed = Mathf.Min(verticalSpeed, -curvedDownwardSpeed);

        // 跳ね返り速度を適用
        _rigidbody2D.linearVelocity = new Vector2(horizontalDirection * horizontalSpeed, verticalSpeed);

        // 少しの間だけ反応を固定
        yield return new WaitForSeconds(reactionLockDuration);

        // 壁無視の残り時間があればその分待つ
        float remainingIgnoreDuration = Mathf.Max(0f, temporaryWallIgnoreDuration - reactionLockDuration);
        if (remainingIgnoreDuration > 0f)
        {
            yield return new WaitForSeconds(remainingIgnoreDuration);
        }

        // 壁との当たり判定を戻す
        IgnoreWallCollision(wallCollider, false);
        _isReacting = false;
    }

    private System.Collections.IEnumerator ApplyStraightReaction(Collider2D wallCollider)
    {
        _isReacting = true;

        // 重力は元に戻しておく
        _rigidbody2D.gravityScale = _defaultGravityScale;

        // めり込み防止で壁から少し離す
        PushAwayFromWall(wallCollider);

        // 縦方向は下向きだけ残して、横移動は止める
        float downwardSpeed = Mathf.Min(_rigidbody2D.linearVelocity.y, 0f);
        _rigidbody2D.linearVelocity = new Vector2(0f, downwardSpeed);

        // 回転も止める
        _rigidbody2D.angularVelocity = 0f;

        // 一時的に壁との衝突を無視する
        IgnoreWallCollision(wallCollider, true);

        yield return new WaitForSeconds(reactionLockDuration);

        // 壁無視の残り時間があればその分待つ
        float remainingIgnoreDuration = Mathf.Max(0f, temporaryWallIgnoreDuration - reactionLockDuration);
        if (remainingIgnoreDuration > 0f)
        {
            yield return new WaitForSeconds(remainingIgnoreDuration);
        }

        // 壁との当たり判定を戻す
        IgnoreWallCollision(wallCollider, false);

        // 念のため重力も元に戻しておく
        _rigidbody2D.gravityScale = _defaultGravityScale;
        _isReacting = false;
    }

    private bool IsSupportedByNonWallSurface()
    {
        ContactPoint2D[] contacts = new ContactPoint2D[16];
        int contactCount = _rigidbody2D.GetContacts(contacts);

        for (int i = 0; i < contactCount; i++)
        {
            ContactPoint2D contact = contacts[i];
            Collider2D otherCollider = contact.collider;

            if (otherCollider == null || otherCollider.gameObject == gameObject)
            {
                continue;
            }

            // 壁はここでは無視する
            if (otherCollider.gameObject.name.StartsWith(wallObjectNamePrefix))
            {
                continue;
            }

            // 上向きの法線が十分あれば、床などに支えられているとみなす
            if (contact.normal.y >= supportNormalYThreshold)
            {
                return true;
            }
        }

        return false;
    }

    private void IgnoreWallCollision(Collider2D wallCollider, bool ignore)
    {
        if (wallCollider == null)
        {
            return;
        }

        // 自分についている全コライダーに対して壁との当たり判定を切り替える
        Collider2D[] selfColliders = GetComponents<Collider2D>();
        foreach (Collider2D selfCollider in selfColliders)
        {
            if (selfCollider == null)
            {
                continue;
            }

            Physics2D.IgnoreCollision(selfCollider, wallCollider, ignore);
        }
    }

    private void PushAwayFromWall(Collider2D wallCollider)
    {
        if (_collider2D == null || wallCollider == null)
        {
            return;
        }

        Bounds selfBounds = _collider2D.bounds;
        Bounds wallBounds = wallCollider.bounds;
        Vector3 position = transform.position;

        // 壁の左右どちらにいるかで、少し外側へ押し出す
        if (selfBounds.center.x <= wallBounds.center.x)
        {
            float targetX = wallBounds.min.x - selfBounds.extents.x - wallSeparationDistance;
            position.x += targetX - selfBounds.center.x;
        }
        else
        {
            float targetX = wallBounds.max.x + selfBounds.extents.x + wallSeparationDistance;
            position.x += targetX - selfBounds.center.x;
        }

        transform.position = position;
    }
}
