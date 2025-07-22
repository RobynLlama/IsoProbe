using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

namespace ISO9660Lib.ISO9660FS;

/// <summary>
/// A top level class representing an entire ECMA-119 Filesystem
/// as described by ISo-9660 (Incomplete)
/// </summary>
public class ECMAFS
{
  /// <summary>
  /// The size of the header information in each sector
  /// </summary>
  public const int HEADER_SIZE = 24;

  /// <summary>
  /// The physical size of each sector, this is
  /// defined within the standard and never changes
  /// </summary>
  public const int SECTOR_SIZE = 2352;

  /// <summary>
  /// The primary volume descriptor record for this filesystem
  /// </summary>
  public readonly PrimaryVolumeDescriptor PVD;

  /// <summary>
  /// The total number of sectors in this filesystem
  /// </summary>
  public readonly int SectorCount;

  /// <summary>
  /// The size of the file read to create this filesystem
  /// </summary>
  public readonly long FileSize;

  private readonly BinaryReader _backingData;
  private readonly Dictionary<int, LogicalSector> _sectorCache = [];

  /// <summary>
  /// Creates a new ECMAFS from a FileInfo on disk
  /// </summary>
  /// <param name="file">The file to use as the backing data for this ECMAFS</param>
  /// <exception cref="FileNotFoundException"></exception>
  /// <exception cref="InvalidDataException"></exception>
  public ECMAFS(FileInfo file)
  {
    if (!file.Exists)
      throw new FileNotFoundException("Unable to read from file", file.Name);

    FileStream stream = new(file.FullName, FileMode.Open);
    _backingData = new(stream);
    FileSize = stream.Length;

    PVD = GetPrimaryVolumeDescriptor();
    SectorCount = PVD.LogicalBlockCount;
  }

  /// <summary>
  /// Creates a new ECMAFS from a file on disk
  /// </summary>
  /// <param name="fileName">The name of the file to use as backing data</param>
  public ECMAFS(string fileName) : this(new FileInfo(fileName)) { }

  internal PrimaryVolumeDescriptor GetPrimaryVolumeDescriptor()
  {
    if (!TryGetSectorRaw(16, out var _sector))
      throw new InvalidDataException($"Unable to fetch sector 16, ECMAFS is invalid or damaged!");

    using var sector = _sector;

    BinaryReader reader = sector.Reader;

    //discard the header
    reader.ReadBytes(HEADER_SIZE);

    int vType = reader.ReadByte();
    string ident = Encoding.ASCII.GetString(reader.ReadBytes(5));
    int version = reader.ReadByte();

    if (vType != 1)
      throw new InvalidDataException($"Expected a PVD record (0x01) at sector 16, type: {vType}, unable to continue!");

    if (!ident.Equals("cd001", StringComparison.InvariantCultureIgnoreCase))
      throw new InvalidDataException($"This application only supports properly formatted ISO-9660 volumes. Expected \"CD001\" Identifier: {ident}");

    //skip the reserved byte
    //SHOULD always be 0x00 but we're ignoring it for now
    reader.ReadByte();

    string systemID = Encoding.ASCII.GetString(reader.ReadBytes(32)).Trim();
    string volumeID = Encoding.ASCII.GetString(reader.ReadBytes(32)).Trim();

    //skip 8 unused bytes
    reader.ReadBytes(8);

    int logicalBlocks = reader.ReadInt32();
    //skip the second half of the both-endian block
    reader.ReadInt32();

    //skip the escape sequences block
    reader.ReadBytes(32);

    int volumeSetSize = reader.ReadInt16();
    //skip
    reader.ReadInt16();

    int volumeSequenceNo = reader.ReadInt16();
    //skip
    reader.ReadInt16();

    int logicalBlockSize = reader.ReadInt16();
    //skip
    reader.ReadInt16();

    int pathTableSize = reader.ReadInt16();
    //skip
    reader.ReadInt16();

    //skip the type L and M path tables for now
    reader.ReadBytes(16);

    //skip more bytes??
    reader.ReadBytes(4);

    int dirLength = reader.ReadByte() - 1;
    //Console.WriteLine($"Parsing {dirLength} bytes into root record");

    DataRecord root = DataRecord.FromBytes(reader.ReadBytes(dirLength), this);

    return new(version, systemID, volumeID, logicalBlocks, logicalBlockSize, volumeSetSize, volumeSequenceNo, pathTableSize, root, this);
  }

  /// <summary>
  /// Creates or returns a cached logical sector by index and size
  /// </summary>
  /// <param name="sector">The index of the sector to retrieve</param>
  /// <param name="size">The size of the sector's extent</param>
  /// <returns></returns>
  /// <exception cref="InvalidDataException"></exception>
  public LogicalSector GetSectorLogical(int sector, int size)
  {
    int sectorsOccupied = (size + PVD.LogicalBlockSize - 1) / PVD.LogicalBlockSize;

    if (_sectorCache.TryGetValue(sector, out var data))
      return data;

    LogicalSector sec = new(sector, sectorsOccupied, this);
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
  public bool TryGetSectorRaw(int sector, [NotNullWhen(true)] out PhysicalSector? output)
  {
    output = null;
    var location = sector * SECTOR_SIZE;

    if (location + SECTOR_SIZE > FileSize)
      return false;

    _backingData.BaseStream.Seek(location, SeekOrigin.Begin);

    MemoryStream ms = new(_backingData.ReadBytes(SECTOR_SIZE));
    output = new(this, sector, new(ms));
    return true;
  }
}
