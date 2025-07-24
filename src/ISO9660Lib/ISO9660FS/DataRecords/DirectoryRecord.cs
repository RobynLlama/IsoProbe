using System.Linq;
using System.Text;

namespace ISO9660Lib.ISO9660FS;

/// <summary>
/// A type of DataRecord that stores information exclusively about
/// directories
/// </summary>
public class DirectoryRecord : DataRecord
{
  internal DirectoryRecord(uint locationOfExtent, uint dataLength, int flags, string identifier, LogicalSector? containedBy, ExtendedAttributeRecord? ear, ECMAFS owner) : base(locationOfExtent, dataLength, flags, identifier, containedBy, ear, owner)
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
    return ExtentSector.GetDirectoryContents().Where(item => item.Identifier == identifier).FirstOrDefault();
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

    var items = ExtentSector.GetDirectoryContents();

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
