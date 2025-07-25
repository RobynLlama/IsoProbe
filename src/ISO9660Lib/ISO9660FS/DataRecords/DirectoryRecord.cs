using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ISO9660Lib.ISO9660FS;

/// <summary>
/// A type of DataRecord that stores information exclusively about
/// directories
/// </summary>
public class DirectoryRecord : DataRecord
{
  internal DirectoryRecord(uint locationOfExtent, uint dataLength, int flags, string identifier, DirectoryRecord? containedBy, ExtendedAttributeRecord? ear, ECMAFS owner) : base(locationOfExtent, dataLength, flags, identifier, containedBy, ear, owner)
  {
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
    return GetDirectoryContent().Where(item => item.Identifier == identifier).FirstOrDefault();
  }

  /// <summary>
  /// Reads the contents of the extent owned by this
  /// Directory Record and parses it as DataRecords
  /// </summary>
  /// <returns></returns>
  public IEnumerable<DataRecord> GetDirectoryContent()
  {
    if (FlagIsMultiExtent)
    {
      Owner._logger?.LogMessage("Multi-extent files are not supported yet");
      yield break;
    }

    byte[] buffer = new byte[Owner.SECTOR_SIZE];
    using MemoryStream ms = new(buffer);
    using BinaryReader reader = new(ms);

    foreach (var data in GetFileContentsSectors(buffer))
    {
      ms.Seek(0, SeekOrigin.Begin);
      while (ms.Position < data)
      {
        int size = reader.ReadByte() - 1;

        if (size < 1)
          break;

        byte[] recordData = reader.ReadBytes(size);
        var record = FromBytes(recordData, this, Owner);

        yield return record;
      }
    }
  }

  /// <summary>
  /// Dumps the file listing of the record onto a StringBuilder for easy viewing
  /// </summary>
  /// <param name="sb">The StringBuilder to use</param>
  /// <param name="indent">How far indented each item should be</param>
  /// <param name="recursive">If we should descend into any discovered directories</param>
  public void DumpFileListing(StringBuilder sb, int indent, bool recursive = true)
  {
    string padding = new(' ', indent * 2);

    var items = GetDirectoryContent();

    sb.Append(padding);
    sb.AppendLine(ToString());

    foreach (var item in items)
    {
      if (item.Identifier == "." || item.Identifier == "..")
        continue;

      if (item is DirectoryRecord dirItem && recursive)
      {
        //don't print anything just recurse
        dirItem.DumpFileListing(sb, indent + 1, true);
        continue;
      }

      sb.Append(padding);
      sb.Append(padding);
      sb.AppendLine(item.ToString());
    }
  }
}
