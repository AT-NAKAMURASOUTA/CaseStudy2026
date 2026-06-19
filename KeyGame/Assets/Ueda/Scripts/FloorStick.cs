using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class FloorStick : MonoBehaviour
{
    [Header("くっつくかどうか")]
    [SerializeField]
    private bool isStick = true;

    [Header("移動するオブジェクトのタグ")]
    [SerializeField]
    private List<string> targetTags = new List<string>();

    public List<string> tTags => targetTags;

    //指定したTagのオブジェクトをくっついて移動するようにする
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isStick) return;
        if (targetTags.Contains(collision.gameObject.tag))
        {
            foreach (ContactPoint2D contact in collision.contacts)
            {
                //接点が下方向にあるときくっつく
                if (contact.normal.y < -0.7f)
                {
                    collision.transform.SetParent(transform);

                    if (collision.transform.GetComponent<ParentOnParent>() == null)
                        collision.transform.AddComponent<ParentOnParent>();
                    break;
                }
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (!isStick) return;
        if (targetTags.Contains(collision.gameObject.tag))
        {
            if (!collision.gameObject.activeInHierarchy) return;

            collision.transform.SetParent(null);

            if (collision.transform.TryGetComponent<ParentOnParent>(out var pop))
            {
                Destroy(pop);
            }
        }
    }

    //つぶされるなどして、プレイヤーがめり込むとミス判定を出す
    // Collisionの内側に小さめのTriggerを設置

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isStick) return;
        if (collision == null)
        {
            return;
        }
        if (collision.TryGetComponent<PlayerRespawn>(out var playerRespawn))
        {
            playerRespawn.TriggerMiss();
        }
    }
}

public class ParentOnParent : MonoBehaviour
{
    private List<string> targetTags = new List<string>();
    private Transform parentTf;
    private void Awake()
    {
        if (transform.parent != null)
        {
            var mf = transform.parent.GetComponentInParent<FloorStick>();
            parentTf = transform.parent.transform;
            targetTags = new List<string>(mf.tTags);
        }
    }

    //指定したTagのオブジェクトをくっついて移動するようにする
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (targetTags.Contains(collision.gameObject.tag))
        {
            if (collision.transform.GetComponent<ParentOnParent>() == null)
            {
                collision.transform.SetParent(parentTf);
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (targetTags.Contains(collision.gameObject.tag))
        {
            if (!collision.gameObject.activeInHierarchy) return;

            if (collision.transform.GetComponent<ParentOnParent>() == null)
            {
                if (!collision.gameObject.activeInHierarchy) return;
                collision.transform.SetParent(null);
            }
        }
    }
}
