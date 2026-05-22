using UnityEngine;

public class GlueArea : MonoBehaviour
{
    [Header("接着するオブジェクトのタグ")]
    [SerializeField]
    private string targetTag = "AlphabetTag";

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // タグが一致するオブジェクトが接触した場合
        if (collision.CompareTag(targetTag))
        {
            //接着したオブジェクトを固定する
            collision.attachedRigidbody.constraints = RigidbodyConstraints2D.FreezeAll;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        // タグが一致するオブジェクトが接触した場合
        if (collision.CompareTag(targetTag))
        {
            //接着したオブジェクトの固定を解除する
            collision.attachedRigidbody.constraints = RigidbodyConstraints2D.None;
        }
    }
}
