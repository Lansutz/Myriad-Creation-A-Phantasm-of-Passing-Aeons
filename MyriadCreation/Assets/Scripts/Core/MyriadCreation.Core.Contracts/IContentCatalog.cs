using System.Collections.Generic;

namespace MyriadCreation.Core.Contracts
{
    /// <summary>内容类别诊断信息（P0；纯 Core 类型，供 IContentCatalog 描述跨类别内容集）。</summary>
    public sealed class ContentKindInfo
    {
        /// <summary>类别标识（如 innovation/religion/race）。</summary>
        public string kind = "";

        /// <summary>键类型描述（诊断用，如 "int"）。</summary>
        public string keyType = "";

        /// <summary>该类别当前条目数。</summary>
        public int count;
    }

    /// <summary>
    /// 跨类别内容目录契约（P0）：只做诊断/门禁/通用查询，typed 消费走 Content 侧 typed catalog
    /// 或 host 侧 IContentLookup&lt;TKey,TValue&gt; 适配器。
    /// </summary>
    public interface IContentCatalog
    {
        /// <summary>Reset/Reload 后递增；便于消费者判断缓存失效。</summary>
        int Generation { get; }

        /// <summary>当前类别 id 列表。</summary>
        IReadOnlyList<string> Kinds { get; }

        /// <summary>各类别条目数/键类型诊断信息。</summary>
        IReadOnlyList<ContentKindInfo> DescribeKinds();

        /// <summary>按类别 + 字符串 id 粗查原始对象（诊断/工具用；typed 查询请走 IContentLookup）。</summary>
        bool TryGetRaw(string kind, string id, out object value);
    }
}
