using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using TMPro;

public class GenerateAlphabet : MonoBehaviour
{
    [Header("生成位置のオフセット（前方向）")]
    [SerializeField]
    private float forwardOffset = 1.0f;
    [Header("生成位置のオフセット（上方向）")]
    [SerializeField]
    private float upwardOffset = 0.0f;
    [Header("生成する文字の大きさ")]
    [SerializeField]
    private float alphabetScale = 1.0f;

    [Header("このY座標を下回ると削除")]
    [SerializeField]
    private float destroyY = -10f;

    [Header("同時に出せる文字数")]
    [SerializeField]
    private int maxAlphabetCount = 3;

    private int currentAlphabetCount = 0;

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

        if (currentAlphabetCount >= maxAlphabetCount)
        {
            return;
        }

        if (Time.time < nextSpawnTime)
        {
            return;
        }

        Vector3 spawnPosition = FindSpawnPosition();

        //生成
        GameObject go = new GameObject("Alphabet");
        go.tag = "AlphabetTag";//タグを設定
        var spriteRenderer = go.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = alphabetSprites[alphabetIndex];
        go.AddComponent<PolygonCollider2D>();
        go.AddComponent<Rigidbody2D>();
        go.AddComponent<AlphabetSpecialAreaInUpdate>().SetScriptableObject(specialAreaData);//特殊エリアの処理
        go.AddComponent<AlphabetRigidbody>();
        var destroyOnFall = go.AddComponent<DestroyOnFall>();
        var alphabetWallReaction = go.AddComponent<AlphabetWallReaction>();
        var alphabetCuttable = go.AddComponent<AlphabetCuttable>();
        destroyOnFall.SetDestroyY(destroyY);
        destroyOnFall.SetOwner(this);
        alphabetWallReaction.SetAlphabetCharacter((char)('A' + alphabetIndex));
        alphabetCuttable.SetOwner(this);
        currentAlphabetCount++;
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
        currentAlphabetCount = Mathf.Max(0, currentAlphabetCount - 1);
        UpdateAlphabetCountText();
    }

    private void UpdateAlphabetCountText()
    {
        if (alphabetCountText == null)
        {
            return;
        }

        int remainingCount = maxAlphabetCount - currentAlphabetCount;
        alphabetCountText.text = $"{remainingCount}";

    }
}
