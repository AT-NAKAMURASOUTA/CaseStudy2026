using UnityEngine;

public class SwitchCollision : MonoBehaviour
{

    int hitFlag = 0;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.tag == "Player"||
            collision.gameObject.tag == "AlphabetTag")
        {//Player、アルファベットならtrueに
            hitFlag++;
        }
        
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Player" ||
            collision.gameObject.tag == "AlphabetTag")
        {//出て行ったら、減らす
            hitFlag--;
        }
    }



    //スイッチ押された判定を返す
    public bool GetCollisionFlag()
    {
        return hitFlag == 0 ? false:true;
    }
}
