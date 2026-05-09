using UnityEngine;

public class Switch_NormalDoorUpdate : MonoBehaviour
{
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

        if (onData.nowOnCount == onData.maxSize)
        {
            //削除
            Destroy(gameObject);
        }
    }
}
