using UnityEngine;

/// <summary>
/// Quản lý toàn bộ Sound Effects và Background Music.
/// Singleton — gọi AudioManager.Instance.PlayShoot() từ bất kỳ đâu.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;

    [Header("Background Music")]
    public AudioClip bgMusic;

    [Header("Player SFX")]
    public AudioClip[] shootClips;  // Shoot1-6
    public AudioClip[] hitClips;    // Hit1-4

    [Header("Enemy SFX")]
    public AudioClip punchClip;
    public AudioClip punch2Clip;
    public AudioClip laserClip;
    public AudioClip bonusClip;

    [Header("Volume Settings")]
    [Range(0, 1)] public float bgmVolume = 0.3f;
    [Range(0, 1)] public float sfxVolume = 0.6f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Tự tạo AudioSource nếu chưa có
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
        }
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }
    }

    void Start()
    {
        AutoLoadClips();
        PlayBGM();
    }

    void AutoLoadClips()
    {
#if UNITY_EDITOR
        string basePath = "Assets/Mad Doctor Assets/Audio";

        if (bgMusic == null)
            bgMusic = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{basePath}/BG Music.mp3");

        if (shootClips == null || shootClips.Length == 0)
        {
            var list = new System.Collections.Generic.List<AudioClip>();
            for (int i = 1; i <= 6; i++)
            {
                var clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{basePath}/Shoot{i}.wav");
                if (clip != null) list.Add(clip);
            }
            shootClips = list.ToArray();
        }

        if (hitClips == null || hitClips.Length == 0)
        {
            var list = new System.Collections.Generic.List<AudioClip>();
            for (int i = 1; i <= 4; i++)
            {
                var clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{basePath}/Hit{i}.wav");
                if (clip != null) list.Add(clip);
            }
            hitClips = list.ToArray();
        }

        if (punchClip == null)
            punchClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{basePath}/punch.wav");
        if (punch2Clip == null)
            punch2Clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{basePath}/Punch2.wav");
        if (laserClip == null)
            laserClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{basePath}/Laser.wav");
        if (bonusClip == null)
            bonusClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{basePath}/Bonus.wav");
#endif
    }

    // ─── BGM ───────────────────────────────────────────────────────
    public void PlayBGM()
    {
        if (bgMusic == null || bgmSource == null) return;
        bgmSource.clip = bgMusic;
        bgmSource.volume = bgmVolume;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        if (bgmSource != null) bgmSource.Stop();
    }

    // ─── SFX ───────────────────────────────────────────────────────
    void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, sfxVolume);
    }

    void PlayRandomSFX(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return;
        PlaySFX(clips[Random.Range(0, clips.Length)]);
    }

    // ─── Public API ────────────────────────────────────────────────
    public void PlayShoot()
    {
        PlayRandomSFX(shootClips);
    }

    public void PlayHit()
    {
        PlayRandomSFX(hitClips);
    }

    public void PlayPunch()
    {
        PlaySFX(Random.value > 0.5f ? punchClip : punch2Clip);
    }

    public void PlayLaser()
    {
        PlaySFX(laserClip);
    }

    public void PlayBonus()
    {
        PlaySFX(bonusClip);
    }
}
