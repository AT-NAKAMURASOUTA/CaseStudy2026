using UnityEngine;

public class SwitchCollision : MonoBehaviour
{
    bool hitFlag = false;

    private void OnCollisionStay2D(Collision2D collision)
    {
        if(collision.gameObject.tag == "Player")
        {
            hitFlag = true;
        }
    }

    //スイッチ押された判定を返す
    public bool GetCollisionFlag()
    {
        return hitFlag;
    }
}
