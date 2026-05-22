using System.Drawing;
using UnityEngine;

public class DebugAnimation : MonoBehaviour
{
    float t = 0;
    RectTransform rectTransform;

    private void Start()
    {
        // RectTransformを取得
        rectTransform = GetComponent<RectTransform>();

        // 初期位置を取得
        Vector3 initialPosition = rectTransform.localPosition;
        // 左中央を基準点にする
        rectTransform.pivot = new Vector2(0f, 0.5f);

        // 初期位置を左中央に設定
        rectTransform.localPosition = new Vector3(initialPosition.x - rectTransform.rect.width / 2, initialPosition.y, initialPosition.z);
    }

    void Update()
    {
        t += Time.deltaTime;

        float cycle = Mathf.PingPong(t * 0.5f, 1f);
        float angle = Mathf.SmoothStep(0, 180, cycle);

        rectTransform.localRotation = Quaternion.Euler(0, angle, 0);
    }
}
