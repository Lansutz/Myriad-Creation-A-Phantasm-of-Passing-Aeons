using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using CivilizationEvolution.Core;
using CivilizationEvolution.Core.Constants;
using CivilizationEvolution.Core.Data;
using CivilizationEvolution.Core.Dto;
using CivilizationEvolution.Core.Enums;
using CivilizationEvolution.Infrastructure.Save;
using CivilizationEvolution.Rendering;
using CivilizationEvolution.Simulation.Characters;
using CivilizationEvolution.Simulation.Economy;
using CivilizationEvolution.Simulation.Events;
using CivilizationEvolution.Simulation.Generation;
using CivilizationEvolution.Simulation.Modding;
using CivilizationEvolution.Simulation.Politics;
using CivilizationEvolution.Simulation.Population;
using CivilizationEvolution.Simulation.Society;
using CivilizationEvolution.Simulation.WorldState;
using CivilizationEvolution.UI;







using CivilizationEvolution.Simulation.Religion;
using CivilizationEvolution.Simulation.Culture;
using CivilizationEvolution.UI.Panels;
namespace CivilizationEvolution.UI.Common
{
 /// UIManager.Panels —— 各内容面板（角色/宗教/社会/政权概览/音乐/家族树/设置/顶栏/地块信息）（partial class，与 UIManager.cs 共享字段与组件引用）
    public partial class UIManager : MonoBehaviour
    {

 /// <summary>更新顶部信息栏</summary>
        private void UpdateTopBar()
        {
            if (world == null) return;

            if (dateText != null)
            {
 // 时间=已历时长（开局起算——非纪元年第——）
                int ey = world.currentYear - world.startYear;
                int ed = world.currentDay - world.startDay;
                if (ed < 0) { ed += 365; ey -= 1; }
                dateText.text = ey <= 0 && ed <= 0 ? "开局"
                    : $"已历 {ey} 年 {ed} 天";
            }

 // 顶栏政权名（当前查看政权——点选跟随——死显示"未选择势力"修复）
            if (realmNameText != null)
            {
                int vr = ViewRealmId;
                if (world.realms.TryGetValue(vr, out var vrealm))
                {
                    bool isPlayer = vr == world.PlayerRealmId;
                    realmNameText.text = isPlayer ? $"{vrealm.realmName}（本家）" : vrealm.realmName;
                }
                else realmNameText.text = "无政权";
            }

 // 顶栏精简：国库/总人口移除（——政权数据进政权界面—— // 同时消除每 0.2s 全遍历地块算总人口的重操作）
 // 地图信息：尺寸 + 地块数 + 省份数
            if (mapInfoText != null)
            {
                int totalTiles = world.tiles.Length;
                int landTiles = world.GetLandTileCount();
                int seaTiles = world.GetSeaTileCount();
                int provinceCount = world.provinces != null ? world.provinces.Count : 0;
                mapInfoText.text = $"地图 {world.mapWidth}×{world.mapHeight}  地块 {totalTiles}(陆{landTiles}/海{seaTiles})  省份 {provinceCount}";
            }
        }


 /// <summary>更新地块详情</summary>
        private string GetCultureName(int cultureId)
        {
            if (world != null && world.cultures.TryGetValue(cultureId, out var c))
                return c.cultureName;
            return cultureId.ToString();
        }


        private string GetReligionName(int faithId)
        {
            var def = ReligionCatalog.Get(faithId);
            return def != null ? def.religionName : faithId.ToString();
        }


