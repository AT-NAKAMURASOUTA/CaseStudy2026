using UnityEngine;
using UnityEngine.InputSystem;

public sealed class AlphabetThrowController : MonoBehaviour
{
    // 今の持ち状態
    private enum HoldState
    {
        None,    // 何も持っていない
        Holding, // 文字を持っているだけの状態
        Aiming,  // 投げる方向を決めている状態
    }

    [Header("拾える距離")]
    [SerializeField]
    private float pickupRadius = 1.2f;

    [Header("持った位置の前後距離")]
    [SerializeField]
    private float holdDistance = 0.9f;

    [Header("持った位置の高さ")]
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

    [Header("投げたときの回転")]
    [SerializeField]
    private float throwTorque = 8f;

    [Header("投げ角度の下限")]
    [SerializeField]
    private float minAimAngle = 30f;

    [Header("投げ角度の上限")]
    [SerializeField]
    private float maxAimAngle = 90f;

    [Header("投げ角度の往復速度")]
    [SerializeField]
    private float aimSwingSpeed = 120f;

    [Header("矢印の長さ")]
    [SerializeField]
    private float aimArrowLength = 1.8f;

    [Header("矢印の太さ")]
    [SerializeField]
    private float aimArrowWidth = 0.05f;

    [Header("矢印の先端の長さ")]
    [SerializeField]
    private float aimArrowHeadLength = 0.28f;

    [Header("矢印の先端の開き角度")]
    [SerializeField]
    private float aimArrowHeadAngle = 28f;

    [Header("矢印の前方向のずれ")]
    [SerializeField]
    private float aimArrowForwardOffset = 0.35f;

    [Header("矢印の上方向のずれ")]
    [SerializeField]
    private float aimArrowHeightOffset = 0.5f;

    [Header("矢印の色")]
    [SerializeField]
    private Color aimArrowColor = Color.yellow;

    // メインカメラ
    private Camera _mainCamera;

    // プレイヤー本体のRigidbody2D
    private Rigidbody2D _playerRigidbody2D;

    // プレイヤーについているCollider2D一覧
    private Collider2D[] _playerColliders;

    // 今持っているアルファベット
    private Rigidbody2D _heldLetter;

    // 現在の持ち状態
    private HoldState _holdState;

    // プレイヤーが向いている方向
    // 右向きなら1、左向きなら-1
    private float _facingSign = 1f;

    // 矢印の線部分
    private LineRenderer _aimShaft;

    // 矢印の先端部分
    private LineRenderer _aimHead;

    // 現在の投げ角度
    private float _currentAimAngle = 30f;

    // 投げ角度の増減方向
    // 1 なら増加、-1 なら減少
    private float _aimAngleDirection = 1f;

    private void Awake()
    {
        // 最初に必要な参照を取っておく
        _mainCamera = Camera.main;
        _playerRigidbody2D = GetComponent<Rigidbody2D>();
        _playerColliders = GetComponents<Collider2D>();

        // 狙い表示用のLineRendererを作る
        CreateAimRenderers();
    }

    private void Update()
    {
        // カメラ参照が切れていたら取り直す
        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
        }

        if (_heldLetter == null && _holdState != HoldState.None)
        {
            _holdState = HoldState.None;
        }

        // 毎フレーム必要な処理
        UpdateFacingDirection();
        HandleMouseInput();
        HandleKeyboardInput();

        // 狙い中だけ角度を往復させる
        if (_holdState == HoldState.Aiming)
        {
            UpdateAimOscillation();
        }

