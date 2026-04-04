using System.Collections.Generic;
using UnityEngine;

public sealed class HangLever : MonoBehaviour
{
    [Header("文字を引っかける時間")]
    [SerializeField]
    private float requiredHangDuration = 1f;

    [Header("作動で通行可能にする対象")]
    [SerializeField]
    private GameObject targetObject;

    [Header("通常時のレバー角度")]
    [SerializeField]
    private float idleAngle = -22f;

    [Header("作動時のレバー角度")]
    [SerializeField]
    private float activatedAngle = 44f;

    [Header("レバーが倒れる速さ")]
    [SerializeField]
    private float armRotateSpeed = 12f;

    // 今引っかかっている文字をIDで管理
    private readonly HashSet<int> m_HangingLetterIds = new HashSet<int>();

    private Transform m_ArmPivot;
    private Transform m_BaseVisual;
    private Transform m_ArmVisual;
    private float m_HangTimer;
    private bool m_IsActivated;

    private void Reset()
    {
        EnsureVisualColliders();
        ApplyArmAngle(true);
    }

    private void Awake()
    {
        EnsureVisualColliders();
        ApplyArmAngle(true);
    }

    private void OnValidate()
    {
        EnsureVisualColliders();
        ApplyArmAngle(true);
    }

    private void Update()
    {
        if (!m_IsActivated)
        {
            // 文字が引っかかっている間だけタイマーを進める
            if (m_HangingLetterIds.Count > 0)
            {
                m_HangTimer += Time.deltaTime;
                if (m_HangTimer >= requiredHangDuration)
                {
                    Activate();
                }
            }
            else
            {
                // 何も引っかかっていなければタイマーを戻す
                m_HangTimer = 0f;
            }
        }

        // 毎フレーム目標角度に向けてレバーを動かす
        ApplyArmAngle(false);
    }

    public void RegisterHangCandidate(Collider2D other)
    {
        if (TryGetAlphabetId(other, out int alphabetId))
        {
            m_HangingLetterIds.Add(alphabetId);
        }
    }

    public void UnregisterHangCandidate(Collider2D other)
    {
        if (TryGetAlphabetId(other, out int alphabetId))
        {
            m_HangingLetterIds.Remove(alphabetId);

            // 1文字も引っかかっていない状態に戻ったらタイマーもリセット
            if (m_HangingLetterIds.Count == 0)
            {
                m_HangTimer = 0f;
            }
        }
    }

    private void Activate()
    {
        m_IsActivated = true;
        m_HangTimer = 0f;
        m_HangingLetterIds.Clear();

        // 対象を非表示にして通れるように
        if (targetObject != null)
        {
            targetObject.SetActive(false);
        }
    }

