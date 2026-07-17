using UnityEngine;

public class AlphabetCollisionControl : MonoBehaviour
{
    private Rigidbody2D rb;

    private PolygonCollider2D pc;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        pc = GetComponent<PolygonCollider2D>();
    }

    // Update is called once per frame
    void Update()
    {
        if (rb.bodyType != RigidbodyType2D.Dynamic)
        {
            pc.enabled = false;
        }
        else
        {
            pc.enabled = true;
        }
    }
}
