using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;

namespace ISO9660Lib.ISO9660FS;

/// <summary>
/// A top level class representing an entire ECMA-119 Filesystem
/// as described by ISO-9660 (Incomplete)
/// </summary>
public class ECMAFS
{
  /// <summary>
  /// What format the media's filesystem is in
  /// </summary>
  public DiskFormat MediaFormat { get; private set; } = DiskFormat.Unknown;

  /// <summary>
  /// If the disk uses raw sectors or logical sectors
  /// </summary>
  public bool RawSectors { get; private set; } = false;

  /// <summary>
  /// This flag will be set if an SVDs are present which indicate
  /// Joliet extensions roughly 100% of the time
  /// </summary>
  public bool IsJolietExtension { get; private set; } = false;

  /// <summary>
  /// The size of the header information in each sector, this only
  /// applies if the disk is a raw dump with header information.
  /// Its 24 bytes if the disk has a raw header on each sector
  /// </summary>
  public int HEADER_SIZE { get; private set; } = 0;

  /// <summary>
  /// The physical size of each sector, this is can change
  /// depending on if the dump is raw mode or logical mode.
  /// This is 2352 if the disk has a raw header and ECC
  /// </summary>
  public int SECTOR_SIZE { get; private set; } = 2048;

  /// <summary>
  /// A constant value used to identify a MODE 1
  /// disk header
  /// </summary>
  public readonly static byte[] CD001_HEADER =
  [
    0x01, 0x43, 0x44, 0x30, 0x30, 0x31
  ];

  /// <summary>
  /// The primary volume descriptor record for this filesystem
  /// </summary>
  public readonly MasterVolumeDescriptor PVD;

  /// <summary>
  /// The size of the file read to create this filesystem
  /// </summary>
  public readonly long FileSize;

  internal LogWriter? _logger;

  private readonly BinaryReader _backingData;
  private readonly Dictionary<uint, LogicalSector> _sectorCache = [];
  private readonly Dictionary<string, DataRecord?> _recordCache = [];

  /// <summary>
  /// Creates a new ECMAFS from a FileInfo on disk
  /// </summary>
  /// <param name="file">The file to use as the backing data for this ECMAFS</param>
  /// <param name="logSink">A stream to use the log output</param>
  /// <exception cref="FileNotFoundException"></exception>
  /// <exception cref="InvalidDataException"></exception>
  public ECMAFS(FileInfo file, Stream? logSink = null)
  {
    if (!file.Exists)
      throw new FileNotFoundException("Unable to read from file", file.Name);

    FileStream stream = new(file.FullName, FileMode.Open);
    _backingData = new(stream);
    FileSize = stream.Length;


    if (logSink is not null)
      _logger = new(logSink);

    MediaFormat = SetupMode();

    switch (MediaFormat)
    {
      default:
      case DiskFormat.Unknown:
        throw new InvalidDataException("Unable to determine the filesystem of the disk.");

      case DiskFormat.UDF:
        _logger?.LogMessage("Detected UDF filesystem on the disk.");
        throw new InvalidDataException("UDF format disks are currently not supported.");

      case DiskFormat.ISO9660:
        _logger?.LogMessage("Detected standard ISO 9660 (classic CD-ROM) filesystem.");
        break;
    }

    var currentSector = 16u;

    while (true)
    {
      if (!TryGetSectorRaw(currentSector, out var sector))
        throw new InvalidDataException($"Unable to read volume descriptor in sector {currentSector}");

      VolumeDescriptor vd = VolumeDescriptor.FromSector(sector, this);

      if (vd.DescriptorType == VolumeDescriptorType.VolumeDescriptorTerminator)
        break;
      else if (vd is PrimaryVolumeDescriptor pvd)
      {
        PVD = pvd;
        _logger?.LogMessage("Assigned PVD from pvd");
      }
      else if (vd is SupplementalVolumeDescriptor svd)
      {
        PVD = svd;
        IsJolietExtension = true;
        _logger?.LogMessage("Assigned PVD from svd");
      }

      currentSector++;
    }

    if (PVD is null)
      throw new InvalidOperationException("Read all volume descriptors and did not assign a PVD");
  }

