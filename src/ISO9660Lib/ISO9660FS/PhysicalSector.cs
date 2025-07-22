using System;
using System.IO;

namespace ISO9660Lib.ISO9660FS;

/// <summary>
/// A raw section of memory read from a given physical
/// sector of an ECMAFS
/// </summary>
public class PhysicalSector : IDisposable
{
  /// <summary>
  /// The ECMAFS that owns this sector
  /// </summary>
  public readonly ECMAFS Owner;

  /// <summary>
  /// Which sector this RawSector was read from
  /// </summary>
  public readonly int SectorIndex;

  /// <summary>
  /// A BinaryReader to read from the raw sector
  /// </summary>
  public BinaryReader Reader;

  internal PhysicalSector(ECMAFS owner, int sectorIndex, BinaryReader reader)
  {
    Owner = owner;
    SectorIndex = sectorIndex;
    Reader = reader;
  }

  /// <summary>
  /// Disposes of the raw memory
  /// </summary>
  public void Dispose()
  {
    Reader.Dispose();
    GC.SuppressFinalize(this);
  }
}
