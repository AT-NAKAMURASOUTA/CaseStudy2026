using UnityEngine;
using UnityEngine.InputSystem;

public sealed class AlphabetThrowController : MonoBehaviour
{
    // 迴ｾ蝨ｨ縺ｮ迥ｶ諷・
    private enum HoldState
    {
        None,    // 菴輔ｂ謖√▲縺ｦ縺・↑縺・
        Holding, // 謖√▲縺ｦ縺・ｋ縺縺代・迥ｶ諷・
        Aiming,  // 謚輔￡繧区婿蜷代ｒ豎ｺ繧√※縺・ｋ迥ｶ諷・
    }

    [Header("諡ｾ縺医ｋ遽・峇")]
    [SerializeField]
    private float pickupRadius = 1.2f;

    [Header("謖√▽菴咲ｽｮ縺ｮ蜑肴婿蜷代・霍晞屬")]
    [SerializeField]
    private float holdDistance = 0.9f;

    [Header("謖√▽菴咲ｽｮ縺ｮ鬮倥＆")]
    [SerializeField]
    private float holdHeight = 1.0f;

    [Header("諡ｾ縺医ｋ繝ｬ繧､繝､繝ｼ")]
    [SerializeField]
    private LayerMask pickableLayers = ~0;

    [Header("諡ｾ縺医ｋ繧ｪ繝悶ず繧ｧ繧ｯ繝亥錐")]
    [SerializeField]
    private string pickableObjectName = "Alphabet";

    [Header("謚輔￡繧矩溘＆")]
    [SerializeField]
    private float throwSpeed = 12f;

    [Header("Throw Torque")]
    [SerializeField]
    private float throwTorque = 8f;

    [Header("辣ｧ貅冶ｧ貞ｺｦ縺ｮ荳矩剞")]
    [SerializeField]
    private float minAimAngle = 30f;

    [Header("辣ｧ貅冶ｧ貞ｺｦ縺ｮ荳企剞")]
    [SerializeField]
    private float maxAimAngle = 90f;

    [Header("辣ｧ貅冶ｧ貞ｺｦ縺ｮ蠕蠕ｩ騾溷ｺｦ")]
    [SerializeField]
    private float aimSwingSpeed = 120f;


    [Header("Aim Arrow Length")]
    [SerializeField]
    private float aimArrowLength = 1.8f;

    [Header("Aim Arrow Width")]
    [SerializeField]
    private float aimArrowWidth = 0.05f;

    [Header("Aim Arrow Head Length")]
    [SerializeField]
    private float aimArrowHeadLength = 0.28f;

    [Header("遏｢蜊ｰ縺ｮ蜈育ｫｯ縺ｮ隗貞ｺｦ")]
    [SerializeField]
    private float aimArrowHeadAngle = 28f;

    [Header("遏｢蜊ｰ縺ｮ蜑肴婿蜷代・縺壹ｉ縺鈴㍼")]
    [SerializeField]
    private float aimArrowForwardOffset = 0.35f;

    [Header("遏｢蜊ｰ縺ｮ荳頑婿蜷代・縺壹ｉ縺鈴㍼")]
    [SerializeField]
    private float aimArrowHeightOffset = 0.5f;

    [Header("遏｢蜊ｰ縺ｮ濶ｲ")]
    [SerializeField]
    private Color aimArrowColor = Color.yellow;

    // 繝｡繧､繝ｳ繧ｫ繝｡繝ｩ
    private Camera _mainCamera;

    // 繝励Ξ繧､繝､繝ｼ譛ｬ菴薙・Rigidbody2D
    private Rigidbody2D _playerRigidbody2D;

    // 繝励Ξ繧､繝､繝ｼ縺ｫ縺､縺・※縺・ｋCollider2D縺ｮ荳隕ｧ
    private Collider2D[] _playerColliders;

    // 莉頑戟縺｣縺ｦ縺・ｋ繧｢繝ｫ繝輔ぃ繝吶ャ繝・
    private Rigidbody2D _heldLetter;

    // 莉翫・謖√■迥ｶ諷・
    private HoldState _holdState;

