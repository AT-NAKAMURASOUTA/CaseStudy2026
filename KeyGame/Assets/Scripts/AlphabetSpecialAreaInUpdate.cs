using UnityEngine;

public class AlphabetSpecialAreaInUpdate : MonoBehaviour
{
    /*
    アルファベットが、特殊エリア入ったときの処理
    
    アルファベットの挙動に応じて作成しました。
    */


    //速度の調整

   Rigidbody2D rigid2D;

    ScriptableObject_SpecialAreaData assetData;

    SpecialAreaCollision specialAreaFlag;

    //特殊エリアの影響を計算するスクリプト
    SpecialAreaVelocityUpdate specialAreaVelocityUpdate;
    
    //加速度の更新
    bool accelerationFlag = false;


    void Start()
    {
        //取得
        rigid2D = GetComponent<Rigidbody2D>();
        //当たり判定取れるように
        specialAreaFlag = this.gameObject.AddComponent <SpecialAreaCollision>();

        //作成
        specialAreaVelocityUpdate = new();
    }

    // Update is called once per frame
    void Update()
    {

        //加速エリア入った瞬間のみ、適応
        //理由、アルファベットにずっと速度かけると高速で吹っ飛んでしまうから

        if (specialAreaFlag.GetAccelerationCollision())
        {//加速エリアにいる！

            if (accelerationFlag == false)
            {//初めてなら加速する

                float velocityX = specialAreaVelocityUpdate.AccelerationUpdate(
            rigid2D.linearVelocity,
            specialAreaFlag,
            assetData).x;

                rigid2D.linearVelocity = new Vector2(velocityX,
                     rigid2D.linearVelocityY);

                accelerationFlag = true;
            }

        }
        else
        {

            accelerationFlag = false;
        }


        //********************************************
        //低重力の処理

        //落下速度の更新
        float velocityY = specialAreaVelocityUpdate.LowGravityUpdate(
            rigid2D.linearVelocity,
            specialAreaFlag,
            assetData).y;


        rigid2D.linearVelocity = new Vector2(rigid2D.linearVelocityX,
              velocityY);


    }
    public void SetScriptableObject(ScriptableObject_SpecialAreaData data)
    {
        assetData = data; 
    }

    public ScriptableObject_SpecialAreaData GetScriptableObject()
    {
        return assetData;
    }
}
