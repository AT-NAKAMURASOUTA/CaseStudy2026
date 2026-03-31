using UnityEngine;

public class SpecialAreaCollision : MonoBehaviour
{
    //加速エリアに触れているかのフラグ
    bool accelerationHitFlag = false;
    //低重力エリアに触れているかのフラグ
    bool lowGravityHitFlag = false;


    void OnTriggerStay2D(Collider2D collision)
    {
        if(collision.tag == "AccelerationAreaTag")
        {//加速エリア進入中

            accelerationHitFlag = true;Debug.Log("ok");
        }
        if(collision.tag == "LowGravityTag")
        {//低重力エリア

            lowGravityHitFlag = true;
        }
    }

}
