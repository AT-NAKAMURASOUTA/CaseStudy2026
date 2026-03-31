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

            accelerationHitFlag = true;
        }
        if(collision.tag == "LowGravityTag")
        {//低重力エリア

            lowGravityHitFlag = true;
        }
    }

    //当たり判定から外れた時
    void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.tag == "AccelerationAreaTag")
        {//加速エリア進入中

            accelerationHitFlag = false;

        }
        if (collision.tag == "LowGravityTag")
        {//低重力エリア

            lowGravityHitFlag = false;
        }
    }

    //加速エリアに入っているかの判定
    public bool GetAccelerationCollision()
    {
        return accelerationHitFlag;

    }
}
