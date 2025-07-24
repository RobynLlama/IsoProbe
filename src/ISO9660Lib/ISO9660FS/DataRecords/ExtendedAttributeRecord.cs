namespace ISO9660Lib.ISO9660FS;

/// <summary>
/// A type of DataRecord that stores extended attribute data about
/// the record directly following it (incomplete)
/// </summary>
public class ExtendedAttributeRecord : DataRecord
{
  internal ExtendedAttributeRecord(uint locationOfExtent, uint dataLength, string identifier, LogicalSector? containedBy, ECMAFS owner) : base(locationOfExtent, dataLength, 0x00, identifier, containedBy, null, owner)
  {
  }
}
