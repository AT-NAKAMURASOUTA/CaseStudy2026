using UnityEngine;

public class SpecialAreaVelocityUpdate 
{
    /*
    特殊エリアの処理（速度の変更値）を１纏めにして、
    変更があってもこのスクリプトの変更のみで済むように
    関数を作りました。
    */





    //特殊エリアのUpdate
    public Vector3 SpecialAreaUpdate(
        Vector3 nowVelocity,//現在の移動速度
        SpecialAreaCollision collision,//Areaの当たり判定
        ScriptableObject_SpecialAreaData numAssets//特殊エリアの速度増減数値
        )
    {

        //加速
        nowVelocity = AccelerationUpdate(nowVelocity,collision,numAssets);

        //低重力
        nowVelocity = LowGravityUpdate(nowVelocity,collision,numAssets);


        return nowVelocity;
    }

    //加速度のみ
    public Vector3 AccelerationUpdate(Vector3 nowVelocity,//現在の移動速度
        SpecialAreaCollision collision,//Areaの当たり判定
        ScriptableObject_SpecialAreaData numAssets)
    {

        if (collision.GetAccelerationCollision())
        {//加速する

            //ScriptableObjectにSetしてある値（倍率）を使う
            nowVelocity.x *= numAssets.GetAccelerationMagnification();
        }

        return nowVelocity;
    }

    //低重力のみ
    public Vector3 LowGravityUpdate(Vector3 nowVelocity,//現在の移動速度
        SpecialAreaCollision collision,//Areaの当たり判定
        ScriptableObject_SpecialAreaData numAssets)
    {
        //縦移動
        if (collision.GetLowGravityCollision())
        {//落下速度遅く、上昇速度遅く

            //ScriptableObjectにSetしてある値（倍率）を使う
            nowVelocity.y *= numAssets.GetLowGravityMagnification();
        }

        return nowVelocity;
    }
}
