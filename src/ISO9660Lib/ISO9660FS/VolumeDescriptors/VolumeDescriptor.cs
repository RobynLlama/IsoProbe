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

  internal static VolumeDescriptor FromSector(BinaryReader sector, ECMAFS owner)
  {
    //return the reader to the beginning
    sector.BaseStream.Seek(0, SeekOrigin.Begin);

    //skip the header if there is one
    sector.ReadBytes(owner.HEADER_SIZE);

    //read the record type
    int recordType = sector.ReadByte();

    //skip the CD001 header
    sector.ReadBytes(5);

    //version
    int version = sector.ReadByte();

    owner._logger?.LogMessage($"\n Descriptor type: {recordType}\n Descriptor Version: {version}");

    if (recordType != 1 && recordType != 2)
      return new VolumeDescriptor((VolumeDescriptorType)recordType);

    //skip the reserved byte
    //SHOULD always be 0x00 but we're ignoring it for now
    sector.ReadByte();

    string systemID = Encoding.ASCII.GetString(sector.ReadBytes(32)).Trim();
    string volumeID = Encoding.ASCII.GetString(sector.ReadBytes(32)).Trim();

    //skip 8 unused bytes
    sector.ReadBytes(8);

    uint logicalBlocks = sector.ReadUInt32();
    //skip the second half of the both-endian block
    sector.ReadInt32();

    //skip the escape sequences block
    sector.ReadBytes(32);

    uint volumeSetSize = sector.ReadUInt16();
    //skip
    sector.ReadInt16();

    uint volumeSequenceNo = sector.ReadUInt16();
    //skip
    sector.ReadInt16();

    uint logicalBlockSize = sector.ReadUInt16();
    //skip
    sector.ReadInt16();

    uint pathTableSize = sector.ReadUInt32();
    //skip
    sector.ReadInt32();

    uint pathTableL = sector.ReadUInt32();
    uint pathTableLOpt = sector.ReadUInt32();

    //skip pathTableM
    sector.ReadBytes(8);

    var dirLength = sector.ReadByte() - 1;
    //Console.WriteLine($"Parsing {dirLength} bytes into root record");

    byte[] rootData = sector.ReadBytes(dirLength);

    using MemoryStream rms = new(rootData);
    using BinaryReader rootReader = new(rms);

    rootReader.ReadByte();
    uint rootExtentLocation = rootReader.ReadUInt32();
    rootReader.ReadUInt32();
    uint rootDataLength = rootReader.ReadUInt32();
    rootReader.ReadUInt32();

    var volumeSetID = Encoding.ASCII.GetString(sector.ReadBytes(128));
    var publisherID = Encoding.ASCII.GetString(sector.ReadBytes(128));
    var preparerID = Encoding.ASCII.GetString(sector.ReadBytes(128));
    var applicationID = Encoding.ASCII.GetString(sector.ReadBytes(128));

    var copyrightFile = Encoding.ASCII.GetString(sector.ReadBytes(37));
    var abstractFile = Encoding.ASCII.GetString(sector.ReadBytes(37));
    var biblioFile = Encoding.ASCII.GetString(sector.ReadBytes(37));

    //skip 4 timestamps
    sector.ReadBytes(17);
    sector.ReadBytes(17);
    sector.ReadBytes(17);
    sector.ReadBytes(17);

    //skip version and structure byte (always 0x01 0x00)
    sector.ReadBytes(2);

    var rootRecord = new DirectoryRecord(rootExtentLocation, rootDataLength, 0x02, ".", null, null, owner);
    var pathTable = new DirectoryRecord(pathTableL, pathTableSize, 0, "PathTable", null, null, owner);

    if (recordType == 1)
      return new PrimaryVolumeDescriptor(version, systemID, volumeID, logicalBlocks, logicalBlockSize, volumeSetSize, volumeSequenceNo, rootRecord, pathTable, owner);
    else
      return new SupplementalVolumeDescriptor(version, systemID, volumeID, logicalBlocks, logicalBlockSize, volumeSetSize, volumeSequenceNo, rootRecord, pathTable, owner);
  }
}
