using Cysharp.Threading.Tasks;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public static class GoalClearEffectPlayer
{
    private const float ClearTextRiseDuration = 1.65f;
    private const float ClearTextHoldDuration = 1.0f;
    private const float FadeDuration = 0.7f;
    private const float ClearTextY = 28f;

    public static async UniTask PlayAsync(Transform goalTransform, CancellationToken token)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        Camera camera = Camera.main;
        GoalClearUi ui = CreateUi();
        GoalClearWorldFx worldFx = CreateClearTextFx(camera);

        StopPlayer(player);

        Vector3 cameraStartPosition = camera != null ? camera.transform.position : Vector3.zero;
        float cameraStartSize = camera != null ? camera.orthographicSize : 0f;
        Vector3 cameraTargetPosition = GetCameraTargetPosition(camera, player, goalTransform);
        float cameraTargetSize = camera != null ? Mathf.Max(2.7f, camera.orthographicSize * 0.54f) : 0f;

        float elapsed = 0f;
        while (elapsed < ClearTextRiseDuration)
        {
            token.ThrowIfCancellationRequested();
            elapsed += Time.deltaTime;
            float rawT = Mathf.Clamp01(elapsed / ClearTextRiseDuration);
            float cameraT = EaseOutCubic(Mathf.Clamp01(rawT * 1.15f));
            float textT = EaseOutCubic(rawT);
            float clearScale = Mathf.Lerp(0.52f, 1.34f, textT);

            if (camera != null)
            {
                camera.transform.position = Vector3.Lerp(cameraStartPosition, cameraTargetPosition, cameraT);
                camera.orthographicSize = Mathf.Lerp(cameraStartSize, cameraTargetSize, cameraT);
            }

            ui.SetClearText(Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(rawT * 1.1f)), ClearTextY, clearScale);
            if (rawT >= 0.08f)
            {
                worldFx.SetViewportY(camera, GetClearTextViewportY(ClearTextY));
                worldFx.Play();
            }

            await UniTask.Yield(token);
        }

        elapsed = 0f;
        while (elapsed < ClearTextHoldDuration)
        {
            token.ThrowIfCancellationRequested();
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / ClearTextHoldDuration);
            float floatT = 1f - Mathf.Pow(1f - t, 2f);
            float pulse = Mathf.Sin(t * Mathf.PI) * 0.035f;

            ui.SetClearText(1f, ClearTextY, 1.34f + pulse);
            await UniTask.Yield(token);
        }

        elapsed = 0f;
        while (elapsed < FadeDuration)
        {
            token.ThrowIfCancellationRequested();
            elapsed += Time.deltaTime;
            float t = EaseInOut(Mathf.Clamp01(elapsed / FadeDuration));
            ui.SetFade(t);
            ui.SetClearText(1f - t * 0.35f, ClearTextY, 1.3f);
            await UniTask.Yield(token);
        }

        ui.SetFade(1f);
    }

    private static void StopPlayer(GameObject player)
    {
        if (player == null)
        {
            return;
        }

        if (player.TryGetComponent(out PlayerInput input))
        {
            input.DeactivateInput();
        }

        if (player.TryGetComponent(out PlayerMove playerMove))
        {
            playerMove.enabled = false;
        }

        if (player.TryGetComponent(out AlphabetThrowController throwController))
        {
            throwController.enabled = false;
        }

        if (player.TryGetComponent(out PlayerRespawn respawn))
        {
            respawn.enabled = false;
        }

        if (player.TryGetComponent(out Rigidbody2D rigidbody2D))
        {
            rigidbody2D.linearVelocity = Vector2.zero;
            rigidbody2D.angularVelocity = 0f;
        }

        if (player.TryGetComponent(out Animator animator))
        {
            animator.SetBool("isWalking", false);
            animator.SetBool("isJumping", false);
        }
    }

    private static Vector3 GetCameraTargetPosition(Camera camera, GameObject player, Transform goalTransform)
    {
        if (camera == null)
        {
            return Vector3.zero;
        }

        Vector3 focus = goalTransform != null ? goalTransform.position : camera.transform.position;
        if (player != null)
        {
            focus = Vector3.Lerp(player.transform.position, focus, 0.25f);
        }

        return new Vector3(focus.x, focus.y + 0.9f, camera.transform.position.z);
    }

    private static GoalClearUi CreateUi()
    {
        GameObject canvasObject = new GameObject("GoalClearEffectCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10000;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform root = canvasObject.GetComponent<RectTransform>();
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        TextMeshProUGUI clearText = CreateClearText(root);
        Image fadeImage = CreateImage(root, "Fade", new Color(0f, 0f, 0f, 0f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        fadeImage.raycastTarget = false;

        return new GoalClearUi(clearText, fadeImage);
    }

    private static GoalClearWorldFx CreateClearTextFx(Camera camera)
    {
        Vector3 position = Vector3.zero;
        float textWorldWidth = 6f;
        float textWorldHeight = 1.5f;
        if (camera != null)
        {
            float distance = Mathf.Abs(camera.transform.position.z);
            position = camera.ViewportToWorldPoint(new Vector3(0.5f, GetClearTextViewportY(ClearTextY), distance));

            float worldHeight = camera.orthographic ? camera.orthographicSize * 2f : 10f;
            float worldWidth = worldHeight * camera.aspect;
            textWorldWidth = worldWidth * 0.62f;
            textWorldHeight = worldHeight * 0.23f;
        }

        GameObject root = new GameObject("GoalClearWorldFx");
        root.transform.position = new Vector3(position.x, position.y, 0f);

        Material particleMaterial = new Material(Shader.Find("Sprites/Default"));
        return new GoalClearWorldFx(
            root.transform,
            CreateBurst(root.transform, "ClearCoreFlash", particleMaterial, textWorldWidth, textWorldHeight),
            CreateSparkBurst(root.transform, "ClearSparkBurst", particleMaterial, textWorldWidth, textWorldHeight),
            CreateRisingStream(root.transform, "ClearRisingStream", particleMaterial, textWorldWidth, textWorldHeight),
            CreateEdgeStream(root.transform, "ClearTopEdgeStream", particleMaterial, new Vector3(0f, textWorldHeight * 0.48f, 0f), new Vector3(textWorldWidth, 0.24f, 0.1f)),
            CreateEdgeStream(root.transform, "ClearBottomEdgeStream", particleMaterial, new Vector3(0f, -textWorldHeight * 0.48f, 0f), new Vector3(textWorldWidth, 0.22f, 0.1f)),
            CreateEdgeStream(root.transform, "ClearLeftEdgeStream", particleMaterial, new Vector3(-textWorldWidth * 0.5f, 0f, 0f), new Vector3(0.24f, textWorldHeight, 0.1f)),
            CreateEdgeStream(root.transform, "ClearRightEdgeStream", particleMaterial, new Vector3(textWorldWidth * 0.5f, 0f, 0f), new Vector3(0.24f, textWorldHeight, 0.1f)),
            CreateRing(root.transform, "ClearRing", particleMaterial, textWorldWidth, textWorldHeight)
        );
    }

    private static float GetClearTextViewportY(float clearTextY)
    {
        return 0.66f + clearTextY / 1080f;
    }

    private static ParticleSystem CreateBurst(Transform parent, string name, Material material, float width, float height)
    {
        ParticleSystem ps = CreateParticleSystem(parent, name, material, 30);
        ParticleSystem.MainModule main = ps.main;
        main.duration = 0.65f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.22f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.55f, 1.25f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.98f, 0.7f, 0.85f),
            new Color(1f, 0.64f, 0.16f, 0.55f)
        );
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 36) });

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(width, height, 0.1f);

        ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
        color.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.96f, 0.6f), 0f),
                new GradientColorKey(new Color(1f, 0.55f, 0.12f), 1f),
            },
            new[]
            {
                new GradientAlphaKey(0.95f, 0f),
                new GradientAlphaKey(0f, 1f),
            }
        );
        color.color = gradient;

        return ps;
    }

    private static ParticleSystem CreateSparkBurst(Transform parent, string name, Material material, float width, float height)
    {
        ParticleSystem ps = CreateParticleSystem(parent, name, material, 32);
        ParticleSystem.MainModule main = ps.main;
        main.duration = 1.65f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.85f, 1.55f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.7f, 4.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.98f, 0.55f, 1f),
            new Color(1f, 0.68f, 0.16f, 1f)
        );
        main.gravityModifier = 0.35f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[]
        {
            new ParticleSystem.Burst(0.08f, 54),
            new ParticleSystem.Burst(0.42f, 36),
        });

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(width * 1.04f, height * 0.95f, 0.1f);
        shape.randomDirectionAmount = 0.35f;

        ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.y = new ParticleSystem.MinMaxCurve(0.7f, 1.8f);
        velocity.space = ParticleSystemSimulationSpace.World;

        ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
        color.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 1f, 0.75f), 0f),
                new GradientColorKey(new Color(1f, 0.78f, 0.22f), 0.45f),
                new GradientColorKey(new Color(1f, 0.36f, 0.08f), 1f),
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.08f),
                new GradientAlphaKey(0.75f, 0.45f),
                new GradientAlphaKey(0f, 1f),
            }
        );
        color.color = gradient;

        ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
        size.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0f, 0.35f),
            new Keyframe(0.12f, 1f),
            new Keyframe(1f, 0f)
        );
        size.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        return ps;
    }

    private static ParticleSystem CreateRisingStream(Transform parent, string name, Material material, float width, float height)
    {
        ParticleSystem ps = CreateParticleSystem(parent, name, material, 31);
        ParticleSystem.MainModule main = ps.main;
        main.duration = 2.35f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.75f, 1.45f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.1f, 2.4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.15f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.96f, 0.55f, 0.95f),
            new Color(0.55f, 0.9f, 1f, 0.75f)
        );
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 62f;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(width * 0.96f, height * 0.86f, 0.1f);
        shape.randomDirectionAmount = 0.12f;

        ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.45f, 0.45f);
        velocity.y = new ParticleSystem.MinMaxCurve(1.4f, 2.4f);
        velocity.space = ParticleSystemSimulationSpace.World;

        ParticleSystem.NoiseModule noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.55f;
        noise.frequency = 0.42f;
        noise.scrollSpeed = 0.8f;

        ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
        color.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.95f, 0.5f), 0f),
                new GradientColorKey(new Color(0.45f, 0.85f, 1f), 1f),
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.85f, 0.2f),
                new GradientAlphaKey(0f, 1f),
            }
        );
        color.color = gradient;

        return ps;
    }

    private static ParticleSystem CreateEdgeStream(Transform parent, string name, Material material, Vector3 localPosition, Vector3 shapeScale)
    {
        ParticleSystem ps = CreateParticleSystem(parent, name, material, 33);
        ps.transform.localPosition = localPosition;

        ParticleSystem.MainModule main = ps.main;
        main.duration = 2.25f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.55f, 1.1f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.45f, 1.4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.14f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 1f, 0.72f, 1f),
            new Color(0.7f, 0.9f, 1f, 0.82f)
        );
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 28f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0.12f, 20) });

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = shapeScale;
        shape.randomDirectionAmount = 0.45f;

        ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.35f, 0.35f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.5f, 1.55f);
        velocity.space = ParticleSystemSimulationSpace.World;

        ParticleSystem.NoiseModule noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.38f;
        noise.frequency = 0.65f;

        ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
        color.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.96f, 0.48f), 0f),
                new GradientColorKey(new Color(0.48f, 0.88f, 1f), 1f),
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.95f, 0.18f),
                new GradientAlphaKey(0f, 1f),
            }
        );
        color.color = gradient;

        return ps;
    }

    private static ParticleSystem CreateRing(Transform parent, string name, Material material, float width, float height)
    {
        ParticleSystem ps = CreateParticleSystem(parent, name, material, 29);
        ParticleSystem.MainModule main = ps.main;
        main.duration = 0.9f;
        main.loop = false;
        main.startLifetime = 0.75f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.8f, 3.1f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.16f, 0.3f);
        main.startColor = new Color(1f, 0.9f, 0.28f, 0.75f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0.05f, 120) });

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(width * 1.05f, height * 1.05f, 0.1f);
        shape.randomDirectionAmount = 0.8f;

        ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
        color.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.93f, 0.36f), 0f),
                new GradientColorKey(new Color(1f, 0.65f, 0.15f), 1f),
            },
            new[]
            {
                new GradientAlphaKey(0.7f, 0f),
                new GradientAlphaKey(0.45f, 0.35f),
                new GradientAlphaKey(0f, 1f),
            }
        );
        color.color = gradient;

        return ps;
    }

    private static ParticleSystem CreateParticleSystem(Transform parent, string name, Material material, int sortingOrder)
    {
        GameObject obj = new GameObject(name, typeof(ParticleSystem));
        obj.transform.SetParent(parent, false);
        ParticleSystem ps = obj.GetComponent<ParticleSystem>();

        ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.material = material;
        renderer.sortingOrder = sortingOrder;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.alignment = ParticleSystemRenderSpace.View;

        return ps;
    }

    private static TextMeshProUGUI CreateClearText(RectTransform parent)
    {
        GameObject textObject = new GameObject("ClearText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.66f);
        rect.anchorMax = new Vector2(0.5f, 0.66f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, ClearTextY);
        rect.sizeDelta = new Vector2(1120f, 270f);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = "CLEAR";
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 158f;
        text.fontStyle = FontStyles.Bold;
        text.color = new Color(1f, 0.93f, 0.38f, 0f);
        text.outlineWidth = 0.22f;
        text.outlineColor = Color.black;
        text.raycastTarget = false;

        return text;
    }

    private static Image CreateImage(RectTransform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }

    private static float EaseInOut(float t)
    {
        return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) * 0.5f;
    }

    private sealed class GoalClearUi
    {
        private readonly TextMeshProUGUI m_ClearText;
        private readonly Image m_FadeImage;
        private readonly RectTransform m_ClearRect;

        public GoalClearUi(TextMeshProUGUI clearText, Image fadeImage)
        {
            m_ClearText = clearText;
            m_FadeImage = fadeImage;
            m_ClearRect = clearText.rectTransform;
        }

        public void SetClearText(float alpha, float y, float scale)
        {
            Color color = m_ClearText.color;
            color.a = alpha;
            m_ClearText.color = color;
            m_ClearRect.anchoredPosition = new Vector2(0f, y);
            m_ClearRect.localScale = Vector3.one * scale;
        }

        public void SetFade(float alpha)
        {
            m_FadeImage.color = new Color(0f, 0f, 0f, alpha);
        }
    }

    private sealed class GoalClearWorldFx
    {
        private readonly Transform m_Root;
        private readonly ParticleSystem[] m_ParticleSystems;
        private bool m_HasPlayed;

        public GoalClearWorldFx(Transform root, params ParticleSystem[] particleSystems)
        {
            m_Root = root;
            m_ParticleSystems = particleSystems;
        }

        public void SetViewportY(Camera camera, float viewportY)
        {
            if (camera == null || m_Root == null)
            {
                return;
            }

            float distance = Mathf.Abs(camera.transform.position.z);
            Vector3 position = camera.ViewportToWorldPoint(new Vector3(0.5f, viewportY, distance));
            m_Root.position = new Vector3(position.x, position.y, 0f);
        }

        public void Play()
        {
            if (m_HasPlayed)
            {
                return;
            }

            m_HasPlayed = true;
            foreach (ParticleSystem particleSystem in m_ParticleSystems)
            {
                if (particleSystem == null)
                {
                    continue;
                }

                particleSystem.Play(true);
            }
        }
    }
}
