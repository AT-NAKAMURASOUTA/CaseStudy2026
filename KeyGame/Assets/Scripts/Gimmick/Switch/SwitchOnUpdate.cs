using UnityEngine;


public class SwitchOnUpdate : MonoBehaviour
{
    //スイッチ押されたときの演出
    SwitchCollision script_Collision;
    [Header("扉")]
    [SerializeField]GameObject chilledObj;

    void Start()
    {
        script_Collision = this.gameObject.AddComponent<SwitchCollision>();
    }


    void Update()
    {
        if(script_Collision.GetCollisionFlag())
        {//Hitしました！

            if (chilledObj != null)
            {//オブジェクトあれば

                Destroy(chilledObj);
               
            }
        }
    }
}
