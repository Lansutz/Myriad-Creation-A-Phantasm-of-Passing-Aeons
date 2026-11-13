using MyriadCreation.Core.Contracts;

namespace MyriadCreation.Simulation.ActionGate
{
    /// <summary>
    /// 职权解析：从 realm 的元首/摄政解析 actor 的授权来源（M2：元首/摄政/议会等）。
    /// 通过委托取数，避免解析器直接依赖 GameWorld；真实接线由 GameWorld 注入。
    /// </summary>
    public sealed class RealmAuthorityResolver : IAuthorityResolver
    {
        public delegate bool RulerLookup(int realmId, out int rulerId, out int regentId);

        private readonly RulerLookup _lookup;

        public RealmAuthorityResolver(RulerLookup lookup)
        {
            _lookup = lookup;
        }

        public bool TryResolve(int actorCharacterId, int actorRealmId, out AuthoritySource source)
        {
            source = AuthoritySource.None;
            if (actorRealmId < 0 || actorCharacterId < 0 || _lookup == null)
            {
                return false;
            }

            if (!_lookup(actorRealmId, out int rulerId, out int regentId))
            {
                return false;
            }

            if (actorCharacterId == rulerId)
            {
                source = AuthoritySource.HeadOfState;
                return true;
            }

            if (regentId >= 0 && actorCharacterId == regentId)
            {
                source = AuthoritySource.Regent;
                return true;
            }

            return false;
        }
    }
}
