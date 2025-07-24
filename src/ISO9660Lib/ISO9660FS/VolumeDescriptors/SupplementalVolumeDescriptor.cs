namespace ISO9660Lib.ISO9660FS;

/// <summary>
/// A secondary volume descriptor that supersedes the PVD
/// when read. Used for extended functionality
/// </summary>
public class SupplementalVolumeDescriptor : MasterVolumeDescriptor
{
  internal SupplementalVolumeDescriptor(int volumeDescriptorVersion, string systemID, string volumeID, uint logicalBlockCount, uint logicalBlockSize, uint volumeSetSize, uint volumeSetNumber, DirectoryRecord root, DirectoryRecord path, ECMAFS owner) : base(volumeDescriptorVersion, systemID, volumeID, logicalBlockCount, logicalBlockSize, volumeSetSize, volumeSetNumber, root, path, VolumeDescriptorType.SupplementalVolumeDescriptor, owner)
  {
  }
}
