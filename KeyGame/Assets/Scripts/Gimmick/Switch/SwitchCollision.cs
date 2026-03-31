using UnityEngine;

public class SwitchCollision : MonoBehaviour
{
    bool hitFlag = false;

    private void OnCollisionStay2D(Collision2D collision)
    {
        if(collision.gameObject.tag == "Player"||
            collision.gameObject.tag == "AlphabetTag")
        {//Player、アルファベットならtrueに
            hitFlag = true;
        }
        
    }

    //スイッチ押された判定を返す
    public bool GetCollisionFlag()
    {
        return hitFlag;
    }
}