  internal DiskFormat SetupMode()
  {
    //Start in logical mode
    //Logical mode is smaller so if we can't reach sector 16 the ECMA is damaged
    //or the file isn't a disk at all
    if (!TryGetSectorRaw(16, out var _sector))
      return DiskFormat.Unknown;

    static bool Valid9660Header(BinaryReader reader)
    {
      return reader.ReadBytes(6).SequenceEqual(CD001_HEADER);
    }

    static bool ValidUDFHeader(BinaryReader reader)
    {
      //skip checksum, etc
      reader.ReadBytes(4);
      string identifier = Encoding.ASCII.GetString(reader.ReadBytes(24)).TrimEnd();

      return identifier switch
      {
        "BEA01" or "NSR02" or "NSR03" or "TEA01" => true,
        _ => false,
      };
    }

    using var logicalHeader = _sector;

    if (Valid9660Header(logicalHeader.Reader))
      return DiskFormat.ISO9660;

    logicalHeader.Reader.BaseStream.Seek(0, SeekOrigin.Begin);

    if (ValidUDFHeader(logicalHeader.Reader))
      return DiskFormat.UDF;

    _logger?.LogMessage("Attempting to read disk in raw mode");

    HEADER_SIZE = 24;
    SECTOR_SIZE = 2352;

    if (!TryGetSectorRaw(16, out var _sector2))
      return DiskFormat.Unknown;

    using var rawHeader = _sector2;
    rawHeader.Reader.ReadBytes(HEADER_SIZE);

    if (Valid9660Header(rawHeader.Reader))
    {
      RawSectors = true;
      return DiskFormat.ISO9660;
    }

    rawHeader.Reader.BaseStream.Seek(0, SeekOrigin.Begin);
    rawHeader.Reader.ReadBytes(HEADER_SIZE);

    if (ValidUDFHeader(rawHeader.Reader))
    {
      RawSectors = true;
      return DiskFormat.UDF;
    }

    return DiskFormat.Unknown;
  }

  /// <summary>
  /// Gets a Data Record by its fully qualified identifier
  /// </summary>
  /// <param name="fullPath">The full path to the record</param>
  /// <returns>
  /// <em>DataRecord</em> if the item exists or
  /// <em>NULL</em> if it does not
  /// </returns>
  public DataRecord? GetRecordFromPath(string fullPath)
  {
    //the path MUST start at the root
    if (!fullPath.StartsWith(Path.DirectorySeparatorChar))
      return null;

    _logger?.LogMessage($"Retrieving: {fullPath}");

    //we always get the root automatically
    fullPath = fullPath.TrimStart(Path.DirectorySeparatorChar);

    if (fullPath == string.Empty)
      return PVD.RootRecord;

    if (_recordCache.TryGetValue(fullPath, out var cacheRecord))
    {
      _logger?.LogMessage("Cache hit");
      return cacheRecord;
    }

    string[] identifiers = fullPath.Split(Path.DirectorySeparatorChar);
    DataRecord? previous = PVD.RootRecord;

    foreach (var item in identifiers)
    {
      if (previous is not DirectoryRecord dirItem)
      {
        previous = null;
        break;
      }

      previous = dirItem.GetChildRecord(item);
    }

    _recordCache.TryAdd(fullPath, previous);
    return previous;
  }

  /// <summary>
  /// Creates or returns a cached logical sector by index and size
  /// </summary>
  /// <param name="sector">The index of the sector to retrieve</param>
  /// <param name="size">The size of the sector's extent</param>
  /// <param name="parent">The data record that owns this logical sector</param>
  /// <returns></returns>
  /// <exception cref="InvalidDataException"></exception>
  public LogicalSector GetSectorLogical(uint sector, uint size, DataRecord? parent = null)
  {
    uint sectorsOccupied = (size + PVD.LogicalBlockSize - 1) / PVD.LogicalBlockSize;

    if (_sectorCache.TryGetValue(sector, out var data))
    {
      _logger?.LogMessage($"Retrieving logical sector #{sector} from cache");

      if (data.Parent != parent)
        _logger?.LogError($"Sector #{sector} has multiple parents. ISO9660Lib does not yet support this behavior. This may cause further errors when traversing the filesystem upwards!\nClaimants:\n  {data.Parent}\n  {parent}");

      return data;
    }

    _logger?.LogMessage($"Retrieving new logical sector #{sector}");
    LogicalSector sec = new(sector, sectorsOccupied, this, parent);
    _sectorCache.Add(sector, sec);

    return sec;
  }

  /// <summary>
  /// Tries to fetch the given raw sector from this ECMAFS
  /// </summary>
  /// <param name="sector">Which sector to fetch</param>
  /// <param name="output">A RawSector containing a complete physical sector</param>
  /// <returns>
  /// <em>TRUE</em> if the sector was successfully located and read into a binary reader<br />
  /// <em>FALSE</em> for any failure
  /// </returns>
  /// <remarks>
  /// The out RawSector will be null in the event a <em>FALSE</em> value is returned.
  /// </remarks>
  public bool TryGetSectorRaw(uint sector, [NotNullWhen(true)] out PhysicalSector? output)
  {
    output = null;
    var location = sector * SECTOR_SIZE;
    _logger?.LogMessage($"Retrieving sector #{sector:N0}");

    if (location + SECTOR_SIZE > FileSize)
    {
      _logger?.LogError($"Sector is out of bounds: {location:N0} / {FileSize:N0}");
      return false;
    }


    _backingData.BaseStream.Seek(location, SeekOrigin.Begin);

    MemoryStream ms = new(_backingData.ReadBytes(SECTOR_SIZE));
    output = new(this, sector, new(ms));
    return true;
  }
}
