using UnityEngine;

public class AlphabetSpecialAreaInUpdate : MonoBehaviour
{
    //速度の調整

   Rigidbody2D rigid2D;

    ScriptableObject_SpecialAreaData assetData;

    SpecialAreaCollision specialAreaFlag;
    
    //加速度の更新
    bool accelerationFlag = false;


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
        if (specialAreaFlag.GetAccelerationCollision())
        {//加速エリアにいる！

            if (accelerationFlag == false)
            {//初めてなら加速する

                float nowVel = rigid2D.linearVelocityX * assetData.GetAccelerationMagnification();

                rigid2D.linearVelocity = new Vector2(nowVel,
                     rigid2D.linearVelocityY);

                accelerationFlag = true;
            }

        }
        else
        {

            accelerationFlag = false;
        }


        if (specialAreaFlag.GetLowGravityCollision())
        {

            //速度計算
            float nowVel = rigid2D.linearVelocityY;


            nowVel *=
                assetData.GetLowGravityMagnification();
            rigid2D.linearVelocity = new Vector2(rigid2D.linearVelocityX,
                 nowVel);


        }
    }
    public void SetScriptableObject(ScriptableObject_SpecialAreaData data)
    {
        assetData = data; 
    }
}
