using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Core.Contracts;
using MyriadCreation.UI.Panels;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>P1.6：家族树面板单文件负责构建/数据/翻页。</summary>
    public class FamilyTreePanelViewTests
    {
        private GameObject _go;

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
        }

        private FamilyTreePanelView NewView(out FakeCharacters characters, out FakeDetails details)
        {
            _go = new GameObject("FamilyTreePanelViewTest");
            var view = _go.AddComponent<FamilyTreePanelView>();
            view.Build(_go.transform);
            characters = new FakeCharacters();
            details = new FakeDetails();
            view.SetQueries(characters, details);
            return view;
        }

        [Test]
        public void Show_ListsParentsSpouseAndChildren()
        {
            FamilyTreePanelView view = NewView(out FakeCharacters characters, out FakeDetails details);
            characters.rows.Add(new CharacterRowDto { characterId = 1, name = "甲" });
            details.map[1] = new CharacterDetailDto
            {
                characterId = 1,
                name = "甲",
                age = 40,
                isAlive = true,
                dynastyName = "测试王朝",
                familyName = "测试家族",
                father = new FamilyMemberDto { name = "父辈", age = 65, isAlive = false },
                mother = new FamilyMemberDto { name = "母辈", age = 60, isAlive = true },
                spouse = new FamilyMemberDto { name = "配偶", age = 38, isAlive = true },
                siblings = new List<FamilyMemberDto> { new FamilyMemberDto { name = "兄", age = 44, isAlive = true } },
                children = new List<FamilyMemberDto> { new FamilyMemberDto { name = "子", age = 12, isAlive = true } }
            };

            view.Show();

            Assert.IsTrue(view.IsOpen);
            StringAssert.Contains("测试王朝", view.BodyText);
            StringAssert.Contains("父辈", view.BodyText);
            StringAssert.Contains("配偶", view.BodyText);
            StringAssert.Contains("兄", view.BodyText);
            StringAssert.Contains("子", view.BodyText);
            StringAssert.Contains("已故", view.BodyText, "已故亲属应标注");
        }

        [Test]
        public void Show_WithoutQuery_ShowsDash_AndDoesNotThrow()
        {
            _go = new GameObject("FamilyTreePanelViewEmpty");
            var view = _go.AddComponent<FamilyTreePanelView>();
            view.Build(_go.transform);

            Assert.DoesNotThrow(view.Show);
            Assert.AreEqual("-", view.BodyText);
            Assert.DoesNotThrow(view.Next);
            Assert.DoesNotThrow(view.Prev);
        }

        private sealed class FakeCharacters : ICharacterQuery
        {
            public readonly List<CharacterRowDto> rows = new List<CharacterRowDto>();

            public CharacterRowDto GetCharacter(int characterId) => rows.Find(r => r.characterId == characterId);

            public IReadOnlyList<CharacterRowDto> GetCharactersInRealm(int realmId) => rows;

            public CharacterRowDto GetRuler(int realmId) => rows.Count > 0 ? rows[0] : null;
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
