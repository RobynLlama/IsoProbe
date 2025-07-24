namespace ISO9660Lib.ISO9660FS;

/// <summary>
/// The type of volume descriptor
/// </summary>
public enum VolumeDescriptorType
{
  /// <summary>
  /// A boot record
  /// </summary>
  BootRecord = 0,

  /// <summary>
  /// The primary descriptor
  /// </summary>
  PrimaryVolumeDescriptor,

  /// <summary>
  /// A supplemental descriptor for extensions
  /// </summary>
  SupplementalVolumeDescriptor,

  /// <summary>
  /// I don't know, rarely used
  /// </summary>
  VolumePartitionDescriptor,

  /// <summary>
  /// The descriptor set terminator
  /// </summary>
  VolumeDescriptorTerminator = 255,
}
