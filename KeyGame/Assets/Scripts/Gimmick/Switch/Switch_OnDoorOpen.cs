using UnityEngine;

public class Switch_OnDoorOpen : MonoBehaviour
{
    //スイッチの当たり判定
    SwitchOnFlag collisionData;

    Vector3 startPos;
    [Header("最終目標地点")]
    [SerializeField]Transform endPos;
    [Header("何フレームで移動するか")]
    [SerializeField] float moveMaxCount = 20;

    int nowCount = 0;

    void Start()
    {
        collisionData = GetComponent<SwitchOnFlag>();

        //初期位置
        startPos = new Vector3(transform.position.x,
            transform.position.y,
            transform.position.z);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        SwitchOnFlag.SwitchOnData onData = collisionData.GetSwichOnData();

        //判定する必要ないのでリターン
        if (onData.maxSize == 0) return;

        Debug.Log(nowCount);

        if (onData.nowOnCount == onData.maxSize)
        {//全てのスイッチ押されている！

            //最大まで移動したら終了
            if (nowCount == moveMaxCount) return;

            nowCount++;

            DoorMove();
        }
        else
        {
            //全部閉まり切ったので、終了
            if (nowCount == 0) return;


            nowCount--;

            DoorMove();
        }
    }

    //ドアの移動処理
    private void DoorMove()
    {
        //スタートと終了を結ぶ位置
        Vector3 vec = endPos.position - startPos;

        //カウントから、どれぐらいの位置にいるか求める
        Vector3 moveVec = vec * (nowCount / moveMaxCount);
        moveVec.z = startPos.z;//Z位置固定

        //現在位置とからの位置
        Vector3 nowPos = startPos + moveVec;
        //位置更新
        transform.position = nowPos;
    }
}
