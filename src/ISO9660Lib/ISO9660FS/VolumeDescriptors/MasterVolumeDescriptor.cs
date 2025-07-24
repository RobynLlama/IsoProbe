using System.IO;
using System.Text;

namespace ISO9660Lib.ISO9660FS;

/// <summary>
/// A special record that is always present on sector 16
/// that provides volume information for parsing the ECMAFS
/// </summary>
public class MasterVolumeDescriptor : VolumeDescriptor
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
  public readonly uint LogicalBlockCount;

  /// <summary>
  /// The size of the user data for each sector. See <seealso cref="ECMAFS.SECTOR_SIZE"/>
  /// for the constant physical size of each sector. All space on a sector not
  /// claimed by the header or user data is assumed to be error correcting codes
  /// </summary>
  public readonly uint LogicalBlockSize;

  /// <summary>
  /// The number of total volumes in this ECMAFS volume set. Nearly always 1
  /// </summary>
  public readonly uint VolumeSetSize;

  /// <summary>
  /// Which volume in the set this specific volume is. Nearly always 1
  /// </summary>
  public readonly uint VolumeSetNumber;

  /// <summary>
  /// The data record of the Root record. The Root contains a listing of
  /// every directory and file on the root of the filesystem
  /// </summary>
  public readonly DirectoryRecord RootRecord;

  /// <summary>
  /// The data record of the path table, a special area that lists
  /// directories for quick lookup. Good for hot path stuff but
  /// optional within the spec
  /// </summary>
  public readonly DirectoryRecord PathTable;

  /// <summary>
  /// A 128 character string describing the volume set for this disk
  /// </summary>
  public string VolumeSetID = string.Empty;

  /// <summary>
  /// A 128 character string describing the publisher for this media
  /// </summary>
  public string PublisherID = string.Empty;

  /// <summary>
  /// A 128 character string describing who prepared this media
  /// </summary>
  public string PreparerID = string.Empty;

  /// <summary>
  /// A 128 character string describing the application on this media
  /// </summary>
  public string ApplicationID = string.Empty;

  /// <summary>
  /// A 37 character string describing the copyright of this media
  /// </summary>
  public string CopyrightFile = string.Empty;

  /// <summary>
  /// A 37 character string. ???
  /// </summary>
  public string AbstractFile = string.Empty;

  /// <summary>
  /// A 37 character string. ???
  /// </summary>
  public string BiblioFile = string.Empty;

  /// <summary>
  /// A reference to the ECMAFS that owns this PVD
  /// </summary>
  public readonly ECMAFS Owner;

  internal MasterVolumeDescriptor(
    int volumeDescriptorVersion,
    string systemID,
    string volumeID,
    uint logicalBlockCount,
    uint logicalBlockSize,
    uint volumeSetSize,
    uint volumeSetNumber,
    DirectoryRecord root,
    DirectoryRecord path,
    VolumeDescriptorType type,
    ECMAFS owner
    ) : base(type)
  {
    VolumeDescriptorVersion = volumeDescriptorVersion;
    SystemID = systemID;
    VolumeID = volumeID;
    LogicalBlockCount = logicalBlockCount;
    LogicalBlockSize = logicalBlockSize;
    VolumeSetSize = volumeSetSize;
    VolumeSetNumber = volumeSetNumber;
    RootRecord = root;
    PathTable = path;
    Owner = owner;
  }
}
