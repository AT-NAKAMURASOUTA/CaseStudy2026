using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class GameAudioSettings : MonoBehaviour
{
    private const string BgmVolumeKey = "GameAudioSettings.BgmVolume";
    private const string SeVolumeKey = "GameAudioSettings.SeVolume";
    private const float DefaultVolume = 1f;
    private const float ApplyInterval = 0.5f;

    private static GameAudioSettings s_Instance;
    private static bool s_Loaded;
    private static float s_BgmVolume = DefaultVolume;
    private static float s_SeVolume = DefaultVolume;

    private readonly Dictionary<int, float> m_BaseVolumes = new Dictionary<int, float>();
    private float m_ApplyTimer;

    public static float BgmVolume
    {
        get
        {
            EnsureLoaded();
            return s_BgmVolume;
        }
    }

    public static float SeVolume
    {
        get
        {
            EnsureLoaded();
            return s_SeVolume;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    public static void SetBgmVolume(float volume)
    {
        EnsureLoaded();
        s_BgmVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(BgmVolumeKey, s_BgmVolume);
        PlayerPrefs.Save();
        ApplyAllAudioSources();
    }

    public static void SetSeVolume(float volume)
    {
        EnsureLoaded();
        s_SeVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(SeVolumeKey, s_SeVolume);
        PlayerPrefs.Save();
        ApplyAllAudioSources();
    }

    public static void ApplyAllAudioSources()
    {
        EnsureInstance();
        s_Instance.ApplyAllAudioSourcesInternal();
    }

    private static void EnsureLoaded()
    {
        if (s_Loaded)
        {
            return;
        }

        s_BgmVolume = PlayerPrefs.GetFloat(BgmVolumeKey, DefaultVolume);
        s_SeVolume = PlayerPrefs.GetFloat(SeVolumeKey, DefaultVolume);
        s_Loaded = true;
    }

    private static void EnsureInstance()
    {
        if (s_Instance != null)
        {
            EnsureLoaded();
            return;
        }

        GameObject root = new GameObject(nameof(GameAudioSettings));
        s_Instance = root.AddComponent<GameAudioSettings>();
        DontDestroyOnLoad(root);
        EnsureLoaded();
    }

    private void Awake()
    {
        if (s_Instance != null && s_Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        s_Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureLoaded();
        SceneManager.sceneLoaded += OnSceneLoaded;
        ApplyAllAudioSourcesInternal();
    }

    private void OnDestroy()
    {
        if (s_Instance != this)
        {
            return;
        }

        SceneManager.sceneLoaded -= OnSceneLoaded;
        s_Instance = null;
    }

    private void Update()
    {
        m_ApplyTimer -= Time.unscaledDeltaTime;
        if (m_ApplyTimer > 0f)
        {
            return;
        }

        m_ApplyTimer = ApplyInterval;
        ApplyAllAudioSourcesInternal();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyAllAudioSourcesInternal();
    }

    private void ApplyAllAudioSourcesInternal()
    {
        EnsureLoaded();

        AudioSource[] sources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
        foreach (AudioSource source in sources)
        {
            if (source == null)
            {
                continue;
            }

            int instanceId = source.GetInstanceID();
            if (!m_BaseVolumes.ContainsKey(instanceId))
            {
                m_BaseVolumes.Add(instanceId, source.volume);
            }

            float baseVolume = m_BaseVolumes[instanceId];
            source.volume = baseVolume * (IsBgmSource(source) ? s_BgmVolume : s_SeVolume);
        }
    }

    private static bool IsBgmSource(AudioSource source)
    {
        return ContainsBgmMarker(source.name)
            || ContainsBgmMarker(source.gameObject.name)
            || (source.clip != null && ContainsBgmMarker(source.clip.name));
    }

    private static bool ContainsBgmMarker(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        return value.IndexOf("BGM", System.StringComparison.OrdinalIgnoreCase) >= 0
            || value.IndexOf("Music", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
