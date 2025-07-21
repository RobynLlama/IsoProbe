using System.Diagnostics.CodeAnalysis;
using System.IO;

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

    if (!TryGetSector(16, out var reader))
      throw new InvalidDataException("The provided file is not a valid ECMA-119/ISO-9660 filesystem: missing sector 16 (Primary Volume Descriptor).");

    if (PrimaryVolumeDescriptor.FromSector(reader, this) is not PrimaryVolumeDescriptor pvd)
      throw new InvalidDataException("Unable to parse sector 16 as a valid Primary Volume Description");

    PVD = pvd;
    SectorCount = PVD.LogicalBlockCount;
  }

  /// <summary>
  /// Creates a new ECMAFS from a file on disk
  /// </summary>
  /// <param name="fileName">The name of the file to use as backing data</param>
  public ECMAFS(string fileName) : this(new FileInfo(fileName)) { }

  /// <summary>
  /// Tries to fetch the given sector from this ECMAFS
  /// </summary>
  /// <param name="sector">Which sector to fetch</param>
  /// <param name="output">A BinaryReader containing a complete physical sector</param>
  /// <returns>
  /// <em>TRUE</em> if the sector was successfully located and read into a binary reader<br />
  /// <em>FALSE</em> for any failure
  /// </returns>
  /// <remarks>
  /// The out BinaryReader will be null in the event a <em>FALSE</em> value is returned.
  /// </remarks>
  public bool TryGetSector(int sector, [NotNullWhen(true)] out BinaryReader? output)
  {
    output = null;
    var location = sector * SECTOR_SIZE;

    if (location + SECTOR_SIZE > FileSize)
      return false;

    _backingData.BaseStream.Seek(location, SeekOrigin.Begin);

    MemoryStream ms = new(_backingData.ReadBytes(SECTOR_SIZE));
    output = new(ms);
    return true;
  }
}
