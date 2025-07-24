using System.IO;
using System.Text;

namespace ISO9660Lib.ISO9660FS;

/// <summary>
/// The most basic form of volume description
/// </summary>
public class VolumeDescriptor
{
  /// <summary>
  /// What type of descriptor this volume descriptor is
  /// </summary>
  public VolumeDescriptorType DescriptorType;

  internal VolumeDescriptor(VolumeDescriptorType type)
  {
    DescriptorType = type;
  }

  internal static VolumeDescriptor FromSector(PhysicalSector sector, ECMAFS owner)
  {
    //return the reader to the beginning
    var reader = sector.Reader;
    reader.BaseStream.Seek(0, SeekOrigin.Begin);

    //skip the header if there is one
    reader.ReadBytes(owner.HEADER_SIZE);

    //read the record type
    int recordType = reader.ReadByte();

    //skip the CD001 header
    reader.ReadBytes(5);

    //version
    int version = reader.ReadByte();

    owner._logger?.LogMessage($"\n Descriptor type: {recordType}\n Descriptor Version: {version}");

    if (recordType != 1 && recordType != 2)
      return new VolumeDescriptor((VolumeDescriptorType)recordType);

    //skip the reserved byte
    //SHOULD always be 0x00 but we're ignoring it for now
    reader.ReadByte();

    string systemID = Encoding.ASCII.GetString(reader.ReadBytes(32)).Trim();
    string volumeID = Encoding.ASCII.GetString(reader.ReadBytes(32)).Trim();

    //skip 8 unused bytes
    reader.ReadBytes(8);

    uint logicalBlocks = reader.ReadUInt32();
    //skip the second half of the both-endian block
    reader.ReadInt32();

    //skip the escape sequences block
    reader.ReadBytes(32);

    uint volumeSetSize = reader.ReadUInt16();
    //skip
    reader.ReadInt16();

    uint volumeSequenceNo = reader.ReadUInt16();
    //skip
    reader.ReadInt16();

    uint logicalBlockSize = reader.ReadUInt16();
    //skip
    reader.ReadInt16();

    uint pathTableSize = reader.ReadUInt32();
    //skip
    reader.ReadInt32();

    uint pathTableL = reader.ReadUInt32();
    uint pathTableLOpt = reader.ReadUInt32();

    //skip pathTableM
    reader.ReadBytes(8);

    var dirLength = reader.ReadByte() - 1;
    //Console.WriteLine($"Parsing {dirLength} bytes into root record");

    byte[] rootData = reader.ReadBytes(dirLength);

    using MemoryStream rms = new(rootData);
    using BinaryReader rootReader = new(rms);

    rootReader.ReadByte();
    uint rootExtentLocation = rootReader.ReadUInt32();
    rootReader.ReadUInt32();
    uint rootDataLength = rootReader.ReadUInt32();
    rootReader.ReadUInt32();

    var volumeSetID = Encoding.ASCII.GetString(reader.ReadBytes(128));
    var publisherID = Encoding.ASCII.GetString(reader.ReadBytes(128));
    var preparerID = Encoding.ASCII.GetString(reader.ReadBytes(128));
    var applicationID = Encoding.ASCII.GetString(reader.ReadBytes(128));

    var copyrightFile = Encoding.ASCII.GetString(reader.ReadBytes(37));
    var abstractFile = Encoding.ASCII.GetString(reader.ReadBytes(37));
    var biblioFile = Encoding.ASCII.GetString(reader.ReadBytes(37));

    //skip 4 timestamps
    reader.ReadBytes(17);
    reader.ReadBytes(17);
    reader.ReadBytes(17);
    reader.ReadBytes(17);

    //skip version and structure byte (always 0x01 0x00)
    reader.ReadBytes(2);

    var rootRecord = new DirectoryRecord(rootExtentLocation, rootDataLength, 0x02, ".", null, null, owner);
    var pathTable = new DirectoryRecord(pathTableL, pathTableSize, 0, "PathTable", null, null, owner);

    if (recordType == 1)
      return new PrimaryVolumeDescriptor(version, systemID, volumeID, logicalBlocks, logicalBlockSize, volumeSetSize, volumeSequenceNo, rootRecord, pathTable, owner);
    else
      return new SupplementalVolumeDescriptor(version, systemID, volumeID, logicalBlocks, logicalBlockSize, volumeSetSize, volumeSequenceNo, rootRecord, pathTable, owner);
  }
}