    // 繝励Ξ繧､繝､繝ｼ縺悟髄縺・※縺・ｋ譁ｹ蜷・
    // 蜿ｳ蜷代″縺ｪ繧・縲∝ｷｦ蜷代″縺ｪ繧・1
    private float _facingSign = 1f;

    // 遏｢蜊ｰ縺ｮ譽帝Κ蛻・
    private LineRenderer _aimShaft;

    // 遏｢蜊ｰ縺ｮ蜈育ｫｯ驛ｨ蛻・
    private LineRenderer _aimHead;

    // 迴ｾ蝨ｨ縺ｮ迢吶＞隗貞ｺｦ
    private float _currentAimAngle = 30f;

    // 迢吶＞隗貞ｺｦ縺ｮ騾ｲ陦梧婿蜷・
    // 1 縺ｧ蠅怜刈縲・1 縺ｧ貂帛ｰ・
    private float _aimAngleDirection = 1f;


    private void Awake()
    {
        // 譛蛻昴↓蠢・ｦ√↑蜿ら・繧貞叙縺｣縺ｦ縺翫￥
        _mainCamera = Camera.main;
        _playerRigidbody2D = GetComponent<Rigidbody2D>();
        _playerColliders = GetComponents<Collider2D>();

        // 遏｢蜊ｰ繧定｡ｨ遉ｺ縺吶ｋ縺溘ａ縺ｮLineRenderer繧剃ｽ懊ｋ
        CreateAimRenderers();
    }

    private void Update()
    {
        // 繧ｫ繝｡繝ｩ蜿ら・縺悟・繧後※縺・◆繧牙叙繧顔峩縺・
        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
        }

        if (_heldLetter == null && _holdState != HoldState.None)
        {
            _holdState = HoldState.None;
        }

        // 豈弱ヵ繝ｬ繝ｼ繝蠢・ｦ√↑蜃ｦ逅・
        UpdateFacingDirection();
        HandleMouseInput();
        HandleKeyboardInput();

        // 迢吶＞荳ｭ縺縺題ｧ貞ｺｦ繧定・蜍輔〒蠕蠕ｩ縺輔○繧・
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

