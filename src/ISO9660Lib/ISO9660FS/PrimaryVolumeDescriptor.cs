using System;
using System.IO;
using System.Text;

namespace ISO9660Lib.ISO9660FS;

/// <summary>
/// A special record that is always present on sector 16
/// that provides volume information for parsing the ECMAFS
/// </summary>
public class PrimaryVolumeDescriptor
{
  /// <summary>
  /// What version of the volume descriptor this ECMAFS was written with
  /// </summary>
  public readonly int VolumeDescriptorVersion;

  /// <summary>
  /// A string describing what system this ECMAFS is intended for use with
  /// </summary>
  public readonly string SystemID;

  /// <summary>
  /// A string description of the volume. Basically a friendly name to use
  /// </summary>
  public readonly string VolumeID;

  /// <summary>
  /// How many sectors total are in this ECMAFS
  /// </summary>
  public readonly int LogicalBlockCount;

  /// <summary>
  /// The size of the user data for each sector. See <seealso cref="ECMAFS.SECTOR_SIZE"/>
  /// for the constant physical size of each sector. All space on a sector not
  /// claimed by the header or user data is assumed to be error correcting codes
  /// </summary>
  public readonly int LogicalBlockSize;

  /// <summary>
  /// The number of total volumes in this ECMAFS volume set. Nearly always 1
  /// </summary>
  public readonly int VolumeSetSize;

  /// <summary>
  /// Which volume in the set this specific volume is. Nearly always 1
  /// </summary>
  public readonly int VolumeSetNumber;

  /// <summary>
  /// The size in bytes of the path table. The path table is currently
  /// unused in the ECMAFS implementation
  /// </summary>
  public readonly int PathTableSize;

  /// <summary>
  /// The data record of the Root record. The Root contains a listing of
  /// every directory and file on the root of the filesystem
  /// </summary>
  public readonly DataRecord RootRecord;

  /// <summary>
  /// A reference to the ECMAFS that owns this PVD
  /// </summary>
  public readonly ECMAFS Owner;

  internal PrimaryVolumeDescriptor(
    int volumeDescriptorVersion,
    string systemID,
    string volumeID,
    int logicalBlockCount,
    int logicalBlockSize,
    int volumeSetSize,
    int volumeSetNumber,
    int pathTableSize,
    DataRecord rootRecord,
    ECMAFS owner
    )
  {
    VolumeDescriptorVersion = volumeDescriptorVersion;
    SystemID = systemID;
    VolumeID = volumeID;
    LogicalBlockCount = logicalBlockCount;
    LogicalBlockSize = logicalBlockSize;
    VolumeSetSize = volumeSetSize;
    VolumeSetNumber = volumeSetNumber;
    PathTableSize = pathTableSize;
    RootRecord = rootRecord;
    Owner = owner;
  }

  /// <summary>
  /// Attempts to create a PrimaryVolumeDescriptor from a BinaryReader
  /// containing exactly one sector
  /// </summary>
  /// <param name="reader">The BinaryReader containing the PVD</param>
  /// <param name="owner">The ECMAFS that owns this PVD</param>
  /// <returns></returns>
  public static PrimaryVolumeDescriptor? FromSector(BinaryReader reader, ECMAFS owner)
  {
    //discard the header
    reader.ReadBytes(ECMAFS.HEADER_SIZE);

    int vType = reader.ReadByte();
    string ident = Encoding.ASCII.GetString(reader.ReadBytes(5));
    int version = reader.ReadByte();

    if (vType != 1)
    {
      Console.WriteLine($"Expected a PVD record (0x01) at sector 16, type: {vType}, unable to continue!");
      return null;
    }

    if (!ident.Equals("cd001", StringComparison.InvariantCultureIgnoreCase))
    {
      Console.WriteLine($"This application only supports properly formatted ISO-9660 volumes. Expected \"CD001\" Identifier: {ident}");
      return null;
    }

    //skip the reserved byte
    //SHOULD always be 0x00 but we're ignoring it for now
    reader.ReadByte();

    string systemID = Encoding.ASCII.GetString(reader.ReadBytes(32)).Trim();
    string volumeID = Encoding.ASCII.GetString(reader.ReadBytes(32)).Trim();

    //skip 8 unused bytes
    reader.ReadBytes(8);

    int logicalBlocks = reader.ReadInt32();
    //skip the second half of the both-endian block
    reader.ReadInt32();

    //skip the escape sequences block
    reader.ReadBytes(32);

    int volumeSetSize = reader.ReadInt16();
    //skip
    reader.ReadInt16();

    int volumeSequenceNo = reader.ReadInt16();
    //skip
    reader.ReadInt16();

    int logicalBlockSize = reader.ReadInt16();
    //skip
    reader.ReadInt16();

    int pathTableSize = reader.ReadInt16();
    //skip
    reader.ReadInt16();

    //skip the type L and M path tables for now
    reader.ReadBytes(16);

    //skip more bytes??
    reader.ReadBytes(4);

    int dirLength = reader.ReadByte() - 1;
    //Console.WriteLine($"Parsing {dirLength} bytes into root record");
    var rootRecord = DataRecord.FromSector(new BinaryReader(new MemoryStream(reader.ReadBytes(dirLength))), owner);

    return new(version, systemID, volumeID, logicalBlocks, logicalBlockSize, volumeSetSize, volumeSequenceNo, pathTableSize, rootRecord, owner);
  }
}
