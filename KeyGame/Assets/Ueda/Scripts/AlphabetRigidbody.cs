using UnityEngine;

public class AlphabetRigidbody : MonoBehaviour
{
    private Rigidbody2D rb;

    private float windPower = 0f;

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
        windPower = windStrength;
    }

    public void ExitWindArea()
    {
        windPower = 0f;
    }   
}
