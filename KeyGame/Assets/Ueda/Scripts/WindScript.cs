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
    private float windDirection = 1.0f;

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

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 風の影響を受けるTagに該当するか確認
        if (activeTags.Contains(collision.gameObject.tag))
        {
            if (!affectedObjects.Contains(collision.gameObject))
            {
                affectedObjects.Add(collision.gameObject);
                //オブジェクトごとにそれぞれ風を適応
                if (collision.TryGetComponent<PlayerMove>(out PlayerMove playerMove))
                {
                    float windStrength = tagWindStrengths[collision.tag];
                    playerMove.InWindArea(windStrength * windDirection);
                }
                if (collision.TryGetComponent<AlphabetRigidbody>(out AlphabetRigidbody alphabetRigidbody))
                {
                    float windStrength = tagWindStrengths[collision.tag];
                    alphabetRigidbody.InWindArea(windStrength * windDirection);
                }
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (affectedObjects.Contains(collision.gameObject))
        {
            affectedObjects.Remove(collision.gameObject);
            //オブジェクトごとにそれぞれ風の影響を解除
            if (collision.TryGetComponent<PlayerMove>(out PlayerMove playerMove))
            {
                playerMove.ExitWindArea();
            }
            if (collision.TryGetComponent<AlphabetRigidbody>(out AlphabetRigidbody alphabetRigidbody))
            {
                alphabetRigidbody.ExitWindArea();
            }
        }
    }
}
