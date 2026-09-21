using System;
using System.Collections.Generic;

namespace CivilizationEvolution.Simulation.Culture
{
    /// <summary>
    /// 扈从制发展等级。
    /// 跨文化通用机制（日耳曼comitatus / 蒙古那可儿 / 阿拉伯安萨尔 / 吐蕃尚论 / 维京hird），
    /// 不是某个文化专属。核心是首领-扈从的互惠人身依附关系。
    /// </summary>
    public enum RetinueLevel
    {
        /// <summary>无扈从制</summary>
        None = 0,
        /// <summary>非正式：自愿战士团体，无固定供养，战时聚集平时解散</summary>
        Informal = 1,
        /// <summary>成熟：首领供养扈从，誓言效忠，战利品分配</summary>
        Established = 2,
        /// <summary>制度化：扈从转化为贵族侍从/骑士，有固定等级和土地</summary>
        Institutionalized = 3,
        /// <summary>封建化：扈从制转化为封君封臣制，土地分封+军事义务</summary>
        Feudalized = 4
    }

    /// <summary>
    /// 单个扈从队（战团）。
    /// 首领的私人战士团体，基于个人忠诚和互惠义务。
    /// </summary>
    [Serializable]
    public class RetinueBand
    {
        /// <summary>扈从队ID</summary>
        public int bandId;

        /// <summary>首领角色ID</summary>
        public int leaderId;

        /// <summary>扈从角色ID列表</summary>
        public List<int> retainerIds = new List<int>();

        /// <summary>扈从人数（包括角色和普通战士）</summary>
        public int size;

        /// <summary>忠诚度（0-100，首领供养/战利品分配→忠诚）</summary>
        public float loyalty = 80f;

        /// <summary>声望（0-100，战绩→声望→吸引更多扈从）</summary>
        public float prestige = 0f;

        /// <summary>装备等级（0-10，影响战斗力）</summary>
        public int equipmentLevel = 1;

        /// <summary>驻地地块ID（-1=移动中）</summary>
        public int homeTile = -1;

        /// <summary>是否活跃（在地图上行动/参战）</summary>
        public bool isActive = false;
    }

    /// <summary>
    /// 扈从制系统（基础版）。
    /// 管理政权的扈从制等级和扈从队。
    /// 核心作用：
    /// 1. 军事：提供精锐战士单位
    /// 2. 政治：促进军事首领→王权转化（Chiefdom→EthnicGroup过渡条件之一）
    /// 3. 社会：扈从转化为贵族阶层（Institutionalized等级解锁NobilityClergy）
    /// </summary>
    public class RetinueSystem
    {
        /// <summary>各政权的扈从制等级（key=realmId）</summary>
        private readonly Dictionary<int, RetinueLevel> _realmLevels = new Dictionary<int, RetinueLevel>();

        /// <summary>各政权的扈从队列表（key=realmId）</summary>
        private readonly Dictionary<int, List<RetinueBand>> _realmBands = new Dictionary<int, List<RetinueBand>>();

        /// <summary>获取政权的扈从制等级</summary>
        public RetinueLevel GetLevel(int realmId)
        {
            return _realmLevels.TryGetValue(realmId, out var level) ? level : RetinueLevel.None;
        }

        /// <summary>设置政权的扈从制等级</summary>
        public void SetLevel(int realmId, RetinueLevel level)
        {
            _realmLevels[realmId] = level;
        }

        /// <summary>获取政权的所有扈从队</summary>
        public List<RetinueBand> GetBands(int realmId)
        {
            return _realmBands.TryGetValue(realmId, out var bands) ? bands : new List<RetinueBand>();
        }

        /// <summary>添加扈从队</summary>
        public void AddBand(int realmId, RetinueBand band)
        {
            if (!_realmBands.ContainsKey(realmId))
                _realmBands[realmId] = new List<RetinueBand>();
            _realmBands[realmId].Add(band);
        }

        /// <summary>扈从制是否达到制度化（可解锁贵族阶层）</summary>
        public bool IsInstitutionalized(int realmId)
        {
            return GetLevel(realmId) >= RetinueLevel.Institutionalized;
        }

        /// <summary>扈从制是否达到封建化（可解锁封君封臣制）</summary>
        public bool IsFeudalized(int realmId)
        {
            return GetLevel(realmId) >= RetinueLevel.Feudalized;
        }
    }
}
