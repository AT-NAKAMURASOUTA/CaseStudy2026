using UnityEngine;
using UnityEngine.InputSystem;

public sealed class AlphabetThrowController : MonoBehaviour
{
    // 現在の状態
    private enum HoldState
    {
        None,    // 何も持っていない
        Holding, // 持っているだけの状態
        Aiming,  // 投げる方向を決めている状態
    }

    [Header("拾える範囲")]
    [SerializeField]
    private float pickupRadius = 1.2f;

    [Header("持つ位置の前方向の距離")]
    [SerializeField]
    private float holdDistance = 0.9f;

    [Header("持つ位置の高さ")]
    [SerializeField]
    private float holdHeight = 1.0f;

    [Header("拾えるレイヤー")]
    [SerializeField]
    private LayerMask pickableLayers = ~0;

    [Header("拾えるオブジェクト名")]
    [SerializeField]
    private string pickableObjectName = "Alphabet";

    [Header("投げる速さ")]
    [SerializeField]
    private float throwSpeed = 12f;

    [Header("投げる時の回転の強さ")]
    [SerializeField]
    private float throwTorque = 8f;

    [Header("矢印の長さ")]
    [SerializeField]
    private float aimArrowLength = 1.8f;

    [Header("矢印の太さ")]
    [SerializeField]
    private float aimArrowWidth = 0.05f;

    [Header("矢印の先端の長さ")]
    [SerializeField]
    private float aimArrowHeadLength = 0.28f;

    [Header("矢印の先端の角度")]
    [SerializeField]
    private float aimArrowHeadAngle = 28f;

    [Header("矢印の前方向のずらし量")]
    [SerializeField]
    private float aimArrowForwardOffset = 0.35f;

    [Header("矢印の上方向のずらし量")]
    [SerializeField]
    private float aimArrowHeightOffset = 0.5f;

    [Header("矢印の色")]
    [SerializeField]
    private Color aimArrowColor = Color.yellow;

    // メインカメラ
    private Camera _mainCamera;

    // プレイヤー本体のRigidbody2D
    private Rigidbody2D _playerRigidbody2D;

    // プレイヤーについているCollider2Dの一覧
    private Collider2D[] _playerColliders;

    // 今持っているアルファベット
    private Rigidbody2D _heldLetter;

    // 今の持ち状態
    private HoldState _holdState;

    // プレイヤーが向いている方向
    // 右向きなら1、左向きなら-1
    private float _facingSign = 1f;

    // 矢印の棒部分
    private LineRenderer _aimShaft;

    // 矢印の先端部分
    private LineRenderer _aimHead;

    private void Awake()
    {
        // 最初に必要な参照を取っておく
        _mainCamera = Camera.main;
        _playerRigidbody2D = GetComponent<Rigidbody2D>();
        _playerColliders = GetComponents<Collider2D>();

        // 矢印を表示するためのLineRendererを作る
        CreateAimRenderers();
    }

    private void Update()
    {
        // カメラ参照が切れていたら取り直す
        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
        }

