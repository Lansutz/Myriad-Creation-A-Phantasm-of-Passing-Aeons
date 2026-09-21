using System;
using System.Collections.Generic;

namespace CivilizationEvolution.Simulation.Modding
{
    public interface IContentStore<TKey, TValue> : IContentResolver<TKey, TValue>
    {
        void Set(TKey id, TValue value);
        bool Remove(TKey id);
        void Clear();
    }

    public sealed class ContentStore<TKey, TValue> : IContentStore<TKey, TValue>
    {
        private readonly Dictionary<TKey, TValue> _values = new Dictionary<TKey, TValue>();
        public bool TryGet(TKey id, out TValue value) => _values.TryGetValue(id, out value);
        public IEnumerable<TValue> All => _values.Values;
        public void Set(TKey id, TValue value)
        {
            if (EqualityComparer<TKey>.Default.Equals(id, default(TKey))) throw new ArgumentException("Content id cannot be the default key.", nameof(id));
            if (ReferenceEquals(value, null)) throw new ArgumentNullException(nameof(value));
            _values[id] = value;
        }
        public bool Remove(TKey id) => _values.Remove(id);
        public void Clear() => _values.Clear();
    }

    public interface IContentProvider<TKey, TValue>
    {
        string ContentType { get; }
        void Load(ContentSource source, IContentStore<TKey, TValue> target);
    }

    public interface IContentProviderRegistration
    {
        string ContentType { get; }
        void Load(ContentSource source);
    }

    public sealed class ContentProviderRegistration<TKey, TValue> : IContentProviderRegistration
    {
        private readonly IContentProvider<TKey, TValue> _provider;
        private readonly IContentStore<TKey, TValue> _target;

        public ContentProviderRegistration(IContentProvider<TKey, TValue> provider, IContentStore<TKey, TValue> target)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _target = target ?? throw new ArgumentNullException(nameof(target));
        }

