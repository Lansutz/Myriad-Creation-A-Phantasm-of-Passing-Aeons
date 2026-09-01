using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace CivilizationEvolution.UI
{
    /// <summary>
    /// 音乐播放器：加载 StreamingAssets/Music/ 下音频（mp3/ogg/wav）循环播放
    /// 支持播放/暂停/上一首/下一首/音量——用户可自行放入音乐文件
    /// </summary>
    public class MusicPlayer : MonoBehaviour
    {
        private AudioSource _source;
        private readonly List<string> _trackNames = new List<string>();
        private int _currentIndex = -1;
        private bool _loading;

        private void Awake()
        {
            _source = gameObject.AddComponent<AudioSource>();
            _source.loop = true;
            _source.playOnAwake = false;
            _source.volume = 0.5f;
        }

        private void Start()
        {
            RefreshTracks();
        }

        /// <summary>扫描 Music 目录（mp3/ogg/wav）</summary>
        public void RefreshTracks()
        {
            _trackNames.Clear();
            string dir = Path.Combine(Application.streamingAssetsPath, "Music");
            if (Directory.Exists(dir))
            {
                foreach (var f in Directory.GetFiles(dir))
                {
                    string ext = Path.GetExtension(f).ToLowerInvariant();
                    if (ext == ".mp3" || ext == ".ogg" || ext == ".wav")
                        _trackNames.Add(Path.GetFileName(f));
                }
            }
            _trackNames.Sort();
            if (_currentIndex >= _trackNames.Count) _currentIndex = -1;
        }

        public int TrackCount => _trackNames.Count;
        public bool HasTracks => _trackNames.Count > 0;
        public string CurrentTrack => _currentIndex >= 0 && _currentIndex < _trackNames.Count
            ? _trackNames[_currentIndex] : "";

        /// <summary>播放/暂停切换</summary>
        public void TogglePlay()
        {
            if (_source.isPlaying) { Pause(); return; }
            if (_currentIndex < 0) { Next(); return; }
            Play();
        }

        public void Play()
        {
            if (_source.clip != null && !_loading) _source.Play();
        }

        public void Pause() => _source.Pause();

        public void Stop() => _source.Stop();

        /// <summary>下一首（自动循环列表）</summary>
        public void Next()
        {
            if (_trackNames.Count == 0) return;
            _currentIndex = (_currentIndex + 1) % _trackNames.Count;
            StartCoroutine(LoadTrack(_currentIndex));
        }

        /// <summary>上一首</summary>
        public void Previous()
        {
            if (_trackNames.Count == 0) return;
            _currentIndex = (_currentIndex - 1 + _trackNames.Count) % _trackNames.Count;
            StartCoroutine(LoadTrack(_currentIndex));
        }

        /// <summary>设置音量 0~1</summary>
        public void SetVolume(float v) => _source.volume = Mathf.Clamp01(v);

        public float GetVolume() => _source.volume;

        private IEnumerator LoadTrack(int index)
        {
            if (index < 0 || index >= _trackNames.Count) yield break;
            _loading = true;
            string path = Path.Combine(Application.streamingAssetsPath, "Music", _trackNames[index]);
            string url = "file://" + path.Replace('\\', '/');

            using (var request = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.UNKNOWN))
            {
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success)
                {
                    var clip = DownloadHandlerAudioClip.GetContent(request);
                    if (clip != null)
                    {
                        _source.clip = clip;
                        _source.Play();
                    }
                }
                else
                {
                    Debug.LogWarning($"[MusicPlayer] 加载失败：{_trackNames[index]}（{request.error}）");
                }
            }
            _loading = false;
        }

        private void Update()
        {
            // 单曲播完自动下一首
            if (_source != null && !_source.isPlaying && !_loading && _source.clip != null
                && _source.time <= 0f && _trackNames.Count > 1)
            {
                Next();
            }
        }
    }
}