        private void UpdateTileInfo()
        {
            if (tileInfoPanel == null || _selectedTile < 0 || world == null) return;
            if (_selectedTile >= world.tiles.Length) return;

            ref TileData tile = ref world.tiles[_selectedTile];

            if (tileNameText != null)
            {
 // 标题=归属（CK3 式：点政权领=政权名——点无主=状态）
                int owner = tile.ownerRealmId;
                string realmTag;
                if (owner >= 0 && world.realms.TryGetValue(owner, out var or))
                    realmTag = or.realmName;
                else
                {
                    bool hasPop = tile.populationBlocks != null && tile.populationBlocks.Count > 0;
                    realmTag = hasPop ? "无主之地 · 聚落" : "无主之地 · 荒野";
                }
                tileNameText.text = $"{realmTag} · 地块 #{_selectedTile}";
            }
 // 查看政权按钮：仅领地块可用（无主无政权可看——隐藏）
            if (viewRealmButton != null)
                viewRealmButton.gameObject.SetActive(tile.ownerRealmId >= 0);
            if (tileTerrainText != null)
                tileTerrainText.text = $"高程: {tile.elevation01:F2}\n坡度: {tile.slopeDegree:F1}°\n海陆: {(tile.isLand ? "陆地" : "海洋")}\n海洋分级: {tile.oceanTier}";
            if (tileClimateText != null)
                tileClimateText.text = $"年均温: {tile.annualTemp:F1}℃\n年降水: {tile.annualPrecipMm:F0}mm\n湿度: {tile.airHumidityPct:F0}%\n温度带: {tile.climateZone}";
            if (tileBiomeText != null)
                tileBiomeText.text = $"群系: {tile.biome}\n肥力: {tile.fertility:F2}\n发展度: {tile.development:F2}";
            if (tilePopulationText != null)
            {
                float pop = 0f;
                if (tile.populationBlocks != null)
                    foreach (var pb in tile.populationBlocks)
                        pop += pb.count;
                long people = (long)(pop * 50f);

 // 地块归属分级（CK3 式——点任何地块都有准确反馈）： // 有主=政权领地；无主有人=部落/自由聚落；无主无人=荒野
                int owner = tile.ownerRealmId;
                if (owner >= 0 && world.realms.TryGetValue(owner, out var ownRealm))
                {
                    tilePopulationText.text = $"人口: {people:N0} 人\n所属: {ownRealm.realmName}";
                }
                else if (pop > 0f)
                {
 // 无主聚落（部落/自由民——无人涂色但有人——待征服/演化）
                    int domCulture = PopulationStats.GetDominantCulture(tile);
                    string cName = GetCultureName(domCulture);
                    tilePopulationText.text = $"人口: {people:N0} 人（无主聚落·{cName}）";
                }
                else
                {
                    tilePopulationText.text = "荒芜之地（无政权·无人烟）";
                }
            }
            if (tileEconomyText != null)
                tileEconomyText.text = $"法理政权: {tile.ownerRealmId}\n占领政权: {tile.occupyingRealmId}\n道路: {tile.roadLevel}\n连通海域: {tile.seaConnectId}";
        }


 // ===== 角色面板 =====
 /// <summary>打开角色面板（刷新角色列表并显示第一个）</summary>
        public void OpenCharacterPanel()
        {
            var cm = world != null ? world.GetCharacterManager() : null;
            if (cm == null) return;

            _characterList.Clear();
            foreach (var c in cm.GetAllCharacters().Values)
                _characterList.Add(c.characterId);
            _characterList.Sort();
            _charIndex = 0;

            if (characterPanel != null) characterPanel.SetActive(true);
            UpdateCharacterPanel();
        }


 /// <summary>关闭角色面板</summary>
        public void CloseCharacterPanel()
        {
            if (characterPanel != null) characterPanel.SetActive(false);
        }


 /// <summary>打开社会政治面板</summary>
 /// <summary>刷新宗教面板（国教教统信息+支柱+圣人——静态文本生成）</summary>
        private void RefreshReligionPanel()
        {
            if (world == null || religionPanelText == null) return;
            int viewRealm = ViewRealmId; // 视角政权（点选跟随——非固定玩家）
            ReligionDef succession = null;
            FaithSystem faith = null;
            int patronSaint = -1;
            if (viewRealm >= 0 && viewRealm < world.realms.Count)
            {
                var realm = world.realms[viewRealm];
                succession = ReligionCatalog.Get(realm.stateReligionId);
                faith = world.GetFaithSystem(realm.stateReligionId);
                patronSaint = realm.statePatronSaintId;
            }
            religionPanelText.text = ReligionPanelText.Build(succession, faith, patronSaint);
        }


 /// <summary>启动时恢复保存的显示设置（Bootstrap/GameManager 调）</summary>
        public static void RestoreDisplaySettings()
        {
            int w = PlayerPrefs.GetInt("screen_width", 0);
            int h = PlayerPrefs.GetInt("screen_height", 0);
            int mode = PlayerPrefs.GetInt("screen_mode", -1);
            if (w > 0 && h > 0)
            {
                var fm = mode switch
                {
                    0 => FullScreenMode.FullScreenWindow,
                    1 => FullScreenMode.Windowed,
                    2 => FullScreenMode.MaximizedWindow,
                    _ => FullScreenMode.FullScreenWindow
                };
                Screen.SetResolution(w, h, fm);
            }
        }


