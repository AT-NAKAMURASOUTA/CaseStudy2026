using UnityEngine;

public class Switch_OnDoorOpen : MonoBehaviour
{

    /*
    全てのボタンが押されている時だけ、扉が開く処理

    -全て押されている（扉が開く）
    -１つでも押されていないボタンがある（初期位置に戻る、扉が閉まっていく処理）
    */

    //スイッチの当たり判定
    SwitchOnFlag collisionData;

    //初期位置
    Vector3 startPos;

    [Header("最終目標地点")]
    [SerializeField]Transform endPos;
    [Header("何フレームで移動するか")]
    [SerializeField] float moveMaxCount = 20;

    int nowCount = 0;

    void Start()
    {
        //スイッチの当たり判定
        collisionData = GetComponent<SwitchOnFlag>();

        //初期位置
        startPos = new Vector3(transform.position.x,
            transform.position.y,
            transform.position.z);
    }


    void FixedUpdate()
    {
        SwitchOnFlag.SwitchOnData onData = collisionData.GetSwichOnData();

        //対応するスイッチ無いので、判定する必要ないのでリターン
        if (onData.maxSize == 0) return;


        
        if (onData.nowOnCount == onData.maxSize)
        {//全てのスイッチ押されている！

            //最大まで移動したら終了
            if (nowCount == moveMaxCount) return;

            nowCount++;//移動カウント

            //ドアが開いていく処理
            DoorMove();
        }
        else
        {
            //全部閉まり切ったので、終了
            if (nowCount == 0) return;


            nowCount--;//移動カウント

            //ドアが閉まっていく処理
            DoorMove();
        }
    }


    //ドアの移動処理
    private void DoorMove()
    {
        //スタートと終了を結ぶ線
        Vector3 vec = endPos.position - startPos;

        //**************************************
        //どれぐらいの位置に今いるか求める

        //（現在のカウント/最大カウント）で、線に対してどの位置にいるか割合を求める
        float rate = (nowCount / moveMaxCount);
        //割合を使って位置を計算
        Vector3 moveVec = vec * rate;
        moveVec.z = startPos.z;//Z位置固定

        //スタート位置+移動量で、現在位置を求める
        Vector3 nowPos = startPos + moveVec;
        
        //位置更新
        transform.position = nowPos;
    }
}
