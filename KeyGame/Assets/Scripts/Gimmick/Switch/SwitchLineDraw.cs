using System;
using System.Collections.Generic;
using UnityEngine;



public class SwitchLineDraw : MonoBehaviour
{

    List<GameObject> listData = new();

    LineRenderer line;

    //ラインの太さ
    float startWidth = 0.2f;
    float endWidth = 0.2f;

    Color lineColor = new Color(0f, 0f, 0f, 0.8f);

    //サインで求めた値を適応する量
    float sinLengh = 0.1f;

    float sinNum = 0f;

    //Sinで点滅ループさせるときの速さ
    float sinSpeed = 0.1f;

    public void Init(List<GameObject>list)
    {
        listData = list;

        //nullなら追加
        if (this.gameObject.GetComponent<LineRenderer>() == null)
        {
            line = this.gameObject.AddComponent<LineRenderer>();

            line.material = new Material(Shader.Find("Sprites/Default"));//デフォルトMaterial
        }
        line.sortingOrder = 100;   // 数字が大きいほど手前
        line.startWidth = startWidth;
        line.endWidth = endWidth;

        line.startColor = lineColor;
        line.endColor = lineColor;
    }



    // Update is called once per frame
    void Update()
    {
        //ラインなし
        if (listData.Count <= 1) return;

        //サイン用
        sinNum += sinSpeed;


        //ポイントの数を指定
        line.positionCount = listData.Count;

        //ラインの位置セット
        for (int i = 0;i < listData.Count; i++)
        {
            line.SetPosition(i, listData[i].transform.position);
        }


        //aでは、特定の範囲分だけ増減させる
        Color setColor = new Color(lineColor.r, lineColor.g, lineColor.b,
            lineColor.a + ((float)Math.Sin(sinNum)* sinLengh));

        //ラインカラー変更
        line.startColor = setColor;
        line.endColor = setColor;
    }
}
