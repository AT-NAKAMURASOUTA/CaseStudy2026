using UnityEngine;

public class MoveWings : MonoBehaviour
{
    [Header("両翼")]
    [SerializeField]
    private Transform LeftWing;

    [SerializeField]
    private Transform RightWing;

    [Header("ぱたぱた周期")]
    [SerializeField]
    private float wingRoop = 0.5f;

    [Header("ぱたぱた幅")]
    [SerializeField]
    private float wingWidth = 30.0f;

    private float wingTimer = 0.0f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (LeftWing == null || RightWing == null) return;

        wingTimer += Time.deltaTime * wingRoop;

        float wingOffset = Mathf.Sin(wingTimer) * wingWidth;

        LeftWing.localRotation = Quaternion.Euler(0, 0, wingOffset);
        RightWing.localRotation = Quaternion.Euler(0, 0, -wingOffset);
    }
}
