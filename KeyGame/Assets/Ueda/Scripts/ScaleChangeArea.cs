using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class ScaleChangeArea : MonoBehaviour
{
    [Header("TargetTag")]
    [SerializeField]
    private string targetTag = "AlphabetTag";

    [Header("倍率")]
    [SerializeField]
    private float scaleChangeRate = 2.0f;

    [Header("何秒かけて大きくするか")]
    [SerializeField]
    private float changeTime = 1.0f;

    public List<GameObject> usedList = new List<GameObject>();

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag(targetTag))
        {
            usedList.RemoveAll(obj => obj == null);
            if (usedList.Any(obj => obj == collision.gameObject))
            {
                return;
            }
            //collision.transform.localScale *= scaleChangeRate;
            StartCoroutine(ScaleLerp(collision.gameObject));
            usedList.Add(collision.gameObject);
        }
    }

    public IEnumerator ScaleLerp(GameObject obj)
    {
        Vector3 baseScale = obj.transform.localScale;
        Vector3 endScale = baseScale * scaleChangeRate;

        float elapsedTime = 0.0f;

        while (elapsedTime < changeTime)
        {
            if (obj == null)
            {
                elapsedTime = 0.0f;
                yield break;
            }

            var currentScale = Vector3.Lerp(baseScale, endScale, elapsedTime / changeTime);

            Debug.Log("scaleRate : " + elapsedTime / changeTime);

            obj.transform.localScale = currentScale;

            elapsedTime += Time.deltaTime;

            yield return null;
        }

        if (obj != null)
        obj.transform.localScale = endScale; 

    }
}
