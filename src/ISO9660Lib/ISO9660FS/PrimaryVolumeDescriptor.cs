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
    DataRecord root,
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
    RootRecord = root;
    Owner = owner;
  }
}
