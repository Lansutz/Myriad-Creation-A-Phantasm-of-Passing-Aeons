using MyriadCreation.Content.Definitions;
using MyriadCreation.Core.Contracts;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;

namespace MyriadCreation.Simulation.WorldState
{
    /// <summary>
    /// <see cref="ILawContext"/> 的宿主适配器：把 <see cref="RealmData"/> 的只读数值/官职查询暴露给 Laws 核心逻辑。
    /// 位于 State/WorldState 侧（host），Laws 文件夹不反向依赖本类型。
    /// </summary>
    public sealed class LawRealmContext : ILawContext
    {
        private readonly RealmData _realm;

        public LawRealmContext(RealmData realm)
        {
            _realm = realm;
        }

        public int RealmId => _realm?.realmId ?? -1;

        public int GetSupremeRulerId() => _realm?.GetSupremeRulerId() ?? -1;

        public float Treasury => _realm?.treasury ?? 0f;

        public float Prestige => _realm?.prestige ?? 0f;

        public float Legitimacy => _realm?.lawState?.legitimacy ?? 0f;

        public float Centralization => _realm?.centralization ?? 0f;

        public float Authority => _realm?.lawState?.authority ?? 0f;

        public bool IsCentralOfficeHolder(int characterId)
        {
            if (_realm == null)
            {
                return false;
            }
            if (_realm.officeHolders != null)
            {
                foreach (var officeHolder in _realm.officeHolders)
                {
                    if (officeHolder.Value != characterId)
                    {
                        continue;
                    }
                    // 职权由内容表的模块决定：持 law_proposal 的官职才能推中央机构法。
                    // 一个角色可能同时挂多个槽位，必须看完全部槽位再判否。
                    // 表中不存在的旧槽位维持旧行为（任一官职），避免旧档/旧内容卡死。
                    if (!OfficeCatalog.TryGet(officeHolder.Key, out OfficeDef office))
                    {
                        return true;
                    }
                    if (OfficeSystem.HasModule(office, OfficeSystem.ModuleLawProposal))
                    {
                        return true;
                    }
                }
            }
            if (_realm.adminDivisions != null)
            {
                foreach (AdminDivision adminDivision in _realm.adminDivisions)
                {
                    if (adminDivision != null && adminDivision.holderCharacterId == characterId)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