 /// <summary>查看政权总览（打开聚合面板——人口/国库/官职/宗教——
 /// 数据源=已有系统聚合——）</summary>
        private void OpenViewRealmPanel()
        {
            if (overviewPanel != null)
            {
                _viewingDivisionId = -1; // 每次打开回政权总览层
                _divisionNavStack.Clear();
                RefreshOverviewPanel();
                overviewPanel.SetActive(true);
            }
        }


 /// <summary>返回上一级（区划详情→父区划/政权总览）</summary>
        private void OnOverviewBack()
        {
            if (_viewingDivisionId < 0) { CloseOverviewPanel(); return; }
            _viewingDivisionId = _divisionNavStack.Count > 0
                ? _divisionNavStack[_divisionNavStack.Count - 1] : -1;
            if (_divisionNavStack.Count > 0) _divisionNavStack.RemoveAt(_divisionNavStack.Count - 1);
            RefreshOverviewPanel();
        }


 /// <summary>下钻到区划（按钮点击——push 当前——显示子详情）</summary>
        private void OnDivisionClicked(int divisionId)
        {
            if (_viewingDivisionId >= 0) _divisionNavStack.Add(_viewingDivisionId);
            _viewingDivisionId = divisionId;
            RefreshOverviewPanel();
        }


        private void CloseOverviewPanel()
        {
            if (overviewPanel != null) overviewPanel.SetActive(false);
        }


 /// <summary>刷新政权总览（_viewRealmId 视角政权——聚合各系统）</summary>
        private void RefreshOverviewPanel()
        {
            if (world == null || overviewText == null) return;
            int realmId = ViewRealmId;
            if (!world.realms.TryGetValue(realmId, out var realm))
            {
                overviewText.text = RealmOverviewText.Build(null, null);
                return;
            }
            var society = world.GetRealmSociety(realmId);
            var officeDisplay = BuildOfficeDisplay(world, realm);
            ReligionDef religion = realm.stateReligionId >= 0
                ? ReligionCatalog.Get(realm.stateReligionId) : null;
            string saint = "";
            if (realm.statePatronSaintId > 0 && religion != null)
            {
                foreach (var snt in CanonizationSystem.GetSaints(realm.stateReligionId))
                    if (snt.saintId == realm.statePatronSaintId) { saint = snt.saintName; break; }
            }
 // 两态显示：-1=政权总览（含区划树）——≥0=区划详情（下钻页）
            if (_viewingDivisionId < 0)
            {
                overviewText.text = RealmOverviewText.Build(realm, society, officeDisplay,
                    religion, saint, realmId == world.PlayerRealmId, realm.adminDivisions, -1);
            }
            else
            {
                var division = FindDivision(realm.adminDivisions, _viewingDivisionId);
                long pop = -1;
                if (division != null)
                {
                    long sum = 0;
                    foreach (int t in division.tiles)
                    {
                        var tile = t >= 0 && t < world.tiles.Length ? world.tiles[t] : default;
                        if (tile.populationBlocks != null)
                            foreach (var pb in tile.populationBlocks) sum += (long)pb.count;
                    }
                    pop = sum * 50L;
                }
                overviewText.text = RealmDivisionText.Build(division,
                    realm.adminDivisions, pop, "", realm.realmName);
            }

 // 区划按钮（当前层的直接子区划——下钻入口——动态重建）
            RefreshDivisionButtons(realm);
 // 返回按钮（详情态显示）
            if (overviewBackButton != null)
                overviewBackButton.gameObject.SetActive(_viewingDivisionId >= 0);
        }


