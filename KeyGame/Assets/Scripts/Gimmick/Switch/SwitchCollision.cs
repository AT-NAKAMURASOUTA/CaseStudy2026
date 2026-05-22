using UnityEngine;

public class SwitchCollision : MonoBehaviour
{
    /*
    スイッチの当たり判定処理
    */

    enum ButtonType
    {
        ONE_PUSH,//１回しか押せない
        INFINITE,//無限に押せる
    }

    //ボタンタイプ
    [SerializeField]
    ButtonType type;

    int hitFlag = 0;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Player" ||
            collision.gameObject.tag == "AlphabetTag")
        {//Player、アルファベットならtrueに
            hitFlag++;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        switch (type)
        {
            case ButtonType.ONE_PUSH://1回しかおさない時は無視
                return;
        }

        //無限に押せるボタンの処理

        if (collision.gameObject.tag == "Player" ||
            collision.gameObject.tag == "AlphabetTag")
        {//出て行ったら、減らす
            hitFlag--;
        }
    }




    //スイッチ押された判定を返す
    public bool GetCollisionFlag()
    {
        return hitFlag == 0 ? false : true;
    }
}
