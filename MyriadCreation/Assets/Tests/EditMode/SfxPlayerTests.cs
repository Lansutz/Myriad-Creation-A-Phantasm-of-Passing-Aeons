using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MyriadCreation.Infrastructure.Audio;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// SfxPlayer 音效总线回归（task-59）：Volume 生效并下发到池内 AudioSource、无 clip 静默、
    /// AudioSource 池固定 4~8 个且多次播放只复用不增长。
    /// 工程当前无音频资源：用运行时 <see cref="AudioClip.Create"/> 构造测试 clip；
    /// batchmode 下 SfxPlayer 不真正 Play，测试只校验总线/池状态，故无音频设备也稳定。
    /// </summary>
    public class SfxPlayerTests
    {
        private static readonly FieldInfo SourcesField =
            typeof(SfxPlayer).GetField("_sources", BindingFlags.Static | BindingFlags.NonPublic);

        private AudioClip _clip;

        [SetUp]
        public void SetUp()
        {
            // 无音频设备/编辑器 EditMode 下 AudioSource.Play 可能打日志，不影响总线行为断言。
            LogAssert.ignoreFailingMessages = true;
            SfxPlayer.ResetForTests();
            _clip = AudioClip.Create("SfxPlayerTestClip", 64, 1, 44100, false);
        }

        [TearDown]
        public void TearDown()
        {
            SfxPlayer.ResetForTests();
            if (_clip != null)
            {
                Object.DestroyImmediate(_clip);
                _clip = null;
            }
            LogAssert.ignoreFailingMessages = false;
        }

        [Test]
        public void SetVolume_ClampsAndAppliesToPoolSources()
        {
            SfxPlayer.SetVolume(0.4f);
            Assert.AreEqual(0.4f, SfxPlayer.Volume, 0.0001f);
            SfxPlayer.Volume = 1.5f;
            Assert.AreEqual(1f, SfxPlayer.Volume, 0.0001f, "音量应夹到 1");
            SfxPlayer.SetVolume(-0.2f);
            Assert.AreEqual(0f, SfxPlayer.Volume, 0.0001f, "音量应夹到 0");
            SfxPlayer.SetVolume(0.5f);

            RequireClip();
            SfxPlayer.Play(_clip, 0.5f);
            AudioSource[] sources = GetPooledSources();
            Assert.IsNotNull(sources, "Play 后应创建 AudioSource 池");
            Assert.AreEqual(SfxPlayer.PoolSize, sources.Length, "池大小固定为 PoolSize");
            int played = 0;
            for (int i = 0; i < sources.Length; i++)
            {
                Assert.IsNotNull(sources[i], "池内 AudioSource 应全部创建");
                if (sources[i].clip == _clip)
                {
                    played++;
                    Assert.AreEqual(0.25f, sources[i].volume, 0.0001f, "播放音量 = 总线音量 × volumeScale");
                }
                else
                {
                    Assert.AreEqual(0.5f, sources[i].volume, 0.0001f, "未播放的音源应保持总线音量");
                }
            }
            Assert.AreEqual(1, played, "一次 Play 只占用一个音源");

            SfxPlayer.SetVolume(0.8f);
            for (int i = 0; i < sources.Length; i++)
            {
                Assert.AreEqual(0.8f, sources[i].volume, 0.0001f, "SetVolume 应同步到池内全部 AudioSource");
            }
        }

        [Test]
        public void Play_NullClip_IsSilentAndDoesNotCreatePool()
        {
            Assert.DoesNotThrow(() => SfxPlayer.Play(null));
            Assert.DoesNotThrow(() => SfxPlayer.Play(null, 0.5f));
            Assert.AreEqual(0, SfxPlayer.PooledSourceCount, "null clip 应静默返回，不创建/占用音频池");
        }

        [Test]
        public void Play_ReusesPool_AndStaysWithinUpperBound()
        {
            RequireClip();
            for (int i = 0; i < 32; i++)
            {
                SfxPlayer.Play(_clip, 1f);
            }
            int pooled = SfxPlayer.PooledSourceCount;
            Assert.AreEqual(SfxPlayer.PoolSize, pooled, "多次播放后池大小保持固定（循环复用）");
            Assert.GreaterOrEqual(pooled, 4, "卡要求 AudioSource 池 4~8");
            Assert.LessOrEqual(pooled, 8, "卡要求 AudioSource 池 4~8");
        }

        private void RequireClip()
        {
            if (_clip == null)
            {
                Assert.Ignore("当前环境无法创建 AudioClip（无音频设备），跳过音频池播放断言");
            }
        }

        private static AudioSource[] GetPooledSources()
        {
            Assert.IsNotNull(SourcesField, "找不到 SfxPlayer._sources（反射契约变化，需同步测试）");
            return (AudioSource[])SourcesField.GetValue(null);
        }
    }
}
