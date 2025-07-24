namespace ISO9660Lib.ISO9660FS;

/// <summary>
/// This descriptor indicates the set of descriptors has terminated
/// </summary>
public class VolumeDescriptorSetTerminator : VolumeDescriptor
{
  internal VolumeDescriptorSetTerminator() : base(VolumeDescriptorType.VolumeDescriptorTerminator)
  {
  }
}
