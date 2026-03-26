using UnityEngine;

public class DestroyOnFall : MonoBehaviour
{
    [Header("このY座標より下に落ちたら削除")]
    [SerializeField]
    private float destroyY = -10f;

    public void SetDestroyY(float value)
    {
        destroyY = value;
    }

    void Update()
    {
        if (transform.position.y < destroyY)
        {
            Destroy(gameObject);
        }
    }
}
