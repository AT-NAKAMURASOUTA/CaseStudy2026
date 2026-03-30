using UnityEngine;

public class DestroyOnFall : MonoBehaviour
{
    [Header("このY座標より下に落ちたら消える")]
    [SerializeField]
    private float destroyY = -10f;

    private GenerateAlphabet owner;

    public void SetDestroyY(float value)
    {
        destroyY = value;
    }

    public void SetOwner(GenerateAlphabet value)
    {
        owner = value;
    }

    void Update()
    {
        if (transform.position.y < destroyY)
        {
            owner?.NotifyAlphabetDestroyed();
            Destroy(gameObject);
        }
    }
}