        public string ContentType => _provider.ContentType;
        public void Load(ContentSource source) => _provider.Load(source, _target);
    }

    public sealed class ContentProviderCatalog
    {
        private readonly List<IContentProviderRegistration> _providers = new List<IContentProviderRegistration>();

        public void Add<TKey, TValue>(IContentProvider<TKey, TValue> provider, IContentStore<TKey, TValue> target)
        {
            _providers.Add(new ContentProviderRegistration<TKey, TValue>(provider, target));
        }

        public void Load(ContentSource source)
        {
            foreach (var provider in _providers)
                provider.Load(source);
        }

        public int Count => _providers.Count;
    }

    /// <summary>Compatibility adapter while legacy registries migrate to typed stores.</summary>
    public sealed class DictionaryContentStore<TKey, TValue> : IContentStore<TKey, TValue>
    {
        private readonly Dictionary<TKey, TValue> _values;
        public DictionaryContentStore(Dictionary<TKey, TValue> values) => _values = values ?? throw new ArgumentNullException(nameof(values));
        public bool TryGet(TKey id, out TValue value) => _values.TryGetValue(id, out value);
        public IEnumerable<TValue> All => _values.Values;
        public void Set(TKey id, TValue value)
        {
            if (EqualityComparer<TKey>.Default.Equals(id, default(TKey))) throw new ArgumentException("Content id cannot be the default key.", nameof(id));
            if (ReferenceEquals(value, null)) throw new ArgumentNullException(nameof(value));
            _values[id] = value;
        }
        public bool Remove(TKey id) => _values.Remove(id);
        public void Clear() => _values.Clear();
    }

    /// <summary>Adapter for legacy/specialized content loaders. It still obeys the same provider contract.</summary>
    public sealed class DelegateContentProvider<TKey, TValue> : IContentProvider<TKey, TValue>
    {
        private readonly Action<string, IContentStore<TKey, TValue>> _loadRoot;
        public DelegateContentProvider(string contentType, Action<string, IContentStore<TKey, TValue>> loadRoot)
        {
            ContentType = string.IsNullOrEmpty(contentType) ? throw new ArgumentException("Content type is required.", nameof(contentType)) : contentType;
            _loadRoot = loadRoot ?? throw new ArgumentNullException(nameof(loadRoot));
        }
        public string ContentType { get; }
        public void Load(ContentSource source, IContentStore<TKey, TValue> target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            foreach (var root in EnumerateRoots(source.rootPath))
            {
                try { _loadRoot(root, target); }
                catch (Exception e) { UnityEngine.Debug.LogWarning($"[ContentProvider] {ContentType} 加载失败：{root}：{e.Message}"); }
            }
        }
        private static IEnumerable<string> EnumerateRoots(string root)
        {
            if (string.IsNullOrEmpty(root) || !System.IO.Directory.Exists(root)) yield break;
            bool canonical = System.IO.Directory.Exists(System.IO.Path.Combine(root, "Culture"))
                || System.IO.File.Exists(System.IO.Path.Combine(root, "Race", "RaceDefs.json"))
                || System.IO.File.Exists(System.IO.Path.Combine(root, "Innovation", "Innovations.json"))
                || System.IO.File.Exists(System.IO.Path.Combine(root, "Religion", "Religions.json"));
            if (canonical) { yield return root; yield break; }
            var dirs = System.IO.Directory.GetDirectories(root);
            Array.Sort(dirs, StringComparer.Ordinal);
            foreach (var dir in dirs) yield return dir;
        }
    }

    /// <summary>Generic provider for canonical wrapped JSON files.</summary>
    public sealed class JsonFileContentProvider<TKey, TValue, TWrapper> : IContentProvider<TKey, TValue>
    {
        private readonly string _relativeFile;
        private readonly Func<string, TWrapper> _deserialize;
        private readonly Func<TWrapper, IEnumerable<TValue>> _items;
        private readonly Func<TValue, TKey> _key;
        public JsonFileContentProvider(string contentType, string relativeFile, Func<string, TWrapper> deserialize, Func<TWrapper, IEnumerable<TValue>> items, Func<TValue, TKey> key)
        {
            ContentType = string.IsNullOrEmpty(contentType) ? throw new ArgumentException("Content type is required.", nameof(contentType)) : contentType;
            _relativeFile = relativeFile ?? throw new ArgumentNullException(nameof(relativeFile));
            _deserialize = deserialize ?? throw new ArgumentNullException(nameof(deserialize));
            _items = items ?? throw new ArgumentNullException(nameof(items));
            _key = key ?? throw new ArgumentNullException(nameof(key));
        }
        public string ContentType { get; }
        public void Load(ContentSource source, IContentStore<TKey, TValue> target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            foreach (var root in EnumerateRoots(source.rootPath))
            {
                var file = System.IO.Path.Combine(root, _relativeFile);
                if (!System.IO.File.Exists(file)) continue;
                try
                {
                    var wrapper = _deserialize(System.IO.File.ReadAllText(file));
                    if (wrapper == null) continue;
                    var values = _items(wrapper);
                    if (values == null) continue;
                    foreach (var value in values)
                    {
                        if (ReferenceEquals(value, null)) continue;
                        var key = _key(value);
                        if (EqualityComparer<TKey>.Default.Equals(key, default(TKey))) continue;
                        target.Set(key, value);
                    }
                }
                catch (Exception e) { UnityEngine.Debug.LogWarning($"[ContentProvider] {ContentType} 加载失败：{file}：{e.Message}"); }
            }
        }
        private static IEnumerable<string> EnumerateRoots(string root)
        {
            if (string.IsNullOrEmpty(root) || !System.IO.Directory.Exists(root)) yield break;
            if (System.IO.Directory.Exists(System.IO.Path.Combine(root, "Culture")) || System.IO.Directory.Exists(System.IO.Path.Combine(root, "Race")) || System.IO.Directory.Exists(System.IO.Path.Combine(root, "Innovation")) || System.IO.Directory.Exists(System.IO.Path.Combine(root, "Religion")))
            { yield return root; yield break; }
            var packageDirs = System.IO.Directory.GetDirectories(root);
            Array.Sort(packageDirs, StringComparer.Ordinal);
            foreach (var dir in packageDirs) yield return dir;
        }
    }
}
