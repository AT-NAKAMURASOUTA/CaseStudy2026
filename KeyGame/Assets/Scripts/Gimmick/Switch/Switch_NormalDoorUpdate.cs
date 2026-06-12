using UnityEngine;

public class Switch_NormalDoorUpdate : MonoBehaviour
{

    /*
    １回すべてのボタンが押されたら永遠に扉が開く「扉Object」の処理

    2026/6/12
    スイッチが２つあり、その中でどちらかが押されていたら扉を消せるようにする処理を追加
    */




    //スイッチの当たり判定
    [Header("ここは、特定のグループが押されているかを判別したい時にアタッチ。")]
    [SerializeField]SwitchOnFlag collisionData;

    void Start()
    {
        if(collisionData == null)
        {
            collisionData = GetComponent<SwitchOnFlag>();
        }
        
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
