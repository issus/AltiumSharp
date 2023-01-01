using OpenMcdf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;

namespace OriginalCircuit.AltiumSharp
{
    public static class CFItemExtensions
    {
        public static CFItem TryGetChild(this CFItem item, string name)
        {
            if (item is CFStorage storage)
            {
                if (storage.TryGetStorage(name, out var resultStorage))
                {
                    return resultStorage;
                }
                else if (storage.TryGetStream(name, out var resultStream))
                {
                    return resultStream;
                }
            }

            return null;
        }

        public static CFItem GetChild(this CFItem item, string name)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            if (item is CFStorage storage)
            {
                return TryGetChild(item, name) ?? throw new ArgumentException($"Item '{name}' doesn't exists within storage '{storage.Name}'.", nameof(name));
            }
            else
            {
                throw new InvalidOperationException($"Item '{item.Name}' is a stream and cannot have child items.");
            }
        }

        public static IEnumerable<CFItem> Children(this CFItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            var result = new List<CFItem>();
            if (item is CFStorage storage)
            {
                storage.VisitEntries(childItem => result.Add(childItem), false);
            }
            else
            {
                throw new InvalidOperationException($"Item '{item.Name}' is a stream and cannot have child items.");
            }
            return result;
        }
    }

    public static class CFStorageExtensions
    {
        public static CFStream GetOrAddStream(this CFStorage storage, string streamName)
        {
            if (storage == null) throw new ArgumentNullException(nameof(storage));

            return storage.TryGetStream(streamName, out var childStream) ? childStream : storage.AddStream(streamName);
        }

        public static CFStorage GetOrAddStorage(this CFStorage storage, string storageName)
        {
            if (storage == null) throw new ArgumentNullException(nameof(storage));

            return storage.TryGetStorage(storageName, out var childStorage) ? childStorage : storage.AddStorage(storageName);
        }
    }

    public static class CFStreamExtensions
    {
        public static MemoryStream GetMemoryStream(this CFStream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            return new MemoryStream(stream.GetData());
        }

        public static BinaryReader GetBinaryReader(this CFStream stream, Encoding encoding)
        {
            return new BinaryReader(stream.GetMemoryStream(), encoding, false);
        }

        public static BinaryReader GetBinaryReader(this CFStream stream)
        {
            return GetBinaryReader(stream, Encoding.UTF8);
        }

        public static void Write(this CFStream stream, Action<BinaryWriter> action, Encoding encoding)
        {
            using (var memoryStream = new MemoryStream())
            using (var writer = new BinaryWriter(memoryStream, encoding))
            {
                action?.Invoke(writer);
                stream.SetData(memoryStream.ToArray());
            }
        }

        public static void Write(this CFStream stream, Action<BinaryWriter> action)
        {
            stream.Write(action, Encoding.UTF8);
        }
    }

    public static class CompoundFileExtensions
    {
        private static readonly Regex PathElementSplitter = new Regex(@"(?<!\\)\/+", RegexOptions.Compiled);

        public static CFItem TryGetItem(this CompoundFile cf, string path)
        {
            if (cf == null) throw new ArgumentNullException(nameof(cf));

            var pathElements = PathElementSplitter.Split(path);
            CFItem item = cf.RootStorage;
            foreach (var pathElement in pathElements)
            {
                item = item.TryGetChild(pathElement);
                if (item == null) break;
            }
            return item;
        }

        public static CFItem GetItem(this CompoundFile cf, string path)
        {
            return TryGetItem(cf, path) ?? throw new ArgumentException($"Storage or stream with path '{path}' doesn't exist.", nameof(path));
        }

        public static CFStorage TryGetStorage(this CompoundFile cf, string path)
        {
            return TryGetItem(cf, path) as CFStorage;
        }

        public static CFStorage GetStorage(this CompoundFile cf, string path)
        {
            return TryGetStorage(cf, path) ?? throw new ArgumentException($"Storage with path '{path}' doesn't exist.", nameof(path));
        }

        public static CFStream TryGetStream(this CompoundFile cf, string path)
        {
            return TryGetItem(cf, path) as CFStream;
        }

        public static CFStream GetStream(this CompoundFile cf, string path)
        {
            return TryGetStream(cf, path) ?? throw new ArgumentException($"Stream with path '{path}' doesn't exist.", nameof(path));
        }
    }

    public static class EnumExtensions {
        public static T WithFlag<T>(this T @enum, T flag, bool value = true) where T : Enum
        {
            var intEnum = Convert.ToInt32(@enum, CultureInfo.InvariantCulture);
            var intFlag = Convert.ToInt32(flag, CultureInfo.InvariantCulture);
            if (value)
            {
                return (T)Enum.ToObject(typeof(T), intEnum | intFlag);
            }
            else
            {
                return (T)Enum.ToObject(typeof(T), intEnum & ~intFlag);
            }
        }

        public static void SetFlag<T>(ref this T @enum, T flag, bool value = true) where T : struct, Enum =>
            @enum = WithFlag(@enum, flag, value);

        public static void ClearFlag<T>(ref this T @enum, T flag) where T : struct, Enum =>
            SetFlag(ref @enum, flag, false);
    }
}
