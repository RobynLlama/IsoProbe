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
  public readonly int LocationOfExtent;

  /// <summary>
  /// The total length (file size) of the record's data. If it is
  /// greater than the LogicalBlockSize of the <see cref="PrimaryVolumeDescriptor"/>
  /// of the ECMAFS that owns it then the data will be spread across
  /// multiple logical blocks
  /// </summary>
  public readonly int DataLength;

  /// <summary>
  /// Whether or not this record represents a directory
  /// </summary>
  public readonly bool FlagIsDirectory;

  /// <summary>
  /// The identifier for this record, eg: the file or
  /// directory name
  /// </summary>
  public readonly string Identifier;

  public LogicalSector OwningSector
  {
    get
    {
      _owningSector ??= Owner.GetSectorLogical(LocationOfExtent, DataLength);
      return _owningSector;
    }
  }

  public readonly ECMAFS Owner;

  private LogicalSector? _owningSector;

  internal DataRecord(
    int locationOfExtent,
    int dataLength,
    int flags,
    string identifier,
    ECMAFS owner
  )
  {
    LocationOfExtent = locationOfExtent;
    DataLength = dataLength;
    Identifier = identifier;
    FlagIsDirectory = (flags & 0x02) != 0;
    Owner = owner;
  }

  internal static DataRecord FromBytes(byte[] data, ECMAFS owner)
  {
    using MemoryStream ms = new(data);
    using BinaryReader reader = new(ms);

    byte extAttrLength = reader.ReadByte();

    if (extAttrLength > 0)
    {
      //Skip Extended Attribute Record bytes if present
      reader.ReadBytes(extAttrLength);
    }

    int extentLocation = reader.ReadInt32();
    reader.ReadInt32();
    int dataLength = reader.ReadInt32();
    reader.ReadInt32();

    //skip timestamp for now
    reader.ReadBytes(7);

    int flags = reader.ReadByte();

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

    identifier ??= Encoding.ASCII.GetString(reader.ReadBytes(length));

    return new(extentLocation, dataLength, flags, identifier, owner);
  }
}
