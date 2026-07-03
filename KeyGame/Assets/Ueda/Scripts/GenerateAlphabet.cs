using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using TMPro;

public class GenerateAlphabet : MonoBehaviour
{
    private sealed class AlphabetRecord
    {
        public readonly List<GameObject> Objects = new List<GameObject>();
    }

    [Header("生成位置のオフセット（前方向）")]
    [SerializeField]
    private float forwardOffset = 1.0f;
    [Header("生成位置のオフセット（上方向）")]
    [SerializeField]
    private float upwardOffset = 0.0f;
    [Header("生成する文字の大きさ")]
    [SerializeField]
    private float alphabetScale = 1.0f;
    [Header("生成する文字のLayer")]
    [SerializeField]
    private LayerMask alphabetLayer;

    [Header("このY座標を下回ると削除")]
    [SerializeField]
    private float destroyY = -10f;

    [Header("同時に出せる文字数")]
    [SerializeField]
    private int maxAlphabetCount = 3;

    private readonly List<AlphabetRecord> alphabetRecords = new List<AlphabetRecord>();

    [Header("文字生成のクールタイム")]
    [SerializeField]
    private float alphabetCooldown = 0.3f;

    private float nextSpawnTime = 0f;

    [Header("文字同士が重ならないための間隔")]
    [SerializeField]
    private float spawnSpacing = 0.6f;

    [Header("生成位置をずらして探す最大回数")]
    [SerializeField]
    private int maxSpawnShiftCount = 6;

    [Header("残り文字数表示")]
    [SerializeField]
    private TMP_Text alphabetCountText;

    [Header("アルファベットに渡す用のScriptableObjectのデータ")]
    [SerializeField] ScriptableObject_SpecialAreaData specialAreaData;

    [Header("アルファベットのアウトラインのプロパティ")]
    [SerializeField]
    private OutLinProperty outLinProperty;

    public List<Sprite> alphabetSprites = new List<Sprite>();

    readonly private int NOKEY = -1;

