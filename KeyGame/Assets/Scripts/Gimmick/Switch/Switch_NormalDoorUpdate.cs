using UnityEngine;

public class Switch_NormalDoorUpdate : MonoBehaviour
{

    /*
    １回すべてのボタンが押されたら永遠に扉が開く「扉Object」の処理
    */




    //スイッチの当たり判定
    SwitchOnFlag collisionData;

    void Start()
    {
        collisionData = GetComponent<SwitchOnFlag>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        SwitchOnFlag.SwitchOnData onData = collisionData.GetSwichOnData();

        //判定する必要ないのでリターン
        if (onData.maxSize == 0) return;


        //全てのボタンが押された！
        if (onData.nowOnCount == onData.maxSize)
        {
            //削除
            Destroy(gameObject);
        }
    }
}
