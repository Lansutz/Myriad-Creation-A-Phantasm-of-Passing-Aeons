using UnityEngine;

namespace MyriadCreation.Infrastructure.Audio
{
    /// <summary>
    /// 全局音效总线（task-59）：静态 API + 内部 AudioSource 池，供整个工程播放一次性短音效。
    /// 池大小固定 <see cref="PoolSize"/>（6，卡要求 4~8），循环复用不无限增长；
    /// 音量由 UI 层 SettingsService.ApplyAudio → <see cref="SetVolume"/> 推入
    /// （Infrastructure 不能反向引用 UI，UI→Infrastructure 方向合法）。
    /// 工程当前无音频片段资源：<see cref="Play"/> 对 null clip 直接返回、不报错；接入资源后自动生效。
    /// 池宿主使用 HideAndDontSave（Play 模式 DontDestroyOnLoad），编辑器/无音频设备（batchmode）安全。
    /// </summary>
    public static class SfxPlayer
    {
        /// <summary>AudioSource 池大小（4~8 范围内固定 6）。</summary>
        public const int PoolSize = 6;

        private static GameObject _poolHost;
        private static AudioSource[] _sources;
        private static int _nextIndex;
        private static float _volume = 1f;

        /// <summary>音效总线音量（0~1）；SettingsService.ApplyAudio 在应用设置时写入。</summary>
        public static float Volume
        {
            get => _volume;
            set
            {
                _volume = Mathf.Clamp01(value);
                ApplyVolumeToPool();
            }
        }

        /// <summary>当前池内 AudioSource 数量（0 = 尚未创建；测试/诊断用）。</summary>
        public static int PooledSourceCount => _sources == null ? 0 : _sources.Length;

        /// <summary>设置音效总线音量（等价 <see cref="Volume"/> setter，供 SettingsService 调用）。</summary>
        public static void SetVolume(float volume)
        {
            Volume = volume;
        }

        /// <summary>播放一次性音效：clip 为 null 时静默返回（工程无音频资源时也能安全调用）。</summary>
        public static void Play(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null)
            {
                return;
            }
            AudioSource[] sources = EnsurePool();
            if (sources == null || sources.Length == 0)
            {
                return;
            }
            AudioSource source = sources[_nextIndex];
            _nextIndex = (_nextIndex + 1) % sources.Length;
            if (source == null)
            {
                return;
            }
            source.clip = clip;
            source.volume = Mathf.Clamp01(_volume * Mathf.Clamp01(volumeScale));
            // 批处理/无音频设备（CI、-nographics）下不真正播放，避免底层音频错误日志；普通游戏运行不受影响。
            if (!Application.isBatchMode)
            {
                source.Play();
            }
        }

        private static AudioSource[] EnsurePool()
        {
            if (_sources != null)
            {
                return _sources;
            }
            _poolHost = new GameObject("SfxPlayerPool");
            _poolHost.hideFlags = HideFlags.HideAndDontSave;
            if (Application.isPlaying)
            {
                Object.DontDestroyOnLoad(_poolHost);
            }
            _sources = new AudioSource[PoolSize];
            for (int i = 0; i < _sources.Length; i++)
            {
                AudioSource source = _poolHost.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = 0f;
                source.volume = _volume;
                _sources[i] = source;
            }
            _nextIndex = 0;
            return _sources;
        }

        private static void ApplyVolumeToPool()
        {
            if (_sources == null)
            {
                return;
            }
            for (int i = 0; i < _sources.Length; i++)
            {
                if (_sources[i] != null)
                {
                    _sources[i].volume = _volume;
                }
            }
        }

#if UNITY_EDITOR
        /// <summary>测试/编辑器用：销毁池宿主并重置静态状态（不影响运行时逻辑）。</summary>
        public static void ResetForTests()
        {
            if (_poolHost != null)
            {
                Object.DestroyImmediate(_poolHost);
            }
            _poolHost = null;
            _sources = null;
            _nextIndex = 0;
            _volume = 1f;
        }
#endif
    }
}
