using UnityEngine;

public class AlphabetRigidbody : MonoBehaviour
{
    private Rigidbody2D rb;

    private float windPower = 0f;

    private int inAreaCount = 0;

    private void Awake() => rb = GetComponent<Rigidbody2D>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // 風の影響を受ける場合、風の力を適用
        if (windPower != 0f)
        {
            if (Mathf.Abs(rb.linearVelocityX) <= Mathf.Abs(windPower))
            {
                rb.linearVelocityX = windPower;
            }
            else
            {
                rb.linearVelocityX += windPower * Time.deltaTime;
            }
        }
    }

    //風の影響の適用と解除のメソッド
    public void InWindArea(float windStrength)
    {
        inAreaCount++;
        windPower = windStrength;
    }

    public void ExitWindArea()
    {
        inAreaCount--;
        if (inAreaCount <= 0)
        {
            windPower = 0f;
            inAreaCount = 0;
        }
    }   
}
