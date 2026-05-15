using Unity.VisualScripting;
using UnityEngine;

public class SwitchDownUpdate : MonoBehaviour
{
    

    [SerializeField] SwitchCollision collision;
    [Header("ボタン押されたときの位置")]
    [SerializeField] Transform onTransform;



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

            float angle = transform.eulerAngles.z;


            var pos = onTransform.position;
            //沈み
            transform.position = new Vector3(pos.x, pos.y, pos.z);
        }
        else
        {//押されていない

            transform.position = new Vector3(startPos.x, startPos.y, startPos.z);
        }
    }
}