    private void EnsureVisualColliders()
    {
        // ルートのColliderは使わないので無効化
        BoxCollider2D rootBoxCollider = GetComponent<BoxCollider2D>();
        if (rootBoxCollider != null)
        {
            rootBoxCollider.enabled = false;
        }

        if (m_BaseVisual == null)
        {
            m_BaseVisual = transform.Find("BaseVisual");
        }

        if (m_ArmPivot == null)
        {
            m_ArmPivot = transform.Find("LeverArmPivot");
        }

        if (m_ArmPivot == null || m_BaseVisual == null)
        {
            return;
        }

        if (m_ArmVisual == null)
        {
            m_ArmVisual = m_ArmPivot.Find("ArmOffset/ArmVisual");
            if (m_ArmVisual == null)
            {
                m_ArmVisual = m_ArmPivot.Find("ArmVisual");
            }
        }

        // 土台側の見た目に合わせてColliderを付ける
        PolygonCollider2D baseCollider = m_BaseVisual.GetComponent<PolygonCollider2D>();
        if (baseCollider == null)
        {
            baseCollider = m_BaseVisual.gameObject.AddComponent<PolygonCollider2D>();
        }
        baseCollider.isTrigger = false;
        SyncColliderToSprite(baseCollider, m_BaseVisual.GetComponent<SpriteRenderer>());

        if (m_ArmVisual == null)
        {
            return;
        }

        // レバー本体にも見た目に合わせたColliderを付ける
        PolygonCollider2D armCollider = m_ArmVisual.GetComponent<PolygonCollider2D>();
        if (armCollider == null)
        {
            armCollider = m_ArmVisual.gameObject.AddComponent<PolygonCollider2D>();
        }
        armCollider.isTrigger = false;
        armCollider.enabled = true;
        SyncColliderToSprite(armCollider, m_ArmVisual.GetComponent<SpriteRenderer>());

        // 文字を引っかけた判定を取るためのTrigger用オブジェクト
        Transform armTriggerTransform = m_ArmVisual.Find("ArmTrigger");
        if (armTriggerTransform == null)
        {
            GameObject triggerObject = new GameObject("ArmTrigger");
            armTriggerTransform = triggerObject.transform;
            armTriggerTransform.SetParent(m_ArmVisual, false);
        }

        armTriggerTransform.localPosition = Vector3.zero;
        armTriggerTransform.localRotation = Quaternion.identity;
        armTriggerTransform.localScale = Vector3.one;

        // Trigger用なので見た目はいらない
        SpriteRenderer triggerRenderer = armTriggerTransform.GetComponent<SpriteRenderer>();
        if (triggerRenderer != null)
        {
            triggerRenderer.enabled = false;
            triggerRenderer.sprite = null;
        }

        // レバーの見た目と同じ形でTriggerを作る
        PolygonCollider2D triggerCollider = armTriggerTransform.GetComponent<PolygonCollider2D>();
        if (triggerCollider == null)
        {
            triggerCollider = armTriggerTransform.gameObject.AddComponent<PolygonCollider2D>();
        }
        triggerCollider.isTrigger = true;
        triggerCollider.enabled = true;
        SyncColliderToSprite(triggerCollider, m_ArmVisual.GetComponent<SpriteRenderer>());

        // Trigger検知用スクリプトを付けてレバーとつなぐ
        HangLeverArmTrigger armTrigger = armTriggerTransform.GetComponent<HangLeverArmTrigger>();
        if (armTrigger == null)
        {
            armTrigger = armTriggerTransform.gameObject.AddComponent<HangLeverArmTrigger>();
        }
        armTrigger.SetOwner(this);
    }

    private static void SyncColliderToSprite(PolygonCollider2D collider2D, SpriteRenderer spriteRenderer)
    {
        if (collider2D == null || spriteRenderer == null || spriteRenderer.sprite == null)
        {
            return;
        }

        Sprite sprite = spriteRenderer.sprite;
        int shapeCount = sprite.GetPhysicsShapeCount();
        if (shapeCount <= 0)
        {
            return;
        }

        collider2D.pathCount = shapeCount;
        List<Vector2> points = new List<Vector2>();

        // スプライトの物理形状をそのままColliderに反映する
        for (int i = 0; i < shapeCount; i++)
        {
            points.Clear();
            sprite.GetPhysicsShape(i, points);
            collider2D.SetPath(i, points);
        }
    }

    private void ApplyArmAngle(bool immediate)
    {
        if (m_ArmPivot == null)
        {
            m_ArmPivot = transform.Find("LeverArmPivot");
            if (m_ArmPivot == null)
            {
                return;
            }
        }

        float targetAngle = m_IsActivated ? activatedAngle : idleAngle;

        // 初期化時や非再生中はその場で角度を合わせる
        if (immediate || !Application.isPlaying)
        {
            m_ArmPivot.localRotation = Quaternion.Euler(0f, 0f, targetAngle);
            return;
        }

        // 再生中は少しずつ目標角度まで回す
        float currentAngle = m_ArmPivot.localEulerAngles.z;
        float nextAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, armRotateSpeed * 90f * Time.deltaTime);
        m_ArmPivot.localRotation = Quaternion.Euler(0f, 0f, nextAngle);
    }

    private static bool TryGetAlphabetId(Collider2D other, out int alphabetId)
    {
        alphabetId = 0;

        if (other == null)
        {
            return false;
        }

        // 接触した相手からAlphabetCuttableを探す
        AlphabetCuttable cuttable = other.GetComponent<AlphabetCuttable>();
        if (cuttable == null)
        {
            cuttable = other.GetComponentInParent<AlphabetCuttable>();
        }

        if (cuttable == null)
        {
            return false;
        }

        alphabetId = cuttable.gameObject.GetInstanceID();
        return true;
    }
}
