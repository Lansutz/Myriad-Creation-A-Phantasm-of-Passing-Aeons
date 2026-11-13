using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Core.Contracts;
using MyriadCreation.UI.Common;
using MyriadCreation.UI.Panels;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>P1.6：角色面板单文件负责构建/数据/翻页，并通过注册表交给管理器。</summary>
    public class CharacterPanelViewTests
    {
        private readonly List<GameObject> _created = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _created)
            {
                if (go != null) Object.DestroyImmediate(go);
            }
            _created.Clear();
        }

        private CharacterPanelView NewView(out FakeCharacters characters, out FakeDetails details)
        {
            var go = new GameObject("CharacterPanelViewTest");
            _created.Add(go);
            var view = go.AddComponent<CharacterPanelView>();
            view.Build(go.transform);

            characters = new FakeCharacters();
            details = new FakeDetails();
            view.SetQueries(characters, details);
            return view;
        }

        [Test]
        public void Show_DisplaysFirstCharacter_NextAndPrevCycle()
        {
            CharacterPanelView view = NewView(out FakeCharacters characters, out FakeDetails details);
            characters.rows.Add(new CharacterRowDto { characterId = 2, name = "乙" });
            characters.rows.Add(new CharacterRowDto { characterId = 1, name = "甲" });
            details.map[1] = new CharacterDetailDto { characterId = 1, name = "甲", age = 30, isMale = true, isAlive = true, roleName = "统治者", prowess = 40f };
            details.map[2] = new CharacterDetailDto { characterId = 2, name = "乙", age = 25, isMale = false, isAlive = true, roleName = "廷臣" };

            view.Show();

            Assert.IsTrue(view.IsOpen);
            Assert.AreEqual(1, view.CurrentCharacterId, "按 id 升序显示");
            Assert.AreEqual("甲", view.TitleText);
            StringAssert.Contains("年龄 30", view.BodyText);
            StringAssert.Contains("统治者", view.BodyText);

            view.Next();
            Assert.AreEqual(2, view.CurrentCharacterId);
            Assert.AreEqual("乙", view.TitleText);

            view.Next();
            Assert.AreEqual(1, view.CurrentCharacterId, "翻页应循环");

            view.Prev();
            Assert.AreEqual(2, view.CurrentCharacterId, "反向翻页应循环");

            view.Hide();
            Assert.IsFalse(view.IsOpen);
        }

        [Test]
        public void Show_WithoutCharacters_ShowsDash()
        {
            CharacterPanelView view = NewView(out _, out _);

            Assert.DoesNotThrow(view.Show);
            Assert.AreEqual(-1, view.CurrentCharacterId);
            Assert.AreEqual("-", view.BodyText);
        }

        [Test]
        public void Registry_RegisterAndHideAll()
        {
            CharacterPanelView view = NewView(out FakeCharacters characters, out FakeDetails details);
            characters.rows.Add(new CharacterRowDto { characterId = 1, name = "甲" });
            details.map[1] = new CharacterDetailDto { characterId = 1, name = "甲" };
            var registry = new UiPanelRegistry();

            registry.Register(view);
            Assert.IsTrue(registry.TryGet("character", out IUserInterfacePanel panel));
            Assert.AreSame(view, panel);
            Assert.AreSame(view, registry.Get<CharacterPanelView>("character"));

            view.Show();
            Assert.IsTrue(view.IsOpen);
            registry.HideAll();
            Assert.IsFalse(view.IsOpen);
        }

        private sealed class FakeCharacters : ICharacterQuery
        {
            public readonly List<CharacterRowDto> rows = new List<CharacterRowDto>();

            public CharacterRowDto GetCharacter(int characterId)
            {
                return rows.Find(r => r.characterId == characterId);
            }

            public IReadOnlyList<CharacterRowDto> GetCharactersInRealm(int realmId)
            {
                return rows;
            }

            public CharacterRowDto GetRuler(int realmId)
            {
                return rows.Count > 0 ? rows[0] : null;
            }
        }

        private sealed class FakeDetails : ICharacterDetailQuery
        {
            public readonly Dictionary<int, CharacterDetailDto> map = new Dictionary<int, CharacterDetailDto>();

            public CharacterDetailDto GetCharacterDetail(int characterId)
            {
                return map.TryGetValue(characterId, out CharacterDetailDto dto) ? dto : null;
            }
        }
    }
}
