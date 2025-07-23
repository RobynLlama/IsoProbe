using System.IO;
using System.Linq;
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
  /// The identifier for this record, eg: the file or
  /// directory name
  /// </summary>
  public readonly string Identifier;

  /// <summary>
  /// The sector that this record belongs to. Note that
  /// it is initialized lazily and may throw if it is invalid
  /// </summary>
  public LogicalSector OwningSector
  {
    get
    {
      _owningSector ??= Owner.GetSectorLogical(LocationOfExtent, DataLength, this);
      return _owningSector;
    }
  }

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
    ECMAFS owner
  )
  {
    LocationOfExtent = locationOfExtent;
    DataLength = dataLength;
    Identifier = identifier;
    FlagIsDirectory = (flags & 0x02) != 0;
    Owner = owner;
  }

  /// <summary>
  /// Returns a child record by exact identifier
  /// </summary>
  /// <param name="identifier">The identifier of the child record</param>
  /// <returns>
  /// <em>DataRecord</em> if one exists under that identifier or
  /// <em>NULL</em> otherwise
  /// </returns>
  public DataRecord? GetChildRecord(string identifier)
  {
    if (!FlagIsDirectory)
      return null;

    return OwningSector.GetDirectoryContents().Where(item => item.Identifier == identifier).FirstOrDefault();
  }

  /// <summary>
  /// Dumps the file listing of the record onto a StringBuilder for easy viewing
  /// </summary>
  /// <param name="sb">The StringBuilder to use</param>
  /// <param name="indent">How far indented each item should be</param>
  /// <param name="recursive">If we should descend into any discovered directories</param>
  public void DumpFileListing(StringBuilder sb, int indent, bool recursive = true)
  {
    if (!FlagIsDirectory)
      return;

    string padding = new(' ', indent * 2);

    var items = OwningSector.GetDirectoryContents();

    sb.Append(padding);
    sb.AppendLine(ToString());

    foreach (var item in items)
    {
      if (item.Identifier == "." || item.Identifier == "..")
        continue;

      if (item.FlagIsDirectory && recursive)
      {
        //don't print anything just recurse
        item.DumpFileListing(sb, indent + 1, true);
        continue;
      }

      sb.Append(padding);
      sb.Append(padding);
      sb.AppendLine(item.ToString());
    }
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

    uint extentLocation = reader.ReadUInt32();
    reader.ReadUInt32();
    uint dataLength = reader.ReadUInt32();
    reader.ReadUInt32();

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
      return $"{padID} [{OwningSector.GetDirectoryContents().Count} items]";
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
