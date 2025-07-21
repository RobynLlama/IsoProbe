using System;
using System.Collections.Generic;
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
  /// The length of the record's extended attributes.<br />
  /// NOTE: The actual Extended Attributes are not stored nor retrieved yet
  /// </summary>
  public readonly int ExtendedAttributesLength;

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

  /// <summary>
  /// Only used by directory records. <br />
  /// A cached list of every record this directory contains.
  /// See <see cref="BuildDirectory"/> for building this cache as
  /// it is not done unless requested
  /// </summary>
  public readonly List<DataRecord> ContainedItems = [];

  /// <summary>
  /// If this record is contained by a directory record this
  /// field will point to it for easy traversal.
  /// </summary>
  public DataRecord? Parent = null;

  /// <summary>
  /// A reference to the ECMAFS this record is contained within
  /// </summary>
  public ECMAFS Owner;

  private byte[] _contents = [];
  private bool _contentsFetched = false;

  internal DataRecord(
    int extendedAttributesLength,
    int locationOfExtent,
    int dataLength,
    int flags,
    string identifier,
    ECMAFS owner,
    DataRecord? parent = null
    )
  {
    ExtendedAttributesLength = extendedAttributesLength;
    LocationOfExtent = locationOfExtent;
    DataLength = dataLength;
    Identifier = identifier;
    FlagIsDirectory = (flags & 0x02) != 0;
    Owner = owner;
    Parent = parent;
  }

  /// <summary>
  /// Updates the <seealso cref="ContainedItems"/> cache for this
  /// directory record.
  /// </summary>
  /// <exception cref="InvalidDataException"></exception>
  public void BuildDirectory()
  {
    if (!FlagIsDirectory)
      return;

    //Console.WriteLine($"Building: {Identifier}");
    if (!Owner.TryGetSector(LocationOfExtent, out var reader))
      throw new InvalidDataException($"Unable to read sector #{LocationOfExtent}");

    //skip out the header
    reader.ReadBytes(ECMAFS.HEADER_SIZE);
    var finalSize = ECMAFS.HEADER_SIZE + DataLength;

    while (reader.BaseStream.Position < finalSize)
    {
      var length = reader.ReadByte() - 1;
      //Console.WriteLine($"Reading a record, length {length}");

      if (length <= 0)
        break;

      var item = FromSector(new BinaryReader(new MemoryStream(reader.ReadBytes(length))), Owner);

      if (item.Identifier == "." || item.Identifier == "..")
        continue;

      item.Parent = this;
      ContainedItems.Add(item);

      if (item.FlagIsDirectory)
        item.BuildDirectory();

    }
  }

  /// <summary>
  /// Reads the contents from this record's extent.
  /// This is generally the file data, but for directories
  /// it will be the raw listing of all items contained within
  /// </summary>
  /// <returns></returns>
  /// <exception cref="InvalidDataException"></exception>
  public Byte[] GetContents()
  {
    if (_contentsFetched)
      return _contents;

    var dataToRead = DataLength;
    var currentSector = LocationOfExtent;
    _contents = new byte[DataLength];

    using MemoryStream cms = new(_contents);

    while (dataToRead > 0)
    {
      if (!Owner.TryGetSector(LocationOfExtent, out var reader))
        throw new InvalidDataException($"Unable to read sector #{LocationOfExtent}");

      //skip header
      reader.ReadBytes(ECMAFS.HEADER_SIZE);

      var nextRead = dataToRead;

      //TODO: make this variable based on sector size in the file
      if (nextRead > 2048)
      {
        nextRead = 2048;
        currentSector++;
      }

      dataToRead -= nextRead;

      cms.Write(reader.ReadBytes(nextRead));
    }

    _contentsFetched = true;

    return _contents;
  }

  /// <summary>
  /// This method dumps the raw contents of the record's extents
  /// to disk under the given basePath, respecting the file's full
  /// path in the ECMAFS and identifier
  /// </summary>
  /// <param name="basePath">The root directory for the dump</param>
  public void DumpContents(string basePath)
  {
    if (!_contentsFetched)
      GetContents();

    if (FlagIsDirectory)
    {
      basePath = Path.Combine(basePath, Identifier);
      DirectoryInfo info = new(basePath);

      if (!info.Exists)
        info.Create();

      foreach (var item in ContainedItems)
        item.DumpContents(basePath);
    }
    else
    {
      //Console.WriteLine($"Dumping {Identifier}");

      using FileStream output = new(Path.Combine(basePath, Identifier), FileMode.OpenOrCreate);
      output.Write(_contents);
      output.Flush();
    }
  }

  /// <summary>
  /// Adds a listing of all files under this directory recursively
  /// to a given StringBuilder
  /// </summary>
  /// <param name="sb">The StringBuilder to add to</param>
  /// <param name="indent">The starting Indentation level for pretty-printing</param>
  public void DumpDirectory(StringBuilder sb, int indent)
  {
    string indent_char = new(' ', indent);

    foreach (var item in ContainedItems)
    {
      sb.Append(indent_char);
      sb.Append(item.GetFullPath());

      if (item.FlagIsDirectory)
      {
        sb.Append(" [");
        sb.Append(item.ContainedItems.Count);
        sb.AppendLine(" items]");
        item.DumpDirectory(sb, indent + 1);
      }
      else
      {
        sb.Append(" [");
        if (item.DataLength < 1024)
        {
          sb.Append(item.DataLength);
          sb.AppendLine(" b]");
        }
        else
        {
          sb.Append((item.DataLength / 1024.0).ToString("F1"));
          sb.AppendLine(" kb]");
        }

      }
    }
  }

  /// <summary>
  /// Builds the fully qualified path of this record recursively
  /// </summary>
  /// <param name="currentPath">Used for recursion and should be omitted when called</param>
  /// <returns>The fully qualified path of this record</returns>
  public string GetFullPath(string currentPath = "")
  {
    if (currentPath == "")
      currentPath = Identifier;
    else
      currentPath = Path.Combine(Identifier, currentPath);

    if (Parent is not null)
      return Parent.GetFullPath(currentPath);

    return currentPath;
  }

  /// <summary>
  /// Creates a DataRecord from a BinaryReader containing a sector
  /// </summary>
  /// <param name="reader">A BinaryReader containing exactly one sector</param>
  /// <param name="owner">The ECMAFS that owns this sector and record</param>
  /// <returns></returns>
  public static DataRecord FromSector(BinaryReader reader, ECMAFS owner)
  {
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

    return new(extAttrLength, extentLocation, dataLength, flags, identifier, owner);
  }
}
