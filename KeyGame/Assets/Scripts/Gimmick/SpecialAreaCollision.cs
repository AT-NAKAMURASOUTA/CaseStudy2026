using UnityEngine;

public class SpecialAreaCollision : MonoBehaviour
{

    void OnTriggerStay2D(Collider2D collision)
    {

        Debug.Log("エリア内！");
        
    }
}
