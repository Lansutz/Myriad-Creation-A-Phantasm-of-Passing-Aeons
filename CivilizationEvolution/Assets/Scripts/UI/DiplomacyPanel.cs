using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CivilizationEvolution.Core;
using CivilizationEvolution.Diplomacy;

namespace CivilizationEvolution.UI
{
    /// <summary>
    /// 外交面板
    /// 管理与其他政权的外交关系：关系概览、盟约、从属、战争借口、宣战、和平条约
    /// </summary>
    public class DiplomacyPanel : MonoBehaviour
    {
        [Header("引用")]
        [SerializeField] private GameWorld world;
        // 运行时注入（Initialize——非场景序列化——[SerializeField] 会触发 UAC1010）
        private DiplomacyManager diplomacy;

        [Header("面板")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button closeButton;

        [Header("目标政权选择")]
        [SerializeField] private Dropdown targetRealmDropdown;
        [SerializeField] private Text targetRealmNameText;

        [Header("关系概览")]
        [SerializeField] private Text relationValueText;
        [SerializeField] private Text hostilityLevelText;
        [SerializeField] private Text conflictLevelText;
        [SerializeField] private Text warStatusText;
        [SerializeField] private Text trustText;
        [SerializeField] private Text threatText;

        [Header("盟约管理")]
        [SerializeField] private Transform allianceListContainer;
        [SerializeField] private GameObject allianceEntryPrefab;
        [SerializeField] private Dropdown allianceTypeDropdown;
        [SerializeField] private Button proposeAllianceButton;
        [SerializeField] private Button breakAllianceButton;

        [Header("从属关系")]
        [SerializeField] private Text subordinationStatusText;
        [SerializeField] private Dropdown subordinationTypeDropdown;
        [SerializeField] private Button establishSubordinationButton;
        [SerializeField] private Button releaseSubordinationButton;

        [Header("战争借口")]
        [SerializeField] private Transform casusBelliListContainer;
        [SerializeField] private GameObject casusBelliEntryPrefab;

        [Header("宣战面板")]
        [SerializeField] private GameObject declareWarPanel;
        [SerializeField] private Dropdown selectedCasusBelliDropdown;
        [SerializeField] private Dropdown warGoalDropdown;
        [SerializeField] private Button confirmDeclareWarButton;
        [SerializeField] private Button cancelDeclareWarButton;
        [SerializeField] private Text declarationPenaltyText;

        [Header("和平条约面板")]
        [SerializeField] private GameObject peaceTreatyPanel;
        [SerializeField] private Text peaceTreatyInfoText;
        [SerializeField] private Transform peaceClauseListContainer;
        [SerializeField] private GameObject peaceClauseEntryPrefab;
        [SerializeField] private Button offerPeaceButton;
        [SerializeField] private Button acceptPeaceButton;
        [SerializeField] private Button rejectPeaceButton;

        // 当前选中的目标政权
        private int _selectedTargetRealmId = -1;
        private List<int> _otherRealmIds = new List<int>();

        // 当前选中的战争借口和战争目标
        private int _selectedCasusBelliIndex = -1;
        private int _selectedWarGoalIndex = -1;

        // 缓存的有效战争借口列表
        private List<CasusBelli> _validCasusBelli = new List<CasusBelli>();
        private List<GameEnums.WarGoalType> _availableWarGoals = new List<GameEnums.WarGoalType>();

        /// <summary>初始化外交面板</summary>
        public void Initialize(GameWorld gameWorld, DiplomacyManager diplomacyManager)
        {
            world = gameWorld;
            diplomacy = diplomacyManager;

            closeButton.onClick.AddListener(ClosePanel);
            targetRealmDropdown.onValueChanged.AddListener(OnTargetRealmChanged);
            proposeAllianceButton.onClick.AddListener(OnProposeAlliance);
            breakAllianceButton.onClick.AddListener(OnBreakAlliance);
            establishSubordinationButton.onClick.AddListener(OnEstablishSubordination);
            releaseSubordinationButton.onClick.AddListener(OnReleaseSubordination);
            confirmDeclareWarButton.onClick.AddListener(OnConfirmDeclareWar);
            cancelDeclareWarButton.onClick.AddListener(OnCancelDeclareWar);
            offerPeaceButton.onClick.AddListener(OnOfferPeace);
            acceptPeaceButton.onClick.AddListener(OnAcceptPeace);
            rejectPeaceButton.onClick.AddListener(OnRejectPeace);

            // 初始化盟约类型下拉框（5种平等盟约）
            allianceTypeDropdown.ClearOptions();
            allianceTypeDropdown.AddOptions(new List<string>
            {
                "互不侵犯条约",
                "防御同盟",
                "进攻同盟",
                "全面同盟",
                "阵营"
            });

            // 初始化从属类型下拉框（5种不平等从属）
            subordinationTypeDropdown.ClearOptions();
            subordinationTypeDropdown.AddOptions(new List<string>
            {
                "朝贡国",
                "附庸国",
                "附属国",
                "保护国",
                "傀儡国"
            });

            HidePanels();
        }

        /// <summary>打开外交面板</summary>
        public void OpenPanel()
        {
            panelRoot.SetActive(true);
            RefreshTargetRealmList();
            RefreshAll();
        }

        /// <summary>关闭外交面板</summary>
        public void ClosePanel()
        {
            panelRoot.SetActive(false);
            HidePanels();
        }

        /// <summary>隐藏子面板</summary>
        private void HidePanels()
        {
            declareWarPanel.SetActive(false);
            peaceTreatyPanel.SetActive(false);
        }

        /// <summary>刷新目标政权列表</summary>
        private void RefreshTargetRealmList()
        {
            _otherRealmIds.Clear();
            var options = new List<string>();

            int playerRealmId = world.PlayerRealmId; // 需确保GameWorld有此属性
            foreach (var realm in world.realms.Values)
            {
                if (realm.realmId != playerRealmId)
                {
                    _otherRealmIds.Add(realm.realmId);
                    options.Add(realm.realmName);
                }
            }

            targetRealmDropdown.ClearOptions();
            targetRealmDropdown.AddOptions(options);

            if (_otherRealmIds.Count > 0)
            {
                _selectedTargetRealmId = _otherRealmIds[0];
                targetRealmDropdown.value = 0;
            }
        }

        /// <summary>目标政权改变</summary>
        private void OnTargetRealmChanged(int index)
        {
            if (index >= 0 && index < _otherRealmIds.Count)
            {
                _selectedTargetRealmId = _otherRealmIds[index];
                RefreshAll();
            }
        }

        /// <summary>刷新所有内容</summary>
        private void RefreshAll()
        {
            if (_selectedTargetRealmId < 0) return;

            RefreshRelationOverview();
            RefreshAllianceList();
            RefreshSubordinationStatus();
            RefreshCasusBelliList();
            RefreshWarGoalDropdown();
        }

        /// <summary>刷新关系概览</summary>
        private void RefreshRelationOverview()
        {
            int playerId = world.PlayerRealmId;
            var rel = diplomacy.GetRelation(playerId, _selectedTargetRealmId);
            var targetRealm = world.realms.ContainsKey(_selectedTargetRealmId) ? world.realms[_selectedTargetRealmId] : null;

            targetRealmNameText.text = targetRealm != null ? targetRealm.realmName : "未知";

            if (rel == null)
            {
                relationValueText.text = "—";
                hostilityLevelText.text = "—";
                conflictLevelText.text = "—";
                warStatusText.text = "和平";
                trustText.text = "—";
                threatText.text = "—";
                return;
            }

            // 关系值（带颜色）
            string relationColor = rel.relation >= 50 ? "#7FFF7F" : rel.relation >= 0 ? "#FFFFFF" : rel.relation >= -50 ? "#FFB366" : "#FF6666";
            relationValueText.text = $"<color={relationColor}>{rel.relation:F0}</color>";

            // 敌对程度
            hostilityLevelText.text = diplomacy.GetHostilityDescription(playerId, _selectedTargetRealmId);

            // 冲突等级
            conflictLevelText.text = diplomacy.GetConflictLevelDescription(playerId, _selectedTargetRealmId);

            // 战争状态
            warStatusText.text = rel.isAtWar ? "<color=#FF6666>战争中</color>" : "<color=#7FFF7F>和平</color>";

            // 信任和威胁
            trustText.text = $"信任: {rel.trust:F0}";
            threatText.text = $"威胁: {rel.threat:F0}";
        }

        /// <summary>刷新盟约列表</summary>
        private void RefreshAllianceList()
        {
            // 清除旧条目
            foreach (Transform child in allianceListContainer)
            {
                Destroy(child.gameObject);
            }

            int playerId = world.PlayerRealmId;
            var rel = diplomacy.GetRelation(playerId, _selectedTargetRealmId);
            if (rel == null) return;

            foreach (var alliance in rel.activeAlliances)
            {
                if (!alliance.isActive) continue;

                var entry = Instantiate(allianceEntryPrefab, allianceListContainer);
                var text = entry.GetComponentInChildren<Text>();
                string allianceName = GetAllianceTypeName(alliance.type);
                string duration = alliance.durationDays < 0 ? "永久" : $"{alliance.durationDays}天";
                text.text = $"{allianceName} ({duration})";

                var breakBtn = entry.GetComponentInChildren<Button>();
                if (breakBtn != null)
                {
                    breakBtn.onClick.AddListener(() =>
                    {
                        diplomacy.BreakAlliance(playerId, _selectedTargetRealmId, alliance.type);
                        RefreshAll();
                    });
                }
            }
        }

        /// <summary>提议盟约</summary>
        private void OnProposeAlliance()
        {
            if (_selectedTargetRealmId < 0) return;

            AllianceType type = (AllianceType)allianceTypeDropdown.value;
            int playerId = world.PlayerRealmId;

            var result = diplomacy.ProposeAlliance(playerId, _selectedTargetRealmId, type);
            if (result != null)
            {
                UIManager.Instance?.AddEventLog($"成功签订{GetAllianceTypeName(type)}", EventLogKind.Info);
            }
            else
            {
                UIManager.Instance?.AddEventLog($"无法签订{GetAllianceTypeName(type)}（关系不足或已存在）", EventLogKind.Warning);
            }

            RefreshAll();
        }

        /// <summary>解除盟约</summary>
        private void OnBreakAlliance()
        {
            if (_selectedTargetRealmId < 0) return;

            AllianceType type = (AllianceType)allianceTypeDropdown.value;
            int playerId = world.PlayerRealmId;

            diplomacy.BreakAlliance(playerId, _selectedTargetRealmId, type);
            UIManager.Instance?.AddEventLog($"解除{GetAllianceTypeName(type)}", EventLogKind.Warning);

            RefreshAll();
        }

        /// <summary>刷新从属关系状态</summary>
        private void RefreshSubordinationStatus()
        {
            int playerId = world.PlayerRealmId;
            var sub = diplomacy.GetSubordination(playerId, _selectedTargetRealmId);

            if (sub == null || !sub.isActive)
            {
                subordinationStatusText.text = "无从属关系";
                establishSubordinationButton.interactable = true;
                releaseSubordinationButton.interactable = false;
            }
            else
            {
                string subName = GetSubordinationTypeName(sub.type);
                string role = sub.suzerainId == playerId ? "宗主国" : "从属国";
                subordinationStatusText.text = $"{subName} ({role})";
                establishSubordinationButton.interactable = false;
                releaseSubordinationButton.interactable = sub.suzerainId == playerId;
            }
        }

        /// <summary>建立从属关系</summary>
        private void OnEstablishSubordination()
        {
            if (_selectedTargetRealmId < 0) return;

            SubordinationType type = (SubordinationType)subordinationTypeDropdown.value;
            int playerId = world.PlayerRealmId;

            var result = diplomacy.EstablishSubordination(playerId, _selectedTargetRealmId, type);
            if (result != null)
            {
                UIManager.Instance?.AddEventLog($"建立{GetSubordinationTypeName(type)}关系", EventLogKind.Info);
            }
            else
            {
                UIManager.Instance?.AddEventLog($"无法建立从属关系", EventLogKind.Warning);
            }

            RefreshAll();
        }

        /// <summary>解除从属关系</summary>
        private void OnReleaseSubordination()
        {
            if (_selectedTargetRealmId < 0) return;

            int playerId = world.PlayerRealmId;
            diplomacy.ReleaseSubordination(playerId, _selectedTargetRealmId);
            UIManager.Instance?.AddEventLog("解除从属关系", EventLogKind.Warning);

            RefreshAll();
        }

        /// <summary>刷新战争借口列表</summary>
        private void RefreshCasusBelliList()
        {
            // 清除旧条目
            foreach (Transform child in casusBelliListContainer)
            {
                Destroy(child.gameObject);
            }

            int playerId = world.PlayerRealmId;
            _validCasusBelli = diplomacy.GetValidCasusBelli(playerId, _selectedTargetRealmId);

            // 填充战争借口下拉框
            selectedCasusBelliDropdown.ClearOptions();
            var cbOptions = new List<string> { "无借口（不宣而战）" };
            foreach (var cb in _validCasusBelli)
            {
                cbOptions.Add($"{WarJustificationSystem.GetCBDescription(cb.type)} (正当性:{cb.justificationStrength:F0})");
            }
            selectedCasusBelliDropdown.AddOptions(cbOptions);
            _selectedCasusBelliIndex = -1;

            // 显示战争借口列表
            foreach (var cb in _validCasusBelli)
            {
                var entry = Instantiate(casusBelliEntryPrefab, casusBelliListContainer);
                var text = entry.GetComponentInChildren<Text>();
                string cbName = WarJustificationSystem.GetCBDescription(cb.type);
                int daysLeft = cb.expiryDay - diplomacy.CurrentDay;
                string expiry = daysLeft > 0 ? $"{daysLeft}天后过期" : "永久";
                text.text = $"{cbName}\n正当性: {cb.justificationStrength:F0} | {expiry}\n{cb.description}";
            }
        }

        /// <summary>刷新战争目标下拉框</summary>
        private void RefreshWarGoalDropdown()
        {
            warGoalDropdown.ClearOptions();
            _availableWarGoals.Clear();

            int playerId = world.PlayerRealmId;

            // 如果选中了战争借口，获取该借口支持的战争目标
            if (_selectedCasusBelliIndex >= 0 && _selectedCasusBelliIndex < _validCasusBelli.Count)
            {
                var cb = _validCasusBelli[_selectedCasusBelliIndex];
                _availableWarGoals = WarJustificationSystem.GetSupportedWarGoals(cb.type);
            }
            else
            {
                // 无借口时只有有限的战争目标
                _availableWarGoals = new List<GameEnums.WarGoalType>
                {
                    GameEnums.WarGoalType.Indemnity,
                    GameEnums.WarGoalType.Humiliation,
                    GameEnums.WarGoalType.ConquerTerritory
                };
            }

            var options = new List<string>();
            foreach (var goal in _availableWarGoals)
            {
                options.Add(GetWarGoalName(goal));
            }
            warGoalDropdown.AddOptions(options);
            _selectedWarGoalIndex = options.Count > 0 ? 0 : -1;

            UpdateDeclarationPenalty();
        }

        /// <summary>更新宣战惩罚显示</summary>
        private void UpdateDeclarationPenalty()
        {
            if (_selectedCasusBelliIndex < 0)
            {
                declarationPenaltyText.text = "<color=#FF6666>无借口宣战：高惩罚（名声-30, 稳定-15）</color>";
            }
            else if (_selectedCasusBelliIndex < _validCasusBelli.Count)
            {
                var cb = _validCasusBelli[_selectedCasusBelliIndex];
                float strength = cb.justificationStrength;
                string penaltyColor = strength >= 80 ? "#7FFF7F" : strength >= 50 ? "#FFD700" : "#FF6666";
                declarationPenaltyText.text = $"<color={penaltyColor}>有借口宣战：正当性 {strength:F0}（惩罚随正当性降低）</color>";
            }
        }

        /// <summary>打开宣战面板</summary>
        public void OpenDeclareWarPanel()
        {
            if (_selectedTargetRealmId < 0) return;

            int playerId = world.PlayerRealmId;
            var rel = diplomacy.GetRelation(playerId, _selectedTargetRealmId);
            if (rel == null || rel.isAtWar)
            {
                UIManager.Instance?.AddEventLog("已处于战争状态", EventLogKind.Warning);
                return;
            }

            declareWarPanel.SetActive(true);
            RefreshCasusBelliList();
            RefreshWarGoalDropdown();
        }

        /// <summary>确认宣战</summary>
        private void OnConfirmDeclareWar()
        {
            if (_selectedTargetRealmId < 0) return;

            int playerId = world.PlayerRealmId;
            CasusBelli selectedCB = null;
            GameEnums.WarGoalType selectedGoal = GameEnums.WarGoalType.None;

            // 获取选中的战争借口
            if (_selectedCasusBelliIndex >= 0 && _selectedCasusBelliIndex < _validCasusBelli.Count)
            {
                selectedCB = _validCasusBelli[_selectedCasusBelliIndex];
            }

            // 获取选中的战争目标
            int goalIndex = warGoalDropdown.value;
            if (goalIndex >= 0 && goalIndex < _availableWarGoals.Count)
            {
                selectedGoal = _availableWarGoals[goalIndex];
            }

            // 创建战争目标对象
            WarGoal warGoalObj = null;
            if (selectedGoal != GameEnums.WarGoalType.None)
            {
                warGoalObj = WarJustificationSystem.CreateWarGoal(selectedGoal, playerId, _selectedTargetRealmId, -1, -1);
            }

            // 宣战
            bool success = diplomacy.DeclareWarWithJustification(playerId, _selectedTargetRealmId, selectedCB, warGoalObj, "玩家宣战");

            if (success)
            {
                UIManager.Instance?.AddEventLog($"对 {(world.realms.ContainsKey(_selectedTargetRealmId) ? world.realms[_selectedTargetRealmId].realmName : "未知")} 宣战！", EventLogKind.War);
                declareWarPanel.SetActive(false);
                RefreshAll();
            }
            else
            {
                UIManager.Instance?.AddEventLog("宣战失败", EventLogKind.Warning);
            }
        }

        /// <summary>取消宣战</summary>
        private void OnCancelDeclareWar()
        {
            declareWarPanel.SetActive(false);
        }

        /// <summary>打开和平条约面板</summary>
        public void OpenPeaceTreatyPanel()
        {
            if (_selectedTargetRealmId < 0) return;

            int playerId = world.PlayerRealmId;
            var rel = diplomacy.GetRelation(playerId, _selectedTargetRealmId);
            if (rel == null || !rel.isAtWar)
            {
                UIManager.Instance?.AddEventLog("未处于战争状态", EventLogKind.Warning);
                return;
            }

            peaceTreatyPanel.SetActive(true);
            RefreshPeaceTreatyInfo();
        }

        /// <summary>刷新和平条约信息</summary>
        private void RefreshPeaceTreatyInfo()
        {
            int playerId = world.PlayerRealmId;
            var rel = diplomacy.GetRelation(playerId, _selectedTargetRealmId);
            if (rel == null) return;

            // 显示战争信息
            int warDays = diplomacy.CurrentDay - rel.warDeclaredDay;
            peaceTreatyInfoText.text = $"战争已持续 {warDays} 天\n敌对程度: {rel.hostilityLevel:F0}\n关系: {rel.relation:F0}";

            // 清除旧条款条目
            foreach (Transform child in peaceClauseListContainer)
            {
                Destroy(child.gameObject);
            }

            // 生成预设和平条约（基于战争分数）
            float warScore = CalculateWarScore(playerId, _selectedTargetRealmId);
            var treaty = WarJustificationSystem.GeneratePeaceTreaty(
                playerId, _selectedTargetRealmId, warScore,
                rel.activeWarGoals, diplomacy.CurrentDay);

            // 显示条约条款
            foreach (var clause in treaty.clauses)
            {
                var entry = Instantiate(peaceClauseEntryPrefab, peaceClauseListContainer);
                var text = entry.GetComponentInChildren<Text>();
                string clauseName = GetTreatyClauseName(clause.type);
                text.text = $"{clauseName}: {clause.description}";
            }
        }

        /// <summary>计算战争分数（简化版）</summary>
        private float CalculateWarScore(int realmA, int realmB)
        {
            // 简化：基于敌对程度和关系值计算战争分数
            var rel = diplomacy.GetRelation(realmA, realmB);
            if (rel == null) return 0f;

            float score = 50f + (rel.hostilityLevel - 50f) * 0.5f;
            return Mathf.Clamp(score, 0f, 100f);
        }

        /// <summary>提出和平</summary>
        private void OnOfferPeace()
        {
            if (_selectedTargetRealmId < 0) return;

            int playerId = world.PlayerRealmId;
            float warScore = CalculateWarScore(playerId, _selectedTargetRealmId);

            // 生成和平条约
            var rel = diplomacy.GetRelation(playerId, _selectedTargetRealmId);
            var treaty = WarJustificationSystem.GeneratePeaceTreaty(
                playerId, _selectedTargetRealmId, warScore,
                rel?.activeWarGoals, diplomacy.CurrentDay);

            // 执行和平条约
            if (rel != null)
            {
                rel.activeTreaties.Add(treaty);
                WarJustificationSystem.ExecutePeaceTreaty(treaty, rel, diplomacy.CurrentDay);
                rel.isAtWar = false;
                rel.relation = Mathf.Max(rel.relation, -30f);
            }

            UIManager.Instance?.AddEventLog("签订和平条约，战争结束", EventLogKind.War);
            peaceTreatyPanel.SetActive(false);
            RefreshAll();
        }

        /// <summary>接受和平</summary>
        private void OnAcceptPeace()
        {
            OnOfferPeace(); // 简化：接受和平等同于签订条约
        }

        /// <summary>拒绝和平</summary>
        private void OnRejectPeace()
        {
            peaceTreatyPanel.SetActive(false);
            UIManager.Instance?.AddEventLog("拒绝和平提议，战争继续", EventLogKind.Warning);
        }

        // ===== 辅助方法 =====

        private string GetAllianceTypeName(AllianceType type)
        {
            return type switch
            {
                AllianceType.NonAggressionPact => "互不侵犯条约",
                AllianceType.DefensiveAlliance => "防御同盟",
                AllianceType.OffensiveAlliance => "进攻同盟",
                AllianceType.TotalAlliance => "全面同盟",
                AllianceType.Faction => "阵营",
                _ => type.ToString()
            };
        }

        private string GetSubordinationTypeName(SubordinationType type)
        {
            return type switch
            {
                SubordinationType.Tributary => "朝贡国",
                SubordinationType.Vassal => "附庸国",
                SubordinationType.Associate => "附属国",
                SubordinationType.Protectorate => "保护国",
                SubordinationType.Puppet => "傀儡国",
                _ => type.ToString()
            };
        }

        private string GetWarGoalName(GameEnums.WarGoalType type)
        {
            return type switch
            {
                GameEnums.WarGoalType.ConquerTerritory => "征服领土",
                GameEnums.WarGoalType.ConquerRegion => "征服地区",
                GameEnums.WarGoalType.Vassalization => "附庸化",
                GameEnums.WarGoalType.PersonalUnion => "共主邦联",
                GameEnums.WarGoalType.Indemnity => "战争赔款",
                GameEnums.WarGoalType.ReleaseVassal => "释放附庸",
                GameEnums.WarGoalType.ConvertReligion => "改变宗教",
                GameEnums.WarGoalType.EnforceTradeRights => "强制贸易权",
                GameEnums.WarGoalType.Disarmament => "裁军",
                GameEnums.WarGoalType.Humiliation => "羞辱",
                GameEnums.WarGoalType.Annihilation => "彻底摧毁",
                GameEnums.WarGoalType.BorderAdjustment => "边境调整",
                GameEnums.WarGoalType.Independence => "独立",
                GameEnums.WarGoalType.InstallRuler => "扶植统治者",
                _ => type.ToString()
            };
        }

        private string GetTreatyClauseName(TreatyClauseType type)
        {
            return type switch
            {
                TreatyClauseType.TerritoryCession => "领土割让",
                TreatyClauseType.WarReparations => "战争赔款",
                TreatyClauseType.PrisonerExchange => "战俘交换",
                TreatyClauseType.TradeRights => "贸易权",
                TreatyClauseType.NavigationRights => "航行权",
                TreatyClauseType.DemilitarizedZone => "非军事区",
                TreatyClauseType.ArmsLimitation => "军备限制",
                TreatyClauseType.ReligiousFreedom => "宗教自由",
                TreatyClauseType.MinorityProtection => "少数民族保护",
                TreatyClauseType.AllianceCommitment => "同盟承诺",
                TreatyClauseType.NonInterference => "不干涉内政",
                TreatyClauseType.ArbitrationAgreement => "仲裁协定",
                TreatyClauseType.Vassalage => "附庸关系",
                TreatyClauseType.PersonalUnion => "共主邦联",
                TreatyClauseType.ReleasePrisoners => "释放囚犯",
                TreatyClauseType.TradePrivileges => "贸易特权",
                TreatyClauseType.Disarmament => "裁军",
                TreatyClauseType.Humiliation => "羞辱",
                TreatyClauseType.Annexation => "吞并",
                TreatyClauseType.Independence => "承认独立",
                TreatyClauseType.BorderDemilitarization => "边境非军事化",
                TreatyClauseType.RoyalMarriage => "王室联姻",
                TreatyClauseType.CulturalAssimilation => "文化同化",
                TreatyClauseType.WarCrimesTrial => "战争罪审判",
                TreatyClauseType.ResourceConcession => "资源特许权",
                TreatyClauseType.Truce => "停战协定",
                _ => type.ToString()
            };
        }
    }
}