using Unity.VisualScripting;
using UnityEngine;

public class SwitchDownUpdate : MonoBehaviour
{
    /*
    スイッチが押されたときに、ボタン部分が押されていることを分かりやすくする
    演出の処理
    */

    
    //スイッチの当たり判定
    [SerializeField] SwitchCollision collision;
    [Header("ボタン押されたときの位置")]
    [SerializeField] Transform onTransform;

    //差分
    Vector3 diffPos;//

    void Start()
    {
        //ボタンと本体との差分を求める
        diffPos = transform.position - onTransform.position;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if(collision.GetCollisionFlag())
        {//ボタン押されている！

            //長いので変数名短縮
            var pos = onTransform.position;
            
            //ボタン部分より、土台部分（自身）の方が大きいので、
            //ボタンを自身の中心点へ移動することで、
            //押されたことを分かりやすくする
            transform.position = new Vector3(pos.x, pos.y, pos.z);
        }
        else
        {//押されていない

            //ボタン部分の初期位置に移動させる
            transform.position = onTransform.position + diffPos;
        }
    }
}
