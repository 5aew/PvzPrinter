using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PvzPrinter.Core.Configuration;
using PvzPrinter.Core.Logging;

namespace PvzPrinter.Core.Printing;

public class TwoLevelLabelCache : IDisposable
{
    private const string CacheFileName = "label_cache.bin";
    private const string MagicHeader = "PVZCACHE";
    private const int MaxEntries = 12000;
    private const int Level1Capacity = 300;

    private readonly IConfigProvider _configProvider;
    private readonly ILogger _logger;
    private readonly ILabelRenderer _renderer;
    private readonly string _cachePath;
    private readonly string _currentSettingsHash;

    // Level 1: In-memory LRU
    private readonly ConcurrentDictionary<string, byte[]> _l1Cache = new();
    private readonly ConcurrentQueue<string> _lruOrder = new();

    // Level 2: Disk index
    private readonly ConcurrentDictionary<string, DiskEntry> _diskIndex = new();
    private readonly object _fileLock = new();

    private record DiskEntry(long Offset, int Length, DateTime LastUsed);

    public TwoLevelLabelCache(
        IConfigProvider configProvider,
        ILogger logger,
        ILabelRenderer renderer)
    {
        _configProvider = configProvider;
        _logger = logger;
        _renderer = renderer;
        _cachePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, CacheFileName);
        _currentSettingsHash = ComputeSettingsHash(configProvider.GetSettings());

