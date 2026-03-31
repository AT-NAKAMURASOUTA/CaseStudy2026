using UnityEngine;

public class SwitchCollision : MonoBehaviour
{
    private void OnCollisionStay2D(Collision2D collision)
    {
        if(collision.gameObject.tag == "Player")
        {
            Debug.Log("a");
        }
    }
}
