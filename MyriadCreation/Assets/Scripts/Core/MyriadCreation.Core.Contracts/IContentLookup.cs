using System.Collections.Generic;

namespace MyriadCreation.Core.Contracts
{
    /// <summary>
    /// 通用内容查找契约（P0）：typed 内容索引/宿主适配器统一形状。
    /// 泛型参数不落任何 Content/Simulation/World 类型，Core 可安全引用。
    /// 不同运行类型使用独立 adapter（同一类不能实现两个 IContentLookup&lt;int,X&gt;，All/Keys 会签名冲突）。
    /// </summary>
    public interface IContentLookup<TKey, TValue>
    {
        bool TryGet(TKey key, out TValue value);

        IReadOnlyCollection<TKey> Keys { get; }

        IReadOnlyCollection<TValue> All { get; }
    }
}
