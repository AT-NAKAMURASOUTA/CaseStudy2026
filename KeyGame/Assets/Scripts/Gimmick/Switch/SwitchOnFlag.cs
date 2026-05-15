using System;
using System.Linq;
using UnityEngine;

public class SwitchOnFlag : MonoBehaviour
{
    /*
    アタッチしたゲームオブジェクト（Script＿SwitchCollision）。
    これから、ボタンが押されているかのフラグを取得し、
    アタッチされたボタンの最大数と、押された数を計測する
    */




    //Inspectorで配列アタッチできるように構造体化
    [System.Serializable]
    public struct SwitchReference
    {
        public GameObject switchReference;
    }

    //対応しているスイッチオブジェクトのデータをセットするもの
    [SerializeField] SwitchReference[] switchObject;
    

    //「最大数==現在押されている数」で、全てのボタンが起動している
    public struct SwitchOnData
    {
        public int maxSize;//対応しているボタンの最大数
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

            if (switchObject[i].switchReference == null)
            {//配列にNullがある！

#if UNITY_EDITOR
                Debug.Log("-ゲームオブジェクトのアタッチが外れている可能性があります-");
#endif
                //Nullなので、
                data.maxSize--;
                break;
            }

            //ヒット用のスクリプト取得
            SwitchCollision script = switchObject[i].switchReference.GetComponent<SwitchCollision>();

            if(script == null)
            {//スイッチCollisionのスクリプトがない！

#if UNITY_EDITOR
                Debug.Log("-アタッチされているゲームオブジェクトに「SwitchCollision」スクリプトありません。" +
                    "アタッチしているオブジェクト間違っていませんか？-");
#endif
                //Nullなので、最大数は減らしとく
                data.maxSize--;
                continue;
            }

            //当たり判定当たっていたら、カウント
            data.nowOnCount += script.GetCollisionFlag() ? 1 : 0;
        }

        return data;


    }
}