 /// <summary>动态重建区划按钮（当前查看层[根或区划]的直接子——≤4 个）</summary>
        private void RefreshDivisionButtons(RealmData realm)
        {
            if (divisionButtonsRoot == null) return;
 // 清旧按钮
            foreach (Transform child in divisionButtonsRoot.transform)
                Destroy(child.gameObject);
            if (realm == null || realm.adminDivisions == null) return;

            int parentId = _viewingDivisionId < 0 ? -1 : _viewingDivisionId;
 // 收集直接子
            var children = new List<AdminDivision>();
            foreach (var d in realm.adminDivisions)
                if (d.parentDivisionId == parentId && d.level > 1) children.Add(d);
 // 区划树显示模式：总览态展示层2入口按钮；详情态展示下一层
            if (_viewingDivisionId < 0)
            {
                foreach (var d in realm.adminDivisions)
                    if (d.level == 2 && !children.Contains(d)) children.Add(d);
            }
            foreach (var d in children)
            {
                var btn = CreateRuntimeButton(d.name);
                int captured = d.divisionId;
                btn.onClick.AddListener(() => OnDivisionClicked(captured));
                btn.transform.SetParent(divisionButtonsRoot.transform, false);
            }
        }


 /// <summary>运行时按钮（代码构建——区划下钻入口）</summary>
        private UnityEngine.UI.Button CreateRuntimeButton(string label)
        {
            var go = new GameObject("DivBtn_" + label, typeof(RectTransform));
            var img = go.AddComponent<UnityEngine.UI.Image>();
            img.color = new Color32(52, 62, 78, 255);
            var btn = go.AddComponent<UnityEngine.UI.Button>();
            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var text = textGo.AddComponent<TMPro.TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 14;
            text.alignment = TMPro.TextAlignmentOptions.Center;
            text.color = Color.white;
            var lt = go.AddComponent<UnityEngine.UI.LayoutElement>();
            lt.minWidth = 90; lt.minHeight = 28;
            return btn;
        }


        private void OpenReligionPanel()
        {
            if (religionPanel != null)
            {
                RefreshReligionPanel();
                religionPanel.SetActive(true);
            }
        }


        private void CloseReligionPanel()
        {
            if (religionPanel != null) religionPanel.SetActive(false);
        }


        private void OpenSocietyPanel()
        {
            if (societyPanel != null) societyPanel.SetActive(true);
            RefreshSocietyPanel();
        }


 /// <summary>关闭社会政治面板</summary>
        private void CloseSocietyPanel()
        {
            if (societyPanel != null) societyPanel.SetActive(false);
        }


 /// <summary>刷新社会政治面板（阶层画像/派系/政体变迁）</summary>
        private void RefreshSocietyPanel()
        {
            if (societyText == null || world == null) return;
            int realmId = ViewRealmId; // 当前查看政权（点选地块自动跟随——非固定玩家）
            if (!world.realms.TryGetValue(realmId, out var realm)) return;

 // 官职显示组装（officeHolders→文化定制称号+持有者名——OfficeTitle 消费）
            var officeDisplay = BuildOfficeDisplay(world, realm);
            societyText.text = SocietyPanelText.Build(realm,
                world.GetRealmSociety(realmId), world.Factions, world.RegimeDynamics, world.currentDay,
                officeDisplay);
        }


 /// <summary>打开音乐播放器面板</summary>
        private void OpenMusicPanel()
        {
            MusicPlayer();
            if (musicPanel != null) musicPanel.SetActive(true);
            RefreshMusicPanel();
        }


 /// <summary>关闭音乐播放器面板</summary>
        private void CloseMusicPanel()
        {
            if (musicPanel != null) musicPanel.SetActive(false);
        }


 /// <summary>刷新音乐面板（曲目列表+当前状态）</summary>
        private void RefreshMusicPanel()
        {
            if (musicText == null) return;
            var mp = MusicPlayer();
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"── 音乐播放器 ── 音量 {mp.Volume * 100f:F0}%");
            sb.AppendLine(mp.IsPlaying ? "▶ 播放中" : "⏸ 暂停");
            for (int i = 0; i < mp.Playlist.Count; i++)
            {
                string mark = i == mp.CurrentIndex ? "▶ " : "  ";
                sb.AppendLine($"{mark}{i + 1}. {mp.Playlist[i].name}");
            }
            if (mp.Playlist.Count == 0)
                sb.AppendLine("（无曲目——请放入 Resources/Music/ 的 ogg/mp3）");
            musicText.text = sb.ToString();
        }


 /// <summary>打开家族树面板</summary>
        private void OpenFamilyTreePanel()
        {
            RefreshCharacterList();
            if (familyTreePanel != null) familyTreePanel.SetActive(true);
            RefreshFamilyTreePanel();
        }


 /// <summary>关闭家族树面板</summary>
        private void CloseFamilyTreePanel()
        {
            if (familyTreePanel != null) familyTreePanel.SetActive(false);
        }


