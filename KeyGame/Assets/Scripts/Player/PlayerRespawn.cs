using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public sealed class PlayerRespawn : MonoBehaviour
{
    [Header("このY座標より下に落ちたらミス")]
    [SerializeField]
    private float missY = -10f;

    [Header("リスポーン地点")]
    [SerializeField]
    private Transform respawnPoint;

    [Header("演出を表示するCanvas")]
    [SerializeField]
    private Canvas targetCanvas;

    [Header("円形表示の最大半径")]
    [SerializeField]
    private float openRadius = 1.2f;

    [Header("ゲーム開始時の開く速さ")]
    [SerializeField]
    private float startOpenDuration = 0.8f;

    [Header("リスポーン時の開く速さ")]
    [SerializeField]
    private float respawnOpenDuration = 0.35f;

    [Header("ミス時の閉じる速さ")]
    [SerializeField]
    private float closeDuration = 0.25f;

    [Header("閉じきった後の待ち時間")]
    [SerializeField]
    private float holdClosedDuration = 0.05f;

    [Header("画面を覆う色")]
    [SerializeField]
    private Color overlayColor = Color.black;

    [Header("円のぼかし量")]
    [SerializeField]
    private float edgeSoftness = 0.02f;

    [Header("ミス演出時のズームサイズ")]
    [SerializeField]
    private float missZoomSize = 3.5f;

    [Header("通常追従のなめらかさ")]
    [SerializeField]
    private float cameraFollowSmoothTime = 0.05f;

    [Header("演出中のカメラ移動のなめらかさ")]
    [SerializeField]
    private float cameraFocusSmoothTime = 0.04f;

    [Header("復活後に通常追従へ戻るなめらかさ")]
    [SerializeField]
    private float cameraReturnToFollowSmoothTime = 0.08f;

    // プレイヤー本体の物理制御用
    private Rigidbody2D m_Rigidbody2D;

    // 左右反転の状態を戻すために使う
    private SpriteRenderer m_SpriteRenderer;

    // 演出で動かすメインカメラ
    private Camera m_MainCamera;

    // 画面全体に被せるUI画像
    private Image m_TransitionOverlay;

    // 円形に開閉する演出用マテリアル
    private Material m_TransitionMaterial;

    // 演出コルーチンを管理するために保持
    private Coroutine m_TransitionCoroutine;

    // ゲーム開始時のカメラサイズを保存しておく
    private float m_StartOrthographicSize;
    private Vector3 m_CameraFollowOffset;

    // 円の中心やカメラの注目先になるワールド座標
    private Vector3 m_FocusWorldPosition;

    // 初期位置・初期回転を保持しておく
    private Vector3 m_StartPosition;
    private Quaternion m_StartRotation;

    // 開始時の向きを戻すための値
    private bool m_StartFlipX;

    // リスポーン中の多重実行防止用
    private bool m_IsRespawning;
    private Vector3 m_CameraMoveVelocity;
    private bool m_IsReturningToFollow;

    private void Awake()
    {
        // 必要なコンポーネントを取得
        m_Rigidbody2D = GetComponent<Rigidbody2D>();
        m_SpriteRenderer = GetComponent<SpriteRenderer>();
        m_MainCamera = Camera.main;
    }

    private void Start()
    {
        // 開始時点の状態を保存
        m_StartPosition = transform.position;
        m_StartRotation = transform.rotation;

        if (m_MainCamera != null)
        {
            m_StartOrthographicSize = m_MainCamera.orthographicSize;
            m_CameraFollowOffset = m_MainCamera.transform.position - transform.position;
            m_CameraFollowOffset.x = 0f;
        }

        if (m_SpriteRenderer != null)
        {
            m_StartFlipX = m_SpriteRenderer.flipX;
        }

        // 画面演出用のオーバーレイを準備
        SetupTransitionOverlay();

        // 最初は完全に閉じた状態から始める
        SetHoleRadius(0f);
        SetCameraZoom(missZoomSize);

        // 最初はプレイヤー位置を中心にしておく
        m_FocusWorldPosition = transform.position;

        // シェーダーに渡す円の中心位置を更新
        UpdateHoleCenter();

        // ゲーム開始時の開く演出を再生
        m_TransitionCoroutine = StartCoroutine(PlayOpenTransition(startOpenDuration));
    }

    private void Update()
    {
        // 毎フレーム、円の中心位置を更新
        UpdateHoleCenter();

        // 一定より下に落ちたらミス扱い
        if (!m_IsRespawning && transform.position.y < missY)
        {
            TriggerMiss();
        }
    }

    private void LateUpdate()
    {
        if (m_IsRespawning)
        {
            return;
        }

        if (m_IsReturningToFollow)
        {
            SmoothReturnToFollow();
            return;
        }

        FollowPlayerWithCamera();
    }

    public void TriggerMiss()
    {
        // すでにリスポーン処理中なら何もしない
        if (m_IsRespawning)
        {
            return;
        }

        m_IsRespawning = true;
        m_CameraMoveVelocity = Vector3.zero;

        // ミスした瞬間の位置を演出の注目先にする
        m_FocusWorldPosition = transform.position;

        if (m_Rigidbody2D != null)
        {
            // 落下中の勢いを止めて、一時的に物理を止める
            m_Rigidbody2D.linearVelocity = Vector2.zero;
            m_Rigidbody2D.angularVelocity = 0f;
            m_Rigidbody2D.simulated = false;
        }

        // すでに別の演出中なら止める
        if (m_TransitionCoroutine != null)
        {
            StopCoroutine(m_TransitionCoroutine);
        }

        // 閉じる → リスポーン → 開く の流れを開始
        m_TransitionCoroutine = StartCoroutine(RespawnSequence());
    }

    private IEnumerator RespawnSequence()
    {
        // まず画面を閉じる
        yield return PlayCloseTransition(closeDuration);

        // 完全に閉じた状態を少しだけ見せる
        yield return new WaitForSeconds(holdClosedDuration);

        // リスポーン地点があればそこへ、なければ開始地点へ戻す
        Vector3 targetPosition = respawnPoint != null ? respawnPoint.position : m_StartPosition;
        Quaternion targetRotation = respawnPoint != null ? respawnPoint.rotation : m_StartRotation;

        transform.position = targetPosition;
        transform.rotation = targetRotation;
        m_FocusWorldPosition = targetPosition;

        if (m_SpriteRenderer != null)
        {
            // 向きも開始時の状態に戻す
            m_SpriteRenderer.flipX = m_StartFlipX;
        }

        if (m_Rigidbody2D != null)
        {
            // 物理状態をリセットして再開
            m_Rigidbody2D.linearVelocity = Vector2.zero;
            m_Rigidbody2D.angularVelocity = 0f;
            m_Rigidbody2D.simulated = true;
        }

        // 開く演出の前にカメラ位置と中心位置を合わせる
        SetCameraToFocusPosition();
        UpdateHoleCenter();

        // リスポーン後の開く演出
        yield return PlayOpenTransition(respawnOpenDuration);

        m_IsRespawning = false;
        m_TransitionCoroutine = null;
    }

    private void SetupTransitionOverlay()
    {
        // Canvasが未設定ならシーン内から探す
        if (targetCanvas == null)
        {
            targetCanvas = FindFirstObjectByType<Canvas>();
        }

        if (targetCanvas == null)
        {
            return;
        }

        // すでにオーバーレイがあるか確認
        Transform overlayTransform = targetCanvas.transform.Find("TransitionOverlay");

        if (overlayTransform == null)
        {
            // なければ全画面UIとして新規作成
            GameObject overlayObject = new GameObject("TransitionOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            overlayTransform = overlayObject.transform;
            overlayTransform.SetParent(targetCanvas.transform, false);

            RectTransform rectTransform = (RectTransform)overlayTransform;

            // 画面全体にぴったり広がるように設定
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        m_TransitionOverlay = overlayTransform.GetComponent<Image>();

        if (m_TransitionOverlay == null)
        {
            m_TransitionOverlay = overlayTransform.gameObject.AddComponent<Image>();
        }

        // 円形遷移用のシェーダーを取得
        Shader shader = Shader.Find("UI/CircleTransitionUI");

        if (shader == null)
        {
            Debug.LogError("UI/CircleTransitionUI shader was not found.");
            return;
        }

        // マテリアルを作って色やぼかし量を設定
        m_TransitionMaterial = new Material(shader);
        m_TransitionMaterial.SetColor("_Color", overlayColor);
        m_TransitionMaterial.SetFloat("_Softness", edgeSoftness);

        // UIに反映
        m_TransitionOverlay.material = m_TransitionMaterial;
        m_TransitionOverlay.color = Color.white;

        // 操作の邪魔をしないようにRaycastは切る
        m_TransitionOverlay.raycastTarget = false;

        // 一番手前に表示
        overlayTransform.SetAsLastSibling();
    }

    private void UpdateHoleCenter()
    {
        if (m_TransitionMaterial == null)
        {
            return;
        }

        if (m_MainCamera == null)
        {
            m_MainCamera = Camera.main;
        }

        if (m_MainCamera == null)
        {
            return;
        }

        // ワールド座標をViewport座標に変換してシェーダーへ渡す
        Vector3 viewportPosition = m_MainCamera.WorldToViewportPoint(m_FocusWorldPosition);
        m_TransitionMaterial.SetVector("_HoleCenter", new Vector4(viewportPosition.x, viewportPosition.y, 0f, 0f));
    }

    private void SetCameraToFocusPosition()
    {
        if (m_MainCamera == null)
        {
            m_MainCamera = Camera.main;
        }

        if (m_MainCamera == null)
        {
            return;
        }

        // カメラのZはそのままで、XとYだけ注目位置に合わせる
        Vector3 cameraPosition = m_MainCamera.transform.position;
        cameraPosition.x = m_FocusWorldPosition.x;
        cameraPosition.y = m_FocusWorldPosition.y;
        m_MainCamera.transform.position = cameraPosition;
    }

    private void MoveCameraToFocus()
    {
        MoveCameraToPosition(GetFocusCameraPosition(), cameraFocusSmoothTime);
    }

    private void FollowPlayerWithCamera()
    {
        if (m_MainCamera == null)
        {
            m_MainCamera = Camera.main;
        }

        if (m_MainCamera == null)
        {
            return;
        }

        m_MainCamera.transform.position = GetFollowCameraPosition();
    }

    private void SmoothReturnToFollow()
    {
        if (m_MainCamera == null)
        {
            m_MainCamera = Camera.main;
        }

        if (m_MainCamera == null)
        {
            return;
        }

        Vector3 targetPosition = GetFollowCameraPosition();
        MoveCameraToPosition(targetPosition, cameraReturnToFollowSmoothTime);

        if ((m_MainCamera.transform.position - targetPosition).sqrMagnitude <= 0.0001f)
        {
            m_MainCamera.transform.position = targetPosition;
            m_CameraMoveVelocity = Vector3.zero;
            m_IsReturningToFollow = false;
        }
    }

    private Vector3 GetFocusCameraPosition()
    {
        float z = m_MainCamera != null ? m_MainCamera.transform.position.z : 0f;
        return new Vector3(m_FocusWorldPosition.x, m_FocusWorldPosition.y, z);
    }

    private Vector3 GetFollowCameraPosition()
    {
        float z = m_MainCamera != null ? m_MainCamera.transform.position.z : 0f;
        return new Vector3(
            transform.position.x + m_CameraFollowOffset.x,
            transform.position.y + m_CameraFollowOffset.y,
            z
        );
    }

    private void MoveCameraToPosition(Vector3 targetPosition, float smoothTime)
    {
        if (m_MainCamera == null)
        {
            m_MainCamera = Camera.main;
        }

        if (m_MainCamera == null)
        {
            return;
        }

        m_MainCamera.transform.position = Vector3.SmoothDamp(
            m_MainCamera.transform.position,
            targetPosition,
            ref m_CameraMoveVelocity,
            Mathf.Max(0.0001f, smoothTime),
            Mathf.Infinity,
            Time.unscaledDeltaTime
        );
    }

    private void SetCameraZoom(float orthographicSize)
    {
        if (m_MainCamera == null)
        {
            m_MainCamera = Camera.main;
        }

        if (m_MainCamera == null)
        {
            return;
        }

        m_MainCamera.orthographicSize = orthographicSize;
    }

    private void SetHoleRadius(float radius)
    {
        if (m_TransitionMaterial == null)
        {
            return;
        }

        // 円の半径をシェーダーに反映
        m_TransitionMaterial.SetFloat("_HoleRadius", radius);
    }

    private IEnumerator PlayOpenTransition(float duration)
    {
        if (m_TransitionMaterial == null)
        {
            yield break;
        }

        float elapsed = 0f;

        // 半径を0から広げつつ、カメラを通常サイズへ戻す
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;

            Vector3 targetCameraPosition = Vector3.Lerp(GetFocusCameraPosition(), GetFollowCameraPosition(), t);
            MoveCameraToPosition(targetCameraPosition, cameraFocusSmoothTime);
            SetCameraZoom(Mathf.Lerp(missZoomSize, m_StartOrthographicSize, t));
            SetHoleRadius(Mathf.Lerp(0f, openRadius, t));
            UpdateHoleCenter();
            yield return null;
        }

        SetCameraZoom(m_StartOrthographicSize);
        SetHoleRadius(openRadius);
        m_CameraMoveVelocity = Vector3.zero;
        m_IsReturningToFollow = true;
    }

    private IEnumerator PlayCloseTransition(float duration)
    {
        if (m_TransitionMaterial == null)
        {
            yield break;
        }

        float elapsed = 0f;

        // カメラ位置は変えずにズームインしつつ、円を閉じていく
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;

            MoveCameraToFocus();
            SetCameraZoom(Mathf.Lerp(m_StartOrthographicSize, missZoomSize, t));
            SetHoleRadius(Mathf.Lerp(openRadius, 0f, t));
            UpdateHoleCenter();
            yield return null;
        }

        SetCameraToFocusPosition();
        SetCameraZoom(missZoomSize);
        SetHoleRadius(0f);
    }
}
