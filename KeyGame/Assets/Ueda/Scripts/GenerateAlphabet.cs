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

    [Header("残り文字数表示")]
    [SerializeField]
    private TMP_Text alphabetCountText;

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

        //生成
        GameObject go = new GameObject("Alphabet");
        var spriteRenderer = go.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = alphabetSprites[alphabetIndex];
        go.AddComponent<PolygonCollider2D>();
        go.AddComponent<Rigidbody2D>();
        var destroyOnFall = go.AddComponent<DestroyOnFall>();
        destroyOnFall.SetDestroyY(destroyY);
        destroyOnFall.SetOwner(this);
        currentAlphabetCount++;
        UpdateAlphabetCountText();
        nextSpawnTime = Time.time + alphabetCooldown;

        var tf = go.transform;
        tf.position = transform.position + (new Vector3(facingDirection, 0.0f) * forwardOffset) + transform.up * upwardOffset;
        tf.localScale = Vector3.one * alphabetScale;
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
