namespace ISO9660Lib.ISO9660FS;

/// <summary>
/// The standard volume descriptor, always first and always
/// has type 0x01.
/// </summary>
public class PrimaryVolumeDescriptor : MasterVolumeDescriptor
{
  internal PrimaryVolumeDescriptor(int volumeDescriptorVersion, string systemID, string volumeID, uint logicalBlockCount, uint logicalBlockSize, uint volumeSetSize, uint volumeSetNumber, DirectoryRecord root, DirectoryRecord path, ECMAFS owner) : base(volumeDescriptorVersion, systemID, volumeID, logicalBlockCount, logicalBlockSize, volumeSetSize, volumeSetNumber, root, path, VolumeDescriptorType.PrimaryVolumeDescriptor, owner)
  {
  }
}
