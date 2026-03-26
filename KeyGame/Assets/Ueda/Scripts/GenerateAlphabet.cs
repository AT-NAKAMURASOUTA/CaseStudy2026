using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

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



    public List<Sprite> alphabetSprites = new List<Sprite>();

    readonly private int NOKEY = -1;

    private float facingDirection = 1f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            if (Mathf.Abs(rb.linearVelocityX) > 2.0f)
                facingDirection = Mathf.Sign(rb.linearVelocityX);
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
    }

}