        // 蜿ｳ繧ｯ繝ｪ繝・け縺輔ｌ縺滓凾縺ｮ蜃ｦ逅・
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            HandleRightClick();
            return;
        }

        // 蟾ｦ繧ｯ繝ｪ繝・け縺輔ｌ縺ｦ縺・↑縺代ｌ縺ｰ菴輔ｂ縺励↑縺・
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

        // 繧ｹ繝壹・繧ｹ縺梧款縺輔ｌ縺ｦ縺・↑縺代ｌ縺ｰ菴輔ｂ縺励↑縺・
        if (!Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            return;
        }

        HandlePrimaryAction();
    }

    private void HandlePrimaryAction()
    {
        // 莉翫・迥ｶ諷九↓繧医▲縺ｦ蟾ｦ繧ｯ繝ｪ繝・け繧・せ繝壹・繧ｹ縺ｮ諢丞袖繧貞､峨∴繧・
        switch (_holdState)
        {
            case HoldState.None:
                // 菴輔ｂ謖√▲縺ｦ縺・↑縺・凾縺ｯ霑代￥縺ｮ譁・ｭ励ｒ諡ｾ縺・
                TryPickLetter();
                break;

            case HoldState.Holding:
                // 謖√▲縺ｦ縺・ｋ譎ゅ・辣ｧ貅也憾諷九↓蜈･繧・
                StartAiming();
                break;

            case HoldState.Aiming:
                // 辣ｧ貅紋ｸｭ縺ｪ繧画兜縺偵ｋ
                ThrowHeldLetter();
                break;
        }
    }

    private void HandleRightClick()
    {
        switch (_holdState)
        {
            case HoldState.Holding:
                // 謖√▲縺ｦ縺・ｋ縺縺代・譎ゅ・縺昴・蝣ｴ縺ｧ髮｢縺・
                ReleaseHeldLetter();
                break;

            case HoldState.Aiming:
                // 辣ｧ貅紋ｸｭ縺ｯ讒九∴繧偵ｄ繧√※謖√▲縺ｦ縺・ｋ迥ｶ諷九↓謌ｻ縺・
                CancelAiming();
                break;
        }
    }

    private void TryPickLetter()
    {
        // 繝励Ξ繧､繝､繝ｼ縺ｮ霑代￥縺ｫ縺ゅｋ縲∵鏡縺医ｋ蟇ｾ雎｡繧貞・驛ｨ隱ｿ縺ｹ繧・
        var hits = Physics2D.OverlapCircleAll(transform.position, pickupRadius, pickableLayers);

        Rigidbody2D nearestLetter = null;
        var nearestDistance = float.MaxValue;

        foreach (var hit in hits)
        {
            // 辟｡蜉ｹ縺ｪ繧ゅ・縺ｯ辟｡隕・
            if (hit == null)
            {
                continue;
            }

            // 謖・ｮ壹＠縺溷錐蜑阪〒蟋九∪繧九が繝悶ず繧ｧ繧ｯ繝医□縺第鏡縺・
            if (!hit.gameObject.name.StartsWith(pickableObjectName))
            {
                continue;
            }

            var rb = hit.attachedRigidbody;

            // Rigidbody2D縺御ｻ倥＞縺ｦ縺・↑縺・ｂ縺ｮ縺ｯ蟇ｾ雎｡螟・
            if (rb == null)
            {
                continue;
            }

            // 繝励Ξ繧､繝､繝ｼ縺ｮ蠕後ｍ蛛ｴ縺ｫ縺ゅｋ譁・ｭ励・諡ｾ繧上↑縺・
            var toLetter = rb.worldCenterOfMass - (Vector2)transform.position;
            if (toLetter.x * _facingSign < 0f)
            {
                continue;
            }

            // 荳逡ｪ霑代＞繧ゅ・繧帝∈縺ｶ
            var distance = Vector2.Distance(transform.position, rb.worldCenterOfMass);
            if (distance >= nearestDistance)
            {
                continue;
            }

            nearestDistance = distance;
            nearestLetter = rb;
        }

        // 菴輔ｂ隕九▽縺九ｉ縺ｪ縺代ｌ縺ｰ邨ゅｏ繧・
        if (nearestLetter == null)
        {
            return;
        }

        // 隕九▽縺代◆譁・ｭ励ｒ謖√▽
        _heldLetter = nearestLetter;

        // 謖√▲縺溽椪髢薙↓蜍輔″繧呈ｭ｢繧√ｋ
        _heldLetter.linearVelocity = Vector2.zero;
        _heldLetter.angularVelocity = 0f;

        // 謇句・縺ｫ蝗ｺ螳壹＠縺溘＞縺ｮ縺ｧKinematic縺ｫ縺吶ｋ
        _heldLetter.bodyType = RigidbodyType2D.Kinematic;
        _holdState = HoldState.Holding;

        // 謖√▲縺溽椪髢薙↓蜑肴婿縺ｮ謖√▽菴咲ｽｮ縺ｸ遘ｻ蜍輔☆繧・
        _heldLetter.transform.position = GetHoldPosition(holdDistance, holdHeight);
        _heldLetter.transform.rotation = Quaternion.identity;

        // 繝励Ξ繧､繝､繝ｼ縺ｨ縺ｶ縺､縺九ｉ縺ｪ縺・ｈ縺・↓縺吶ｋ
        IgnoreHeldLetterCollisionWithPlayer();
    }

    private void StartAiming()
    {
        if (_heldLetter == null)
        {
            return;
        }

        // 辣ｧ貅夜幕蟋区凾縺ｯ荳矩剞隗貞ｺｦ縺九ｉ繧ｹ繧ｿ繝ｼ繝医☆繧・
        _currentAimAngle = minAimAngle;
        _aimAngleDirection = 1f;

        // 辣ｧ貅也憾諷九↓蜈･繧・
        _holdState = HoldState.Aiming;
    }

    private void UpdateAimOscillation()
    {
        // 險ｭ螳壹＠縺滄溷ｺｦ縺ｧ辣ｧ貅冶ｧ貞ｺｦ繧貞ｾ蠕ｩ縺輔○繧・
        _currentAimAngle += aimSwingSpeed * _aimAngleDirection * Time.deltaTime;

        // 荳企剞縺ｫ驕斐＠縺溘ｉ謚倥ｊ霑斐☆
        if (_currentAimAngle >= maxAimAngle)
        {
            _currentAimAngle = maxAimAngle;
            _aimAngleDirection = -1f;
        }
        // 荳矩剞縺ｫ驕斐＠縺溘ｉ謚倥ｊ霑斐☆
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

        // 謖√▲縺ｦ縺・ｋ髢薙・蟶ｸ縺ｫ豎ｺ縺ｾ縺｣縺滉ｽ咲ｽｮ縺ｸ遘ｻ蜍輔＆縺帙ｋ
        _heldLetter.transform.position = GetHoldPosition(holdDistance, holdHeight);

        // 隕九◆逶ｮ縺悟だ縺九↑縺・ｈ縺・↓蝗櫁ｻ｢繧呈綾縺・
        _heldLetter.transform.rotation = Quaternion.identity;
    }

    private void ThrowHeldLetter()
    {
        if (_heldLetter == null)
        {
            return;
        }

        var aimDirection = GetAimDirection();

        // 迚ｩ逅・嫌蜍輔ｒ謌ｻ縺励※謚輔￡繧峨ｌ繧狗憾諷九↓縺吶ｋ
        _heldLetter.bodyType = RigidbodyType2D.Dynamic;
        RestoreHeldLetterCollisionWithPlayer();

        // 豎ｺ繧√◆譁ｹ蜷代∈騾溷ｺｦ繧剃ｸ弱∴縺ｦ鬟帙・縺・
        _heldLetter.linearVelocity = aimDirection * throwSpeed;

        // 蟆代＠蝗櫁ｻ｢繧ょ刈縺医※隕九◆逶ｮ繧定・辟ｶ縺ｫ縺吶ｋ
        _heldLetter.AddTorque(-Mathf.Sign(aimDirection.x) * throwTorque, ForceMode2D.Impulse);

        // 謇区叛縺励◆縺ｮ縺ｧ蜿ら・繧呈ｶ医☆
        _heldLetter = null;
        _holdState = HoldState.None;
    }

    private void CancelAiming()
    {
        if (_heldLetter == null)
        {
            return;
        }

        // 辣ｧ貅悶□縺題ｧ｣髯､縺励※縲∵戟縺｣縺ｦ縺・ｋ迥ｶ諷九↓謌ｻ縺・
        _holdState = HoldState.Holding;
    }

    private void ReleaseHeldLetter()
    {
        if (_heldLetter == null)
        {
            return;
        }

        // 縺昴・蝣ｴ縺ｫ關ｽ縺ｨ縺吶◆繧．ynamic縺ｫ謌ｻ縺・
        _heldLetter.bodyType = RigidbodyType2D.Dynamic;
        RestoreHeldLetterCollisionWithPlayer();
        _heldLetter.linearVelocity = Vector2.zero;
        _heldLetter.angularVelocity = 0f;

        // 謇区叛縺励◆縺ｮ縺ｧ蛻晄悄迥ｶ諷九↓謌ｻ縺・
        _heldLetter = null;
        _holdState = HoldState.None;
    }

    private Vector2 GetMouseWorldPosition()
    {
        // 繝槭え繧ｹ縺ｮ逕ｻ髱｢蠎ｧ讓吶ｒ繝ｯ繝ｼ繝ｫ繝牙ｺｧ讓吶↓螟画鋤縺吶ｋ
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
        // 迴ｾ蝨ｨ縺ｮ隗貞ｺｦ縺九ｉ迢吶＞譁ｹ蜷代ｒ菴懊ｋ
        var radians = _currentAimAngle * Mathf.Deg2Rad;
        var direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));

        // 蟾ｦ蜷代″縺ｮ縺ｨ縺阪・X譁ｹ蜷代ｒ蜿崎ｻ｢縺励※蜑肴婿縺ｸ蜷代￠繧・
        if (_facingSign < 0f)
        {
            direction.x *= -1f;
        }

        return direction.normalized;
    }


    private Vector3 GetHoldPosition(float distance, float height)
    {
        // 繝励Ξ繧､繝､繝ｼ縺ｮ蜑肴婿縲∝ｰ代＠荳翫・菴咲ｽｮ繧定ｿ斐☆
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
        // 縺ｻ縺ｼ豁｢縺ｾ縺｣縺ｦ縺・ｋ譎ゅ・蜷代″繧貞､峨∴縺ｪ縺・
        if (_playerRigidbody2D == null || Mathf.Abs(_playerRigidbody2D.linearVelocity.x) <= 0.01f)
        {
            return;
        }

        // 讓ｪ遘ｻ蜍輔・蜷代″繧堤樟蝨ｨ縺ｮ蜷代″縺ｨ縺励※險倬鹸縺吶ｋ
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

                // 謖√▲縺ｦ縺・ｋ譁・ｭ励′繝励Ξ繧､繝､繝ｼ縺ｫ蠖薙◆繧峨↑縺・ｈ縺・↓縺吶ｋ
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
        // 遏｢蜊ｰ縺ｮ譽帝Κ蛻・→蜈育ｫｯ驛ｨ蛻・ｒ蛻･縲・↓菴懊ｋ
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

        // 譛蛻昴・髱櫁｡ｨ遉ｺ縺ｫ縺励※縺翫￥
        lineRenderer.enabled = false;

        return lineRenderer;
    }

    private void UpdateAimArrow()
    {
        if (_aimShaft == null || _aimHead == null)
        {
            return;
        }

        // 譁・ｭ励ｒ謖√▲縺ｦ縺・↑縺・√∪縺溘・辣ｧ貅紋ｸｭ縺ｧ縺ｪ縺・↑繧臥泙蜊ｰ縺ｯ陦ｨ遉ｺ縺励↑縺・
        if (_heldLetter == null || _holdState != HoldState.Aiming)
        {
            _aimShaft.enabled = false;
            _aimHead.enabled = false;
            return;
        }

        _aimShaft.enabled = true;
        _aimHead.enabled = true;

        var aimDirection = GetAimDirection();

        // 遏｢蜊ｰ縺ｮ蟋狗せ
        var arrowStart =
            _heldLetter.transform.position +
            (Vector3)(GetFacingDirection() * aimArrowForwardOffset) +
            Vector3.up * aimArrowHeightOffset;

        // 遏｢蜊ｰ縺ｮ邨らせ
        var arrowEnd = arrowStart + (Vector3)(aimDirection * aimArrowLength);

        // 譽帝Κ蛻・ｒ譖ｴ譁ｰ
        _aimShaft.SetPosition(0, arrowStart);
        _aimShaft.SetPosition(1, arrowEnd);

        // 蜈育ｫｯ縺ｮ蟾ｦ蜿ｳ縺ｮ轤ｹ繧呈ｱゅａ繧・
        var leftHead = GetArrowHeadPoint(arrowEnd, aimDirection, aimArrowHeadAngle);
        var rightHead = GetArrowHeadPoint(arrowEnd, aimDirection, -aimArrowHeadAngle);

        // 蜈育ｫｯ驛ｨ蛻・ｒ譖ｴ譁ｰ
        _aimHead.SetPosition(0, leftHead);
        _aimHead.SetPosition(1, arrowEnd);
        _aimHead.SetPosition(2, rightHead);
    }

    private Vector3 GetArrowHeadPoint(Vector3 arrowEnd, Vector2 aimDirection, float angleOffset)
    {
        // 邨らせ縺九ｉ蟆代＠譁懊ａ蠕後ｍ縺ｫ轤ｹ繧剃ｽ懊▲縺ｦ遏｢蜊ｰ縺ｮ蜈育ｫｯ縺ｫ縺吶ｋ
        var rotatedDirection = Quaternion.Euler(0f, 0f, angleOffset) * -aimDirection;
        return arrowEnd + (Vector3)(rotatedDirection.normalized * aimArrowHeadLength);
    }

    private void OnDrawGizmosSelected()
    {
        // Scene繝薙Η繝ｼ縺ｧ諡ｾ縺医ｋ遽・峇縺悟・縺九ｋ繧医≧縺ｫ蜀・ｒ謠上￥
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}



