using System.Collections.Generic;
using UnityEngine;
using MyriadCreation.Infrastructure.Audio;

namespace MyriadCreation.UI.Common
{
    /// <summary>
    /// UI 音效静态工具（task-62）：<c>Resources.Load&lt;AudioClip&gt;("UI/*")</c> 一次性缓存，
    /// 播放统一走 <see cref="SfxPlayer"/> 音效总线（音量由 SettingsService.ApplyAudio 推入）。
    /// 资源缺失时静默返回（工程无音频设备/batchmode 也安全）；缓存命中不重复 Resources.Load（不要每帧加载）。
    /// </summary>
    public static class UiSfx
    {
        public const string Click = "click";
        public const string Hover = "hover";
        public const string Open = "open";
        public const string Close = "close";
        public const string Error = "error";

        /// <summary>全部 UI 音效资源名（测试遍历用）。</summary>
        public static readonly string[] AllClipNames = { Click, Hover, Open, Close, Error };

        private const string ResourceFolder = "UI/";

        private static readonly Dictionary<string, AudioClip> _cache = new Dictionary<string, AudioClip>();

        /// <summary>实际 Resources.Load 调用次数（缓存命中不增加；测试用）。</summary>
        public static int LoadCount { get; private set; }

        /// <summary>当前缓存条目数（含缺失资源的 null 占位；测试用）。</summary>
        public static int CachedClipCount => _cache.Count;

        public static void PlayClick()
        {
            Play(Click, 1f);
        }

        public static void PlayHover()
        {
            Play(Hover, 0.55f);
        }

        public static void PlayOpen()
        {
            Play(Open, 1f);
        }

        public static void PlayClose()
        {
            Play(Close, 1f);
        }

        public static void PlayError()
        {
            Play(Error, 0.8f);
        }

        /// <summary>播放指定 UI 音效；资源缺失静默（不抛异常）。</summary>
        public static void Play(string clipName, float volumeScale = 1f)
        {
            AudioClip clip = GetClip(clipName);
            if (clip == null)
            {
                return;
            }
            // 编辑态（非 Play、非 batchmode）不真正播放，避免 EditMode 测试/编辑器音频设备日志；
            // batchmode 下 SfxPlayer 自己也会跳过 AudioSource.Play。
            if (!Application.isPlaying && !Application.isBatchMode)
            {
                return;
            }
            SfxPlayer.Play(clip, volumeScale);
        }

        /// <summary>获取（并缓存）音频片段；缺失返回 null，同时缓存 null 避免反复 Load。</summary>
        public static AudioClip GetClip(string clipName)
        {
            if (string.IsNullOrEmpty(clipName))
            {
                return null;
            }
            if (_cache.TryGetValue(clipName, out AudioClip cached))
            {
                return cached;
            }
            LoadCount++;
            AudioClip clip = Resources.Load<AudioClip>(ResourceFolder + clipName);
            _cache[clipName] = clip;
            return clip;
        }

        /// <summary>清空缓存并复位加载计数（测试/热重载用）。</summary>
        public static void ClearCache()
        {
            _cache.Clear();
            LoadCount = 0;
        }
    }
}
