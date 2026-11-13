using System.Collections.Generic;

namespace MyriadCreation.Simulation.Politics
{
    /// <summary>运行时头衔实例注册表（按 titleKey）。封闭开发：静态表，随 ContentRegistry.Reset 场景清空。</summary>
    public static class TitleInstanceRegistry
    {
        private static readonly Dictionary<string, TitleInstance> _instances = new Dictionary<string, TitleInstance>();

        public static IEnumerable<TitleInstance> All => _instances.Values;

        public static TitleInstance GetOrCreate(string titleId)
        {
            if (string.IsNullOrEmpty(titleId))
            {
                return null;
            }
            if (!_instances.TryGetValue(titleId, out TitleInstance instance))
            {
                instance = new TitleInstance(titleId);
                _instances[titleId] = instance;
            }
            return instance;
        }

        public static bool TryGet(string titleId, out TitleInstance instance)
        {
            return _instances.TryGetValue(titleId ?? string.Empty, out instance);
        }

        public static void Clear()
        {
            _instances.Clear();
        }
    }
}