        // 毎フレーム必要な処理
        UpdateFacingDirection();
        HandleMouseInput();
        UpdateHeldLetterPosition();
        UpdateAimArrow();
    }

    private void HandleMouseInput()
    {
        if (Mouse.current == null)
        {
            return;
        }

        // 右クリックされた時の処理
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            HandleRightClick();
            return;
        }

        // 左クリックされていなければ何もしない
        if (!Mouse.current.leftButton.wasPressedThisFrame)
        {
            return;
        }

        // 今の状態によって左クリックの意味を変える
        switch (_holdState)
        {
            case HoldState.None:
                // 何も持っていない時は近くの文字を拾う
                TryPickLetter();
                break;

            case HoldState.Holding:
                // 持っている時は照準状態に入る
                StartAiming();
                break;

            case HoldState.Aiming:
                // 照準中なら投げる
                ThrowHeldLetter();
                break;
        }
    }

    private void HandleRightClick()
    {
        switch (_holdState)
        {
            case HoldState.Holding:
                // 持っているだけの時はその場で離す
                ReleaseHeldLetter();
                break;

            case HoldState.Aiming:
                // 照準中は構えをやめて持っている状態に戻す
                CancelAiming();
                break;
        }
    }

    private void TryPickLetter()
    {
        // プレイヤーの近くにある、拾える対象を全部調べる
        var hits = Physics2D.OverlapCircleAll(transform.position, pickupRadius, pickableLayers);

        Rigidbody2D nearestLetter = null;
        var nearestDistance = float.MaxValue;

        foreach (var hit in hits)
        {
            // 無効なものは無視
            if (hit == null)
            {
                continue;
            }

            // 指定した名前で始まるオブジェクトだけ拾う
            if (!hit.gameObject.name.StartsWith(pickableObjectName))
            {
                continue;
            }

            var rb = hit.attachedRigidbody;

            // Rigidbody2Dが付いていないものは対象外
            if (rb == null)
            {
                continue;
            }

            // プレイヤーの後ろ側にある文字は拾わない
            var toLetter = rb.worldCenterOfMass - (Vector2)transform.position;
            if (toLetter.x * _facingSign < 0f)
            {
                continue;
            }

            // 一番近いものを選ぶ
            var distance = Vector2.Distance(transform.position, rb.worldCenterOfMass);
            if (distance >= nearestDistance)
            {
                continue;
            }

            nearestDistance = distance;
            nearestLetter = rb;
        }

        // 何も見つからなければ終わり
        if (nearestLetter == null)
        {
            return;
        }

        // 見つけた文字を持つ
        _heldLetter = nearestLetter;

        // 持った瞬間に動きを止める
        _heldLetter.linearVelocity = Vector2.zero;
        _heldLetter.angularVelocity = 0f;

        // 手元に固定したいのでKinematicにする
        _heldLetter.bodyType = RigidbodyType2D.Kinematic;
        _holdState = HoldState.Holding;

        // 持った瞬間に前方の持つ位置へ移動する
        _heldLetter.transform.position = GetHoldPosition(holdDistance, holdHeight);
        _heldLetter.transform.rotation = Quaternion.identity;

        // プレイヤーとぶつからないようにする
        IgnoreHeldLetterCollisionWithPlayer();
    }

    private void StartAiming()
    {
        if (_heldLetter == null)
        {
            return;
        }

        // 照準状態に入る
        _holdState = HoldState.Aiming;
    }

    private void UpdateHeldLetterPosition()
    {
        if (_heldLetter == null)
        {
            return;
        }

        // 持っている間は常に決まった位置へ移動させる
        _heldLetter.transform.position = GetHoldPosition(holdDistance, holdHeight);

        // 見た目が傾かないように回転を戻す
        _heldLetter.transform.rotation = Quaternion.identity;
    }

    private void ThrowHeldLetter()
    {
        if (_heldLetter == null)
        {
            return;
        }

        var aimDirection = GetAimDirection();

        // 物理挙動を戻して投げられる状態にする
        _heldLetter.bodyType = RigidbodyType2D.Dynamic;
        RestoreHeldLetterCollisionWithPlayer();

        // 決めた方向へ速度を与えて飛ばす
        _heldLetter.linearVelocity = aimDirection * throwSpeed;

        // 少し回転も加えて見た目を自然にする
        _heldLetter.AddTorque(-Mathf.Sign(aimDirection.x) * throwTorque, ForceMode2D.Impulse);

        // 手放したので参照を消す
        _heldLetter = null;
        _holdState = HoldState.None;
    }

    private void CancelAiming()
    {
        if (_heldLetter == null)
        {
            return;
        }

        // 照準だけ解除して、持っている状態に戻す
        _holdState = HoldState.Holding;
    }

    private void ReleaseHeldLetter()
    {
        if (_heldLetter == null)
        {
            return;
        }

        // その場に落とすためDynamicに戻す
        _heldLetter.bodyType = RigidbodyType2D.Dynamic;
        RestoreHeldLetterCollisionWithPlayer();
        _heldLetter.linearVelocity = Vector2.zero;
        _heldLetter.angularVelocity = 0f;

        // 手放したので初期状態に戻す
        _heldLetter = null;
        _holdState = HoldState.None;
    }

    private Vector2 GetMouseWorldPosition()
    {
        // マウスの画面座標をワールド座標に変換する
        var screenPosition = Mouse.current.position.ReadValue();

        var worldPosition = _mainCamera.ScreenToWorldPoint(
            new Vector3(
                screenPosition.x,
                screenPosition.y,
                -_mainCamera.transform.position.z
            )
        );

        return new Vector2(worldPosition.x, worldPosition.y);
    }

    private Vector2 GetAimDirection()
    {
        // 持っている文字の位置を基準に狙う
        var basePosition =
            _heldLetter != null
                ? (Vector2)_heldLetter.transform.position
                : (Vector2)transform.position;

        var mousePosition = GetMouseWorldPosition();
        var direction = mousePosition - basePosition;
        var facingSign = _facingSign;

        // マウスがほぼ同じ位置なら、向いている方向をそのまま使う
        if (direction.sqrMagnitude < 0.0001f)
        {
            return new Vector2(facingSign, 0f);
        }

        // 後ろ側にマウスがあっても、向いている側に補正する
        if (direction.x * facingSign <= 0f)
        {
            direction.x = Mathf.Max(Mathf.Abs(direction.x), 0.1f) * facingSign;
        }

        return direction.normalized;
    }

    private Vector3 GetHoldPosition(float distance, float height)
    {
        // プレイヤーの前方、少し上の位置を返す
        return transform.position
            + (Vector3)(GetFacingDirection() * distance)
            + Vector3.up * height;
    }

    private Vector2 GetFacingDirection()
    {
        return new Vector2(_facingSign, 0f);
    }

    private void UpdateFacingDirection()
    {
        // ほぼ止まっている時は向きを変えない
        if (_playerRigidbody2D == null || Mathf.Abs(_playerRigidbody2D.linearVelocity.x) <= 0.01f)
        {
            return;
        }

        // 横移動の向きを現在の向きとして記録する
        _facingSign = Mathf.Sign(_playerRigidbody2D.linearVelocity.x);
    }

    private void IgnoreHeldLetterCollisionWithPlayer()
    {
        if (_heldLetter == null || _playerColliders == null || _playerColliders.Length == 0)
        {
            return;
        }

        var letterColliders = _heldLetter.GetComponents<Collider2D>();

        foreach (var playerCollider in _playerColliders)
        {
            if (playerCollider == null)
            {
                continue;
            }

            foreach (var letterCollider in letterColliders)
            {
                if (letterCollider == null)
                {
                    continue;
                }

                // 持っている文字がプレイヤーに当たらないようにする
                Physics2D.IgnoreCollision(playerCollider, letterCollider, true);
            }
        }
    }

    private void RestoreHeldLetterCollisionWithPlayer()
    {
        if (_heldLetter == null || _playerColliders == null || _playerColliders.Length == 0)
        {
            return;
        }

        var letterColliders = _heldLetter.GetComponents<Collider2D>();

        foreach (var playerCollider in _playerColliders)
        {
            if (playerCollider == null)
            {
                continue;
            }

            foreach (var letterCollider in letterColliders)
            {
                if (letterCollider == null)
                {
                    continue;
                }

                Physics2D.IgnoreCollision(playerCollider, letterCollider, false);
            }
        }
    }

    private void CreateAimRenderers()
    {
        // 矢印の棒部分と先端部分を別々に作る
        _aimShaft = CreateLineRenderer("AimArrowShaft", 2);
        _aimHead = CreateLineRenderer("AimArrowHead", 3);
    }

    private LineRenderer CreateLineRenderer(string objectName, int pointCount)
    {
        var lineObject = new GameObject(objectName);
        lineObject.transform.SetParent(transform, false);

        var lineRenderer = lineObject.AddComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.positionCount = pointCount;
        lineRenderer.startWidth = aimArrowWidth;
        lineRenderer.endWidth = aimArrowWidth;
        lineRenderer.startColor = aimArrowColor;
        lineRenderer.endColor = aimArrowColor;
        lineRenderer.sortingOrder = 20;

        // 最初は非表示にしておく
        lineRenderer.enabled = false;

        return lineRenderer;
    }

    private void UpdateAimArrow()
    {
        if (_aimShaft == null || _aimHead == null)
        {
            return;
        }

        // 文字を持っていない、または照準中でないなら矢印は表示しない
        if (_heldLetter == null || _holdState != HoldState.Aiming)
        {
            _aimShaft.enabled = false;
            _aimHead.enabled = false;
            return;
        }

        _aimShaft.enabled = true;
        _aimHead.enabled = true;

        var aimDirection = GetAimDirection();

        // 矢印の始点
        var arrowStart =
            _heldLetter.transform.position +
            (Vector3)(GetFacingDirection() * aimArrowForwardOffset) +
            Vector3.up * aimArrowHeightOffset;

        // 矢印の終点
        var arrowEnd = arrowStart + (Vector3)(aimDirection * aimArrowLength);

        // 棒部分を更新
        _aimShaft.SetPosition(0, arrowStart);
        _aimShaft.SetPosition(1, arrowEnd);

        // 先端の左右の点を求める
        var leftHead = GetArrowHeadPoint(arrowEnd, aimDirection, aimArrowHeadAngle);
        var rightHead = GetArrowHeadPoint(arrowEnd, aimDirection, -aimArrowHeadAngle);

        // 先端部分を更新
        _aimHead.SetPosition(0, leftHead);
        _aimHead.SetPosition(1, arrowEnd);
        _aimHead.SetPosition(2, rightHead);
    }

    private Vector3 GetArrowHeadPoint(Vector3 arrowEnd, Vector2 aimDirection, float angleOffset)
    {
        // 終点から少し斜め後ろに点を作って矢印の先端にする
        var rotatedDirection = Quaternion.Euler(0f, 0f, angleOffset) * -aimDirection;
        return arrowEnd + (Vector3)(rotatedDirection.normalized * aimArrowHeadLength);
    }

    private void OnDrawGizmosSelected()
    {
        // Sceneビューで拾える範囲が分かるように円を描く
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}
