using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CivilizationEvolution.Audio;

namespace CivilizationEvolution.Tests
{
    /// <summary>
    /// 音乐播放器测试（列表加载/播放控制/音量——代码生成音频不依赖外部文件）
    /// </summary>
    public class MusicPlayerTests
    {
        private static AudioClip MakeClip(string name, float freq = 440f, float duration = 1f)
        {
            int samples = (int)(duration * 44100);
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * i / 44100f) * 0.5f;
            var clip = AudioClip.Create(name, samples, 1, 44100, false);
            clip.SetData(data, 0);
            return clip;
        }

        [Test]
        public void LoadMusic_BuildsPlaylist()
        {
            var go = new GameObject("TestPlayer");
            var player = go.AddComponent<MusicPlayerSystem>();

            player.LoadMusic(new[] { MakeClip("曲一"), MakeClip("曲二"), MakeClip("曲三") });

            Assert.AreEqual(3, player.Playlist.Count, "三首曲目入列表");
            Assert.AreEqual(0, player.CurrentIndex, "默认第一首");
            Assert.AreEqual("曲一", player.CurrentClip.name);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void PlayNextPrev_TracksIndex()
        {
            var go = new GameObject("TestPlayer");
            var player = go.AddComponent<MusicPlayerSystem>();
            player.LoadMusic(new[] { MakeClip("A"), MakeClip("B"), MakeClip("C") });

            player.Play(0);
            Assert.AreEqual("A", player.CurrentClip.name);

            player.Next();
            Assert.AreEqual("B", player.CurrentClip.name, "下一首");

            player.Next();
            player.Next();
            Assert.AreEqual("A", player.CurrentClip.name, "列表循环回第一首");

            player.Prev();
            Assert.AreEqual("C", player.CurrentClip.name, "上一首（循环）");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Volume_Clamped()
        {
            var go = new GameObject("TestPlayer");
            var player = go.AddComponent<MusicPlayerSystem>();

            player.Volume = 1.5f;
            Assert.AreEqual(1f, player.Volume, "音量上限 1");
            player.Volume = -0.5f;
            Assert.AreEqual(0f, player.Volume, "音量下限 0");
            player.Volume = 0.35f;
            Assert.That(player.Volume, Is.EqualTo(0.35f).Within(0.001f), "正常设置");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void EmptyPlaylist_NoCrash()
        {
            var go = new GameObject("TestPlayer");
            var player = go.AddComponent<MusicPlayerSystem>();
            player.LoadMusic(null);

            Assert.AreEqual(0, player.Playlist.Count);
            player.Play();   // 空列表播放不崩
            player.Next();   // 空列表切歌不崩
            player.Prev();
            Assert.AreEqual(-1, player.CurrentIndex);
            Object.DestroyImmediate(go);
        }
    }
}