        UpdateHeldLetterPosition();
        UpdateAimArrow();
    }

    private void HandleMouseInput()
    {
        if (Mouse.current == null)
        {
            return;
        }

        // 右クリック時の処理
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

        HandlePrimaryAction();
    }

    private void HandleKeyboardInput()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        // スペースが押されていなければ何もしない
        if (!Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            return;
        }

        HandlePrimaryAction();
    }

    private void HandlePrimaryAction()
    {
        // 今の状態によって、左クリックやスペースの処理を変える
        switch (_holdState)
        {
            case HoldState.None:
                // 何も持っていないときは近くの文字を拾う
                TryPickLetter();
                break;

            case HoldState.Holding:
                // 持っている状態なら狙いモードに入る
                StartAiming();
                break;

            case HoldState.Aiming:
                // 狙い中なら投げる
                ThrowHeldLetter();
                break;
        }
    }

    private void HandleRightClick()
    {
        switch (_holdState)
        {
            case HoldState.Holding:
                // 持っているだけの状態ならその場で離す
                ReleaseHeldLetter();
                break;

            case HoldState.Aiming:
                // 狙い中は投げるのをやめて持ち状態に戻す
                CancelAiming();
                break;
        }
    }

    private void TryPickLetter()
    {
        // プレイヤーの近くにある、拾える対象を調べる
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

            // Rigidbody2Dが無いものは対象外
            if (rb == null)
            {
                continue;
            }

            // プレイヤーの後ろ側にあるものは拾わない
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

        // 見つからなければ終わり
        if (nearestLetter == null)
        {
            return;
        }

        // 見つけた文字を持つ
        _heldLetter = nearestLetter;

        // 持つ瞬間に動きを止める
        _heldLetter.linearVelocity = Vector2.zero;
        _heldLetter.angularVelocity = 0f;

        // 手持ち中はKinematicにする
        _heldLetter.bodyType = RigidbodyType2D.Kinematic;
        _holdState = HoldState.Holding;

        // 持つ位置へ移動
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

        // 狙い開始時は下限角度からスタート
        _currentAimAngle = minAimAngle;
        _aimAngleDirection = 1f;

        // 狙い状態に入る
        _holdState = HoldState.Aiming;
    }

    private void UpdateAimOscillation()
    {
        // 設定した速度で投げ角度を往復させる
        _currentAimAngle += aimSwingSpeed * _aimAngleDirection * Time.deltaTime;

        // 上限に達したら反転
        if (_currentAimAngle >= maxAimAngle)
        {
            _currentAimAngle = maxAimAngle;
            _aimAngleDirection = -1f;
        }
        // 下限に達したら反転
        else if (_currentAimAngle <= minAimAngle)
        {
            _currentAimAngle = minAimAngle;
            _aimAngleDirection = 1f;
        }
    }

    private void UpdateHeldLetterPosition()
    {
        if (_heldLetter == null)
        {
            return;
        }

        // 持っている間は常に指定位置へ移動させる
        _heldLetter.transform.position = GetHoldPosition(holdDistance, holdHeight);

        // 傾かないように回転は固定
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

        // 狙った方向へ速度を与える
        _heldLetter.linearVelocity = aimDirection * throwSpeed;

        // 少し回転も付けて投げた感じを出す
        _heldLetter.AddTorque(-Mathf.Sign(aimDirection.x) * throwTorque, ForceMode2D.Impulse);

        // 投げ終わったので状態を戻す
        _heldLetter = null;
        _holdState = HoldState.None;
    }

    private void CancelAiming()
    {
        if (_heldLetter == null)
        {
            return;
        }

        // 狙いをやめて、持っている状態に戻す
        _holdState = HoldState.Holding;
    }

    private void ReleaseHeldLetter()
    {
        if (_heldLetter == null)
        {
            return;
        }

        // その場に落とす
        _heldLetter.bodyType = RigidbodyType2D.Dynamic;
        RestoreHeldLetterCollisionWithPlayer();
        _heldLetter.linearVelocity = Vector2.zero;
        _heldLetter.angularVelocity = 0f;

        // 持っていない状態に戻す
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
        // 今の角度から投げる方向を作る
        var radians = _currentAimAngle * Mathf.Deg2Rad;
        var direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));

        // 左向きのときはX方向を反転する
        if (_facingSign < 0f)
        {
            direction.x *= -1f;
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
        // ほぼ止まっているときは向きを変えない
        if (_playerRigidbody2D == null || Mathf.Abs(_playerRigidbody2D.linearVelocity.x) <= 0.01f)
        {
            return;
        }

        // 移動方向を今の向きとして記録する
        _facingSign = Mathf.Sign(_playerRigidbody2D.linearVelocity.x);

        // 持つ方向をSpriteの反転に反映させる
        var spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            _facingSign = spriteRenderer.flipX ? -1f : 1f;
        }
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

                // 持っている間はプレイヤーと当たらないようにする
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
        // 矢印の線部分と先端部分を作る
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

        // 持っていない、または狙い中でないなら矢印は表示しない
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

        // 線部分を更新
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
        // 終点から少し戻した位置に先端の線を作る
        var rotatedDirection = Quaternion.Euler(0f, 0f, angleOffset) * -aimDirection;
        return arrowEnd + (Vector3)(rotatedDirection.normalized * aimArrowHeadLength);
    }

    private void OnDrawGizmosSelected()
    {
        // Sceneビューで拾える範囲が分かるように表示
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}
