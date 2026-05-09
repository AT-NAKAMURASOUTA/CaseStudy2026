using UnityEngine;

public class SwitchDownUpdate : MonoBehaviour
{
    [SerializeField] SwitchCollision collision;
    [Header("スイッチ押されたときの沈む量")]
    [SerializeField] float onDownNum = 0.25f;

    Vector3 startPos;//

    void Start()
    {
        startPos = new Vector3(
            transform.position.x,
            transform.position.y, 
            transform.position.z);//
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if(collision.GetCollisionFlag())
        {//ボタン押されている！

            //沈み
            transform.position = new Vector3(startPos.x, startPos.y - onDownNum, startPos.z);
        }
        else
        {//押されていない

            transform.position = new Vector3(startPos.x, startPos.y, startPos.z);
        }
    }
}