    private float facingDirection = 1f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        UpdateAlphabetCountText();
    }

    // Update is called once per frame
    void Update()
    {
        var playerSpriteRenderer = GetComponent<SpriteRenderer>();
        if (playerSpriteRenderer != null)
        {
            facingDirection = playerSpriteRenderer.flipX ? -1f : 1f;
        }
        else
        {
            var rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                if (Mathf.Abs(rb.linearVelocityX) > 2.0f)
                    facingDirection = Mathf.Sign(rb.linearVelocityX);
            }
        }



        // キーボードのアルファベットキーが押されたかをチェック
        int alphabetIndex = GetKeyboardAlphabet();
        if (NOKEY == alphabetIndex)
        {
            return;
        }

        if (Time.time < nextSpawnTime)
        {
            return;
        }

        CleanupAlphabetRecords();

        if (maxAlphabetCount <= 0)
        {
            return;
        }

        if (alphabetRecords.Count >= maxAlphabetCount)
        {
            DestroyOldestAlphabet();
        }

        Vector3 spawnPosition = FindSpawnPosition();

        //生成
        GameObject go = new GameObject("Alphabet");
        go.tag = "AlphabetTag";//タグを設定
        go.layer = Mathf.RoundToInt(Mathf.Log(alphabetLayer.value, 2));//レイヤーを設定
        var spriteRenderer = go.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = alphabetSprites[alphabetIndex];
        go.AddComponent<PolygonCollider2D>();
        go.AddComponent<Rigidbody2D>();
        go.AddComponent<AlphabetSpecialAreaInUpdate>().SetScriptableObject(specialAreaData);//特殊エリアの処理
        go.AddComponent<AlphabetRigidbody>();
        var outline = go.AddComponent<AlphabetOutLine>();
        outline.outLinProperty = outLinProperty;
        var destroyOnFall = go.AddComponent<DestroyOnFall>();
        var alphabetWallReaction = go.AddComponent<AlphabetWallReaction>();
        var alphabetCuttable = go.AddComponent<AlphabetCuttable>();
        destroyOnFall.SetDestroyY(destroyY);
        destroyOnFall.SetOwner(this);
        alphabetWallReaction.SetAlphabetCharacter((char)('A' + alphabetIndex));
        alphabetCuttable.SetOwner(this);
        RegisterAlphabet(go);
        UpdateAlphabetCountText();
        nextSpawnTime = Time.time + alphabetCooldown;

        var tf = go.transform;
        tf.position = spawnPosition;
        tf.localScale = Vector3.one * alphabetScale;
    }

    private Vector3 FindSpawnPosition()
    {
        Vector3 basePosition = transform.position + (new Vector3(facingDirection, 0.0f) * forwardOffset) + transform.up * upwardOffset;

        for (int i = 0; i <= maxSpawnShiftCount; i++)
        {
            Vector3 candidate = basePosition + new Vector3(spawnSpacing * i * facingDirection, 0.0f, 0.0f);
            if (!IsAlphabetOverlapping(candidate))
            {
                return candidate;
            }
        }

        return basePosition + new Vector3(spawnSpacing * (maxSpawnShiftCount + 1) * facingDirection, 0.0f, 0.0f);
    }

    private bool IsAlphabetOverlapping(Vector3 position)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(position, spawnSpacing * 0.45f);
        foreach (var hit in hits)
        {
            if (hit == null)
            {
                continue;
            }

            if (hit.gameObject.name.StartsWith("Alphabet"))
            {
                return true;
            }
        }

        return false;
    }

    private int GetKeyboardAlphabet()
    {
        var keyborad = Keyboard.current;
        int result = -1;
        if (keyborad == null) return -1;
        for (int i = 0; i < 26; i++)
        {
            var key = keyborad[(Key)((int)Key.A + i)];
            if (key.wasPressedThisFrame)
            {
                result = i;
                break;
            }
        }
        return result;
    }

    public void NotifyAlphabetDestroyed()
    {
        CleanupAlphabetRecords();
        UpdateAlphabetCountText();
    }

    public void NotifyAlphabetDestroyed(GameObject alphabetObject)
    {
        RemoveAlphabetRecord(alphabetObject, false);
        CleanupAlphabetRecords();
        UpdateAlphabetCountText();
    }

    public void ReplaceAlphabetWithFragments(GameObject sourceAlphabet, GameObject leftFragment, GameObject rightFragment)
    {
        AlphabetRecord record = FindAlphabetRecord(sourceAlphabet);
        if (record == null)
        {
            record = new AlphabetRecord();
            alphabetRecords.Add(record);
        }

        record.Objects.Clear();
        AddTrackedObject(record, leftFragment);
        AddTrackedObject(record, rightFragment);
        CleanupAlphabetRecords();
        UpdateAlphabetCountText();
    }

    private void RegisterAlphabet(GameObject alphabetObject)
    {
        AlphabetRecord record = new AlphabetRecord();
        AddTrackedObject(record, alphabetObject);
        alphabetRecords.Add(record);
    }

    private void AddTrackedObject(AlphabetRecord record, GameObject alphabetObject)
    {
        if (record == null || alphabetObject == null)
        {
            return;
        }

        record.Objects.Add(alphabetObject);
    }

    private AlphabetRecord FindAlphabetRecord(GameObject alphabetObject)
    {
        if (alphabetObject == null)
        {
            return null;
        }

        foreach (AlphabetRecord record in alphabetRecords)
        {
            if (record == null)
            {
                continue;
            }

            foreach (GameObject trackedObject in record.Objects)
            {
                if (trackedObject == alphabetObject)
                {
                    return record;
                }
            }
        }

        return null;
    }

    private void RemoveAlphabetRecord(GameObject alphabetObject, bool destroyObjects)
    {
        AlphabetRecord record = FindAlphabetRecord(alphabetObject);
        if (record == null)
        {
            return;
        }

        if (destroyObjects)
        {
            DestroyAlphabetObjects(record);
        }

        alphabetRecords.Remove(record);
    }

    private void DestroyOldestAlphabet()
    {
        CleanupAlphabetRecords();
        if (alphabetRecords.Count == 0)
        {
            return;
        }

        AlphabetRecord oldestRecord = alphabetRecords[0];
        DestroyAlphabetObjects(oldestRecord);
        alphabetRecords.RemoveAt(0);
        UpdateAlphabetCountText();
    }

    private void DestroyAlphabetObjects(AlphabetRecord record)
    {
        if (record == null)
        {
            return;
        }

        foreach (GameObject alphabetObject in record.Objects)
        {
            if (alphabetObject != null)
            {
                Destroy(alphabetObject);
            }
        }
    }

    private void CleanupAlphabetRecords()
    {
        for (int i = alphabetRecords.Count - 1; i >= 0; i--)
        {
            AlphabetRecord record = alphabetRecords[i];
            if (record == null)
            {
                alphabetRecords.RemoveAt(i);
                continue;
            }

            record.Objects.RemoveAll(alphabetObject => alphabetObject == null);
            if (record.Objects.Count == 0)
            {
                alphabetRecords.RemoveAt(i);
            }
        }
    }

    private void UpdateAlphabetCountText()
    {
        if (alphabetCountText == null)
        {
            return;
        }

        CleanupAlphabetRecords();
        int remainingCount = Mathf.Max(0, maxAlphabetCount - alphabetRecords.Count);
        alphabetCountText.text = $"{remainingCount}";

    }
}
