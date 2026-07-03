using UnityEngine;

public class FindMainCamera_SetCanvas : MonoBehaviour
{
    private void Awake()
    {
        Canvas canvas = GetComponent<Canvas>();

        Camera uiCamera = Camera.main;

        // カメラモードを「Screen Space - Camera」に
        canvas.renderMode = RenderMode.ScreenSpaceCamera;

        // このキャンバスが使うカメラを指定
        canvas.worldCamera = uiCamera;

        //スクリプト削除
        Destroy(this);
    }
}