        LoadDiskIndex();
    }

    /// <summary>
    /// Получает 1bpp Bitmap для ячейки. 
    /// Возвращает новый Bitmap — вызывающий код обязан его задиспозить.
    /// </summary>
    public Bitmap GetOrRender(string cellCode, PrintSettings settings)
    {
        // Level 1: RAM
        if (_l1Cache.TryGetValue(cellCode, out var l1Data))
        {
            TouchLru(cellCode);
            return BytesToBitmap(l1Data, settings);
        }

        // Level 2: Disk
        if (_diskIndex.TryGetValue(cellCode, out var entry))
        {
            var data = ReadFromDisk(entry);
            if (data != null)
            {
                AddToL1(cellCode, data);
                UpdateLastUsed(cellCode, entry);
                return BytesToBitmap(data, settings);
            }
        }

        // Cache miss: render fresh
        using var bitmap = _renderer.Render(cellCode, settings);
        var bytes = BitmapTo1BppBytes(bitmap);

        AddToL1(cellCode, bytes);
        SaveToDisk(cellCode, bytes);

        _logger.Info($"Cache MISS for '{cellCode}' — rendered and cached");
        return CloneBitmap(bitmap);
    }

    /// <summary>
    /// Полная очистка кэша (при смене настроек).
    /// </summary>
    public void Clear()
    {
        _l1Cache.Clear();
        while (_lruOrder.TryDequeue(out _)) { }
        _diskIndex.Clear();

        lock (_fileLock)
        {
            if (File.Exists(_cachePath))
                File.Delete(_cachePath);
        }

        _logger.Info("Label cache cleared (settings changed)");
    }

    /// <summary>
    /// Компактизация при закрытии приложения.
    /// Перезаписывает cache.bin без удалённых записей.
    /// </summary>
    public void CompactOnExit()
    {
        try
        {
            lock (_fileLock)
            {
                var validEntries = _diskIndex.ToList();
                if (validEntries.Count == 0) return;

                var tempPath = _cachePath + ".tmp";
                using var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write);
                using var writer = new BinaryWriter(fs);

                // Header
                writer.Write(Encoding.UTF8.GetBytes(MagicHeader));
                writer.Write(Encoding.UTF8.GetBytes(_currentSettingsHash));
                writer.Write(validEntries.Count);

                // Rebuild entries with new offsets
                var newIndex = new Dictionary<string, DiskEntry>();
                foreach (var kvp in validEntries)
                {
                    var data = ReadFromDiskRaw(kvp.Value);
                    if (data == null) continue;

                    var offset = fs.Position;
                    writer.Write((short)kvp.Key.Length);
                    writer.Write(Encoding.UTF8.GetBytes(kvp.Key));
                    writer.Write(data.Length);
                    writer.Write(data);

                    newIndex[kvp.Key] = new DiskEntry(offset, data.Length, kvp.Value.LastUsed);
                }

                writer.Flush();
                fs.Close();

                File.Move(tempPath, _cachePath, overwrite: true);

                // Update index
                _diskIndex.Clear();
                foreach (var kvp in newIndex)
                    _diskIndex[kvp.Key] = kvp.Value;

                _logger.Info($"Cache compacted: {newIndex.Count} entries");
            }
        }
        catch (Exception ex)
        {
            _logger.Error("Cache compaction failed", ex);
        }
    }

    #region Level 1 (RAM LRU)

    private void AddToL1(string cellCode, byte[] data)
    {
        _l1Cache[cellCode] = data;
        _lruOrder.Enqueue(cellCode);

        // Evict if over capacity
        while (_l1Cache.Count > Level1Capacity && _lruOrder.TryDequeue(out var oldest))
        {
            _l1Cache.TryRemove(oldest, out _);
        }
    }

    private void TouchLru(string cellCode)
    {
        // Простой touch: перезаписываем в очередь
        // Для 300 элементов это достаточно быстро
        _lruOrder.Enqueue(cellCode);
    }

    #endregion

    #region Level 2 (Disk)

    private void LoadDiskIndex()
    {
        try
        {
            if (!File.Exists(_cachePath)) return;

            using var fs = new FileStream(_cachePath, FileMode.Open, FileAccess.Read);
            using var reader = new BinaryReader(fs);

            var magic = Encoding.UTF8.GetString(reader.ReadBytes(MagicHeader.Length));
            if (magic != MagicHeader)
            {
                _logger.Info("Cache file has invalid magic header — ignoring");
                return;
            }

            var storedHash = Encoding.UTF8.GetString(reader.ReadBytes(64));
            if (storedHash != _currentSettingsHash)
            {
                _logger.Info("Cache settings hash mismatch — ignoring old cache");
                return;
            }

            var count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                var codeLen = reader.ReadInt16();
                var code = Encoding.UTF8.GetString(reader.ReadBytes(codeLen));
                var dataLen = reader.ReadInt32();
                var offset = fs.Position;
                fs.Seek(dataLen, SeekOrigin.Current); // Skip pixel data
                var lastUsed = DateTime.FromBinary(reader.ReadInt64());

                _diskIndex[code] = new DiskEntry(offset, dataLen, lastUsed);
            }

            _logger.Info($"Loaded cache index: {_diskIndex.Count} entries");
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to load cache index", ex);
        }
    }

    private byte[]? ReadFromDisk(DiskEntry entry)
    {
        lock (_fileLock)
        {
            try
            {
                using var fs = new FileStream(_cachePath, FileMode.Open, FileAccess.Read);
                fs.Seek(entry.Offset, SeekOrigin.Begin);
                var data = new byte[entry.Length];
                fs.Read(data, 0, entry.Length);
                return data;
            }
            catch
            {
                return null;
            }
        }
    }

    private byte[]? ReadFromDiskRaw(DiskEntry entry) => ReadFromDisk(entry);

    private void SaveToDisk(string cellCode, byte[] data)
    {
        lock (_fileLock)
        {
            try
            {
                // Rotация: если лимит превышен, удаляем старейшую запись из индекса
                if (_diskIndex.Count >= MaxEntries)
                {
                    var oldest = _diskIndex.OrderBy(x => x.Value.LastUsed).First();
                    _diskIndex.TryRemove(oldest.Key, out _);
                    // Физическое удаление произойдёт при компактизации
                }

                // Если файла нет — создаём с заголовком
                if (!File.Exists(_cachePath))
                {
                    using var createFs = new FileStream(_cachePath, FileMode.Create, FileAccess.Write);
                    using var headerWriter = new BinaryWriter(createFs);
                    headerWriter.Write(Encoding.UTF8.GetBytes(MagicHeader));
                    headerWriter.Write(Encoding.UTF8.GetBytes(_currentSettingsHash));
                    headerWriter.Write(0); // Count placeholder
                }

                // Дописываем в конец
                using var appendFs = new FileStream(_cachePath, FileMode.Open, FileAccess.Write);
                appendFs.Seek(0, SeekOrigin.End);
                var offset = appendFs.Position;

                using var writer = new BinaryWriter(appendFs);
                writer.Write((short)cellCode.Length);
                writer.Write(Encoding.UTF8.GetBytes(cellCode));
                writer.Write(data.Length);
                writer.Write(data);
                writer.Write(DateTime.UtcNow.ToBinary());

                _diskIndex[cellCode] = new DiskEntry(offset, data.Length, DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to save cache entry for '{cellCode}'", ex);
            }
        }
    }

    private void UpdateLastUsed(string cellCode, DiskEntry oldEntry)
    {
        _diskIndex[cellCode] = oldEntry with { LastUsed = DateTime.UtcNow };
    }

    #endregion

    #region 1bpp Conversion

    private static byte[] BitmapTo1BppBytes(Bitmap bmp)
    {
        using var ms = new MemoryStream();
        // Сохраняем как 1bpp BMP — самый быстрый формат для термопринтера
        bmp.Save(ms, ImageFormat.Bmp);
        return ms.ToArray();
    }

    private static Bitmap BytesToBitmap(byte[] data, PrintSettings settings)
    {
        // Загружаем из байтов (stream должен быть открыт пока жив tempBmp)
        using var ms = new MemoryStream(data);
        using var tempBmp = new Bitmap(ms);

        // Создаём копию в формате 32bpp ARGB — Graphics.FromImage работает с ним
        // Принтер сам сконвертирует в 1bpp при печати через DrawImage
        var result = new Bitmap(tempBmp.Width, tempBmp.Height, PixelFormat.Format32bppArgb);
        result.SetResolution(203f, 203f);

        using var g = Graphics.FromImage(result);
        g.DrawImage(tempBmp, 0, 0);

        return result;
    }

    private static Bitmap CloneBitmap(Bitmap source)
    {
        var clone = new Bitmap(source.Width, source.Height, source.PixelFormat);
        clone.SetResolution(source.HorizontalResolution, source.VerticalResolution);
        using var g = Graphics.FromImage(clone);
        g.DrawImage(source, 0, 0);
        return clone;
    }

    #endregion

    #region Settings Hash

    private static string ComputeSettingsHash(PrintSettings settings)
    {
        var json = JsonSerializer.Serialize(settings);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hash);
    }

    #endregion

    public void Dispose()
    {
        CompactOnExit();
    }
}