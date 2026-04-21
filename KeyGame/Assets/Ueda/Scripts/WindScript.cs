using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

public class WindScript : MonoBehaviour
{
    [Header("風の影響を受けるTagを設定")]
    [SerializeField]
    private List<string> activeTags = new List<string>();

    public List<GameObject> affectedObjects = new List<GameObject>();

    [Header("風の方向を設定")]
    [SerializeField]
    private Vector2 windDirection = Vector2.right;

    [Serializable]
    private struct TagWindStrength
    {
        public string tag;
        public float strength;
    }

    [Header("Tagごとの風の強さを設定")]
    [SerializeField]
    private List<TagWindStrength> tagWindStrengthList = new List<TagWindStrength>();
    private Dictionary<string, float> tagWindStrengths = new Dictionary<string, float>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Tagごとの風の強さを辞書に変換
        foreach (TagWindStrength t in tagWindStrengthList)
        {
            tagWindStrengths[t.tag] = t.strength;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (affectedObjects.Count > 0)
        {
            foreach (GameObject obj in affectedObjects)
            {
                if (obj != null)
                {
                    Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
                    if (rb != null)
                    {
                        //rb.AddForce(windDirection.normalized * windStrength, ForceMode2D.Force);

                        // Tagごとの風の強さを適用
                        if (tagWindStrengths.ContainsKey(obj.tag))
                        {
                            float tagStrength = tagWindStrengths[obj.tag];
                            rb.AddForce(windDirection.normalized * tagStrength, ForceMode2D.Force);
                        }
                    }
                }
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (activeTags.Contains(collision.gameObject.tag))
        {
            if (!affectedObjects.Contains(collision.gameObject))
            {
                affectedObjects.Add(collision.gameObject);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (affectedObjects.Contains(collision.gameObject))
        {
            affectedObjects.Remove(collision.gameObject);
        }
    }
}
