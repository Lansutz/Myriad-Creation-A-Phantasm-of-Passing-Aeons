using System.Collections.Generic;
using UnityEngine;

namespace CivilizationEvolution.Audio
{
    /// <summary>
    /// 音乐播放器：Resources/Music/ 下的 AudioClip 列表播放
    /// （手动控制：播放/暂停/切歌/音量/循环——用户放 ogg/mp3 进 Resources/Music 即入列表）
    /// </summary>
    public class MusicPlayerSystem : MonoBehaviour
    {
        public static MusicPlayerSystem Instance { get; private set; }

        [SerializeField] private AudioSource source;
        [SerializeField] private float volume = 0.6f;
        [SerializeField] private bool loopList = true;

        private List<AudioClip> _playlist = new List<AudioClip>();
        private int _currentIndex = -1;
        private bool _manualPaused;

        public IReadOnlyList<AudioClip> Playlist => _playlist;
        public int CurrentIndex => _currentIndex;
        public AudioClip CurrentClip => _currentIndex >= 0 && _currentIndex < _playlist.Count ? _playlist[_currentIndex] : null;
        public bool IsPlaying => source != null && source.isPlaying;
        public float Volume { get => volume; set { volume = Mathf.Clamp01(value); if (source != null) source.volume = volume; } }

        private void Awake()
        {
            Instance = this;
            EnsureSource();
            source.volume = volume;
        }

        /// <summary>确保 AudioSource 存在（防御：AddComponent 后 Awake 时序/重建场景时）</summary>
        private void EnsureSource()
        {
            if (source == null)
            {
                source = GetComponent<AudioSource>();
                if (source == null)
                    source = gameObject.AddComponent<AudioSource>();
            }
            source.playOnAwake = false;
            source.loop = false; // 列表循环由系统管理（单曲循环=false）
        }

        private void Update()
        {
            // 播完自动下一首（列表循环；仅"播放过且自然播完"时切歌——避免开局空转）
            if (source != null && !source.isPlaying && !_manualPaused
                && _playlist.Count > 0 && _currentIndex >= 0 && source.timeSamples > 0)
            {
                Next();
            }
        }

        /// <summary>加载 Resources/Music/ 全部曲目（外部注入或运行时加载）</summary>
        public void LoadMusic(IEnumerable<AudioClip> clips)
        {
            _playlist.Clear();
            if (clips != null)
                foreach (var c in clips)
                    if (c != null) _playlist.Add(c);
            _currentIndex = _playlist.Count > 0 ? 0 : -1;
        }

        /// <summary>从 Resources 加载（Resources.LoadAll<AudioClip>("Music")）</summary>
        public void LoadFromResources()
        {
            LoadMusic(Resources.LoadAll<AudioClip>("Music"));
        }

        public void Play(int index = -1)
        {
            if (_playlist.Count == 0) return;
            EnsureSource();
            int target = index >= 0 ? index : (_currentIndex >= 0 ? _currentIndex : 0);
            if (target >= _playlist.Count) target = 0;

            _currentIndex = target;
            _manualPaused = false;
            source.clip = _playlist[target];
            source.Play();
        }

        public void Pause()
        {
            if (source == null) return;
            _manualPaused = true;
            source.Pause();
        }

        public void Resume()
        {
            if (source == null) return;
            _manualPaused = false;
            source.UnPause();
        }

        public void Stop()
        {
            if (source == null) return;
            _manualPaused = true;
            source.Stop();
        }

        public void Next()
        {
            if (_playlist.Count == 0) return;
            int next = _currentIndex + 1;
            if (next >= _playlist.Count) next = loopList ? 0 : -1;
            if (next >= 0) Play(next);
        }

        public void Prev()
        {
            if (_playlist.Count == 0) return;
            int prev = _currentIndex - 1;
            if (prev < 0) prev = loopList ? _playlist.Count - 1 : 0;
            Play(prev);
        }
    }
}
