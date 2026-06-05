using System.Collections.Generic;
using UnityEngine;

public class SwitchLineCreate : MonoBehaviour
{
    //Inspectorで配列アタッチできるように構造体化
    [System.Serializable]
    private struct ObjectReference
    {
        public GameObject objectReference;
    }

    //対応しているスイッチオブジェクトのデータをセットするもの
    [Header("ラインを引く場所")]
    [SerializeField] ObjectReference[] objectReference;

    void Awake()
    {
        List<GameObject> list = new();

        for (int i = 0; i < objectReference.Length; i++)
        {
            //オブジェクトデータ投入
            list.Add(objectReference[i].objectReference);
        }

        //ライン用に新規作成
        GameObject obj = new GameObject("Line");
        var script = obj.gameObject.AddComponent<SwitchLineDraw>();
        script.Init(list);//初期化

        //最初しか使わないのでスクリプト削除
        Destroy(this);
    }

}
