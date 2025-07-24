using System.IO;
using System.Text;

namespace ISO9660Lib.ISO9660FS;

/// <summary>
/// A single record in the filesystem, can be a directory or a file
/// or part of a multi-part record
/// </summary>
public class DataRecord
{
  /// <summary>
  /// This the sector ID of the first record containing this
  /// record's actual data
  /// </summary>
  public readonly uint LocationOfExtent;

  /// <summary>
  /// The total length (file size) of the record's data. If it is
  /// greater than the LogicalBlockSize of the <see cref="PrimaryVolumeDescriptor"/>
  /// of the ECMAFS that owns it then the data will be spread across
  /// multiple logical blocks
  /// </summary>
  public readonly uint DataLength;

  /// <summary>
  /// Whether or not this record represents a directory
  /// </summary>
  public readonly bool FlagIsDirectory;

  /// <summary>
  /// Whether or not this record uses multiple extents
  /// for its contents. Used for large file support
  /// </summary>
  public readonly bool FlagIsMultiExtent;

  /// <summary>
  /// The identifier for this record, eg: the file or
  /// directory name without the version field
  /// </summary>
  public readonly string Identifier;

  /// <summary>
  /// The full identifier for this item leading all the way
  /// back up to the volume root. EG: "/SOUNDS/YAY.WAV"
  /// </summary>
  public readonly string FullyQualifiedIdentifier;

  /// <summary>
  /// The version field of the record's identifier, eg
  /// the part after the semicolon in "FILE.BIN;1"
  /// </summary>
  public readonly int RecordVersion;

  /// <summary>
  /// The sector that this record points to. Note that
  /// it is initialized lazily and may throw if it is invalid
  /// </summary>
  public LogicalSector ExtentSector
  {
    get
    {
      _owningSector ??= Owner.GetSectorLogical(LocationOfExtent, DataLength, this);
      return _owningSector;
    }
  }

  /// <summary>
  /// This is where extended record information is stored.
  /// It rarely exists
  /// </summary>
  public readonly ExtendedAttributeRecord? ExtendedAttributes;

  /// <summary>
  /// The sector that this record was defined in. Used
  /// for traversing up the file tree. Will be null on
  /// the volume root
  /// </summary>
  public readonly LogicalSector? ContainingSector;

  /// <summary>
  /// The ECMAFS that this record belongs to
  /// </summary>
  public readonly ECMAFS Owner;

  private LogicalSector? _owningSector;

  internal DataRecord(
    uint locationOfExtent,
    uint dataLength,
    int flags,
    string identifier,
    LogicalSector? containedBy,
    ExtendedAttributeRecord? ear,
    ECMAFS owner
  )
  {
    LocationOfExtent = locationOfExtent;
    DataLength = dataLength;
    FlagIsDirectory = (flags & 0x02) != 0;
    FlagIsMultiExtent = (flags & 0x20) != 0;
    Identifier = identifier;
    ContainingSector = containedBy;
    ExtendedAttributes = ear;
    Owner = owner;

    if (identifier.Contains(';'))
    {
      var semi = identifier.LastIndexOf(';');

      if (int.TryParse(identifier[semi..], out var _ver))
        RecordVersion = _ver;

      Identifier = identifier[..semi];
    }


    if (ContainingSector is null || ContainingSector.Parent is null)
      FullyQualifiedIdentifier = Identifier;
    else
    {
      var parentFQI = ContainingSector.Parent.FullyQualifiedIdentifier;
      FullyQualifiedIdentifier = Path.Combine(parentFQI, Identifier);
    }
  }

  /// <summary>
  /// Reads the contents from this record's extent.
  /// This is generally the file data, but for directories
  /// it will be the raw listing of all items contained within
  /// </summary>
  /// <returns></returns>
  /// <exception cref="InvalidDataException"></exception>
  public byte[] GetFileContents()
  {
    if (FlagIsMultiExtent)
    {
      Owner._logger?.LogMessage("Multi-extent files are not supported yet");
      return [];
    }

    return ExtentSector.GetFileContents();
  }

  internal static DataRecord FromBytes(byte[] data, LogicalSector? containingSector, ECMAFS owningFS)
  {
    using MemoryStream ms = new(data);
    using BinaryReader reader = new(ms);

    byte extAttrLength = reader.ReadByte();
    ExtendedAttributeRecord? ear = null;

    uint extentLocation = reader.ReadUInt32();
    reader.ReadUInt32();
    uint dataLength = reader.ReadUInt32();
    reader.ReadUInt32();

    //skip timestamp for now
    reader.ReadBytes(7);

    int flags = reader.ReadByte();
    bool isDir = (flags & 0x02) != 0;

    //skip interleave info for now
    reader.ReadBytes(2);

    //skip volume sequence info for now
    reader.ReadBytes(4);

    string? identifier = null;

    int length = reader.ReadByte();

    if (length == 1)
    {
      int rawID = reader.ReadByte();

      if (rawID == 0)
        identifier = ".";
      else if (rawID == 1)
        identifier = "..";
    }

    if (identifier is null)
    {
      byte[] rawId = reader.ReadBytes(length);

      if (owningFS.IsJolietExtension)
        identifier = Encoding.BigEndianUnicode.GetString(rawId);
      else
        identifier = Encoding.ASCII.GetString(rawId);
    }


    if (extAttrLength > 0)
    {
      var earSize = (extAttrLength + owningFS.PVD.LogicalBlockSize - 1) / owningFS.PVD.LogicalBlockSize;
      ear = new(extentLocation - earSize, extAttrLength, $"{identifier}-EAR", containingSector, owningFS);
    }

    if (isDir)
      return new DirectoryRecord(extentLocation, dataLength, flags, identifier, containingSector, ear, owningFS);

    return new(extentLocation, dataLength, flags, identifier, containingSector, ear, owningFS);
  }

  /// <summary>
  /// Returns a formatted string for the file that differs
  /// depending on if its a directory or file
  /// </summary>
  /// <returns></returns>
  public override string ToString()
  {
    string padID;

    if (FlagIsDirectory)
    {
      padID = Identifier.PadRight(9);
      return $"{padID} [{ExtentSector.GetDirectoryContents().Count} items]";
    }


    string sizeKB;

    if (DataLength > 1999)
      sizeKB = $"{DataLength / 1000f:N1} kb";
    else
      sizeKB = $"{DataLength:N0} b";

    padID = Identifier.PadRight(15);
    return $"{padID} [{sizeKB}]";
  }
}
