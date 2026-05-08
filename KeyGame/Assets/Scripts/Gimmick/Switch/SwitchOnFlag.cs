using System;
using System.Linq;
using UnityEngine;

public class SwitchOnFlag : MonoBehaviour
{

    [System.Serializable]
    public struct SwitchReference
    {
        public GameObject switchReference;
    }

    //対応しているスイッチオブジェクトのデータをセットするもの
    [SerializeField] SwitchReference[] switchObject;
    

    //スイッチにが起動しているもの
    public struct SwitchOnData
    {
        public int maxSize;//対応しているボタンの数
        public int nowOnCount;//現在押されている数
    }

    //何個対応しているスイッチ押されているか返す
    public SwitchOnData GetSwichOnData()
    {
        SwitchOnData data = new();

        //最大数取得
        data.maxSize = switchObject.Count();

        for(int i = 0;i < data.maxSize;i++)
        {
            //ヒット用のスクリプト取得
            SwitchCollision script = switchObject[i].switchReference.GetComponent<SwitchCollision>();

            //当たり判定当たっていたら、カウント
            data.nowOnCount += script.GetCollisionFlag() ? 1 : 0;
        }

        return data;


    }
}