 /// <summary>刷新家族树面板（当前角色分代树状）</summary>
        private void RefreshFamilyTreePanel()
        {
            if (familyTreeText == null || world == null) return;
            var cm = world.GetCharacterManager();
            if (cm == null) return;
            if (_characterList.Count == 0) RefreshCharacterList();
            if (_characterList.Count == 0)
            {
                familyTreeText.text = "（无角色）";
                return;
            }

            _charIndex = (_charIndex + _characterList.Count) % _characterList.Count;
            familyTreeText.text = cm.BuildFamilyTreeText(_characterList[_charIndex]);
        }


 /// <summary>刷新角色列表（角色面板数据源）</summary>
        private void RefreshCharacterList()
        {
            if (world == null) return;
            var cm = world.GetCharacterManager();
            if (cm == null) return;
            _characterList.Clear();
            foreach (var c in cm.GetCharactersByRealm(world.PlayerRealmId >= 0 ? world.PlayerRealmId : 0))
                _characterList.Add(c.characterId);
            if (_characterList.Count > 0)
                _charIndex = Mathf.Clamp(_charIndex, 0, _characterList.Count - 1);
        }


 /// <summary>刷新角色面板（每帧调用，角色数据动态变化）</summary>
        private void UpdateCharacterPanel()
        {
            var cm = world != null ? world.GetCharacterManager() : null;
            if (cm == null || _characterList.Count == 0)
            {
                if (charNameText != null) charNameText.text = "无角色";
                return;
            }

            if (_charIndex < 0) _charIndex = 0;
            if (_charIndex >= _characterList.Count) _charIndex = _characterList.Count - 1;

            var c = cm.GetCharacter(_characterList[_charIndex]);
            if (c == null) return;

            if (charNameText != null)
                charNameText.text = $"{c.fullName}  {c.age}岁 {(c.isMale ? "男" : "女")}  [{_charIndex + 1}/{_characterList.Count}]";

            if (charStatusText != null)
            {
                string rulerType = c.role == CharacterRole.Ruler ? $"｜{GetRulerTypeName(c.GetRulerType())}" : "";
                string disorder = MentalHealthSystem.GetDisorderName(c);
                string disorderStr = disorder.Length > 0
                    ? $"｜<color=#{ColorUtility.ToHtmlStringRGB(UITheme.LogWar)}>患{disorder}</color>" : "";
                charStatusText.text =
                    $"政权{c.realmId}｜{(c.isAlive ? "在世" : "已故")}｜威望 Lv{c.prestigeCapacityLevel}{rulerType}{disorderStr}";
            }

            if (charStatsText != null)
            {
                charStatsText.text =
                    $"武力 {c.martial:F0}    外交 {c.diplomacy:F0}    军事经略 {c.warfare:F0}\n" +
                    $"管理 {c.stewardship:F0}    谋略 {c.intrigue:F0}    学识 {c.learning:F0}\n" +
                    $"威望 {c.prestige:F0}/{c.GetPrestigeCapacity():F0}    恶名 {c.notoriety:F0}\n" +
                    $"健康 {c.health:F0}    压力 {c.stress:F0}    恐惧 {c.dread:F0}    肥胖 {c.obesity:F0}\n" +
                    $"魅力 {c.charm:F0}    预期寿命 {c.expectedLifespanYears:F0}岁";
            }

            if (charPersonalityText != null)
            {
                charPersonalityText.text =
                    $"大胆 {c.boldness:F0}    悲悯 {c.compassion:F0}    贪婪 {c.greed:F0}    荣誉 {c.honor:F0}\n" +
                    $"理性 {c.rationality:F0}    报复 {c.vengefulness:F0}    虔信 {c.piety:F0}";
            }

            if (charDescText != null)
                charDescText.text = c.GetPersonalityDescription();

            if (charDnaText != null)
            {
                string extra = "";
                var talentDef = DnaSystem.FindDef(c.dnaExpression.talentId);
                var defectDef = DnaSystem.FindDef(c.dnaExpression.defectId);
                if (talentDef != null) extra += $"｜天赋：{talentDef.name}";
                if (defectDef != null) extra += $"｜隐疾：{defectDef.name}";
                charDnaText.text = $"外貌：{c.dnaExpression.appearanceTag}{extra}";
            }
        }

    }
}
