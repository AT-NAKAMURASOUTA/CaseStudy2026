using UnityEngine;

public class AlphabetCheck : MonoBehaviour
{
    [Header("探しているスプライト（アルファベット）")]
    [SerializeField]
    private Sprite searchSprite;

    bool isFound = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        isFound = false;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        //if (!isFound)
        {
            if (collision.TryGetComponent<SpriteRenderer>(out SpriteRenderer s))
            {
                if (s.sprite == searchSprite)
                {
                    isFound = true;

                    Debug.Log("Target is Find!!!!");
                }
            }
        }
    }
}
