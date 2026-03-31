using UnityEngine;

public class AlphabetSpecialAreaInUpdate : MonoBehaviour
{
    //速度の調整

   Rigidbody2D rigid2D;

    ScriptableObject_SpecialAreaData assetData;

    SpecialAreaCollision specialAreaFlag;
    
    //加速度の更新
    bool accelerationFlag = false;
    //低重力の更新
    bool lowGravityFlag = false;


    void Start()
    {
        //取得
        rigid2D = GetComponent<Rigidbody2D>();
        //当たり判定取れるように
        specialAreaFlag = this.gameObject.AddComponent <SpecialAreaCollision>();
    }

    // Update is called once per frame
    void Update()
    {
        if(specialAreaFlag.GetAccelerationCollision()&&
            accelerationFlag==false)
        {//初めて加速する

            float nowVel = rigid2D.linearVelocityX * assetData.GetAccelerationMagnification();

            rigid2D.linearVelocity = new Vector2(nowVel,
                 rigid2D.linearVelocityY);

            accelerationFlag = true;
        }
        else 
        {

            accelerationFlag = false;
        }


        if (specialAreaFlag.GetLowGravityCollision())
        {
            float nowVel = rigid2D.linearVelocityY * assetData.GetLowGravityMagnification();

            rigid2D.linearVelocity = new Vector2(rigid2D.linearVelocityX,
                 nowVel );

            accelerationFlag = true;

            lowGravityFlag = true;
        }
        else if(lowGravityFlag)
        {//低重力エリア抜けました

            lowGravityFlag = false;
        }

    }

    public void SetScriptableObject(ScriptableObject_SpecialAreaData data)
    {
        assetData = data; 
    }
}
