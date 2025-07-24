namespace ISO9660Lib;

/// <summary>
/// The filesystem format of a disk
/// </summary>
public enum DiskFormat
{
  /// <summary>
  /// The FS has not been determined yet or is unable to be determined
  /// </summary>
  Unknown,
  /// <summary>
  /// A Standard CD-ROM
  /// </summary>
  ISO9660,
  /// <summary>
  /// Universal Disk Format
  /// </summary>
  UDF,
}
