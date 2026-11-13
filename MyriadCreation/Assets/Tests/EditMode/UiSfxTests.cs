using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Infrastructure.Audio;
using MyriadCreation.UI.Common;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// UI 音效（task-62）回归：五个 WAV 资源存在、Play 在无音频设备（batchmode）不抛、
    /// Resources.Load 走缓存不重复加载、缺失 clip 静默。
    /// </summary>
    public class UiSfxTests
    {
        [SetUp]
        public void SetUp()
        {
            UiSfx.ClearCache();
        }

        [TearDown]
        public void TearDown()
        {
            UiSfx.ClearCache();
            // 清掉 SfxPlayer 的隐藏 AudioSource 池，避免在编辑器测试进程里残留 HideAndDontSave 对象。
            SfxPlayer.ResetForTests();
        }

        [Test]
        public void ClipResources_AllExist()
        {
            for (int i = 0; i < UiSfx.AllClipNames.Length; i++)
            {
                string name = UiSfx.AllClipNames[i];
                AudioClip direct = Resources.Load<AudioClip>("UI/" + name);
                Assert.IsNotNull(direct, $"缺少音频资源 Assets/Audio/Resources/UI/{name}.wav");
                Assert.IsNotNull(UiSfx.GetClip(name), $"UiSfx.GetClip(\"{name}\") 应返回非空 AudioClip");
            }
            Assert.AreEqual(UiSfx.AllClipNames.Length, UiSfx.CachedClipCount, "五个 clip 应全部进缓存");
        }

        [Test]
        public void Play_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => UiSfx.PlayClick(), "PlayClick 不应抛异常");
            Assert.DoesNotThrow(() => UiSfx.PlayHover(), "PlayHover 不应抛异常");
            Assert.DoesNotThrow(() => UiSfx.PlayOpen(), "PlayOpen 不应抛异常");
            Assert.DoesNotThrow(() => UiSfx.PlayClose(), "PlayClose 不应抛异常");
            Assert.DoesNotThrow(() => UiSfx.PlayError(), "PlayError 不应抛异常");
            Assert.DoesNotThrow(() => UiSfx.Play("__missing_clip__"), "缺失 clip 应静默不抛");
        }

        [Test]
        public void RepeatedPlay_UsesCache_DoesNotReload()
        {
            UiSfx.PlayClick();
            UiSfx.PlayClick();
            UiSfx.PlayClick();
            Assert.AreEqual(1, UiSfx.LoadCount, "同一个 clip 重复播放只应 Resources.Load 一次");
            Assert.AreEqual(1, UiSfx.CachedClipCount);

            UiSfx.PlayHover();
            Assert.AreEqual(2, UiSfx.LoadCount, "不同 clip 首次播放才新增一次加载");

            UiSfx.PlayClick();
            Assert.AreEqual(2, UiSfx.LoadCount, "缓存命中不应增加加载次数");
        }

        [Test]
        public void MissingClip_IsSilentAndCached()
        {
            Assert.IsNull(UiSfx.GetClip("__missing_clip__"), "缺失 clip 返回 null");
            int loadCount = UiSfx.LoadCount;
            Assert.IsNull(UiSfx.GetClip("__missing_clip__"));
            Assert.AreEqual(loadCount, UiSfx.LoadCount, "缺失 clip 的 null 结果也应缓存，避免反复 Load");
            Assert.DoesNotThrow(() => UiSfx.Play("__missing_clip__"));
        }
    }
}
