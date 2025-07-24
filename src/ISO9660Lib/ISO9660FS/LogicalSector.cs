using System;
using System.Collections.Generic;
using System.IO;

namespace ISO9660Lib.ISO9660FS;

/// <summary>
/// A single record in the filesystem, can be a directory or a file
/// or part of a multi-part record
/// </summary>
public class LogicalSector
{
  /// <summary>
  /// The index of this sector within its owning ECMAFS
  /// </summary>
  public readonly uint SectorIndex;

  /// <summary>
  /// A list of the sectors this Logical sector uses. A file or
  /// directory listing larger than the ECMA UserData sector size
  /// will be spread out over more than one sector
  /// </summary>
  public readonly uint PhysicalSectorsOccupied;

  /// <summary>
  /// If this item is contained by a directory record this
  /// field will point to it for easy traversal.
  /// </summary>
  public DataRecord? Parent = null;

  /// <summary>
  /// A reference to the ECMAFS this record is contained within
  /// </summary>
  public ECMAFS Owner;

  private byte[] _fileContents = [];
  private bool _fileContentsFetched = false;
  private readonly List<DataRecord> _directoryContentsCache = [];
  private bool _directoryContentsFetched = false;

  internal LogicalSector(
    uint sectorIndex,
    uint sectorsOccupied,
    ECMAFS owner,
    DataRecord? parent = null
  )
  {
    SectorIndex = sectorIndex;
    PhysicalSectorsOccupied = sectorsOccupied;
    Owner = owner;
    Parent = parent;
  }

  internal List<DataRecord> GetDirectoryContents()
  {
    if (_directoryContentsFetched)
    {
      Owner._logger?.LogMessage($"Retrieving cached directory {Parent?.Identifier}");
      return _directoryContentsCache;
    }

    Owner._logger?.LogMessage($"Retrieving new directory contents");

    var data = GetFileContents();
    using MemoryStream ms = new(data);
    using BinaryReader reader = new(ms);

    while (ms.Position < ms.Length)
    {
      int size = reader.ReadByte() - 1;

      //Read every byte because some zeroes are just padding, actually
      if (size <= 0)
        continue;

      byte[] input = reader.ReadBytes(size);
      _directoryContentsCache.Add(DataRecord.FromBytes(input, this, Owner));
    }

    _directoryContentsFetched = true;
    return _directoryContentsCache;
  }

  internal byte[] GetFileContents(uint? requestedRead = null)
  {
    var blockSize = Owner.PVD.LogicalBlockSize;
    var totalSize = Parent?.DataLength ?? PhysicalSectorsOccupied * blockSize;
    var dataToRead = requestedRead ?? totalSize;

    if (dataToRead > totalSize)
      dataToRead = totalSize;

    if (_fileContentsFetched && requestedRead <= _fileContents.Length)
    {
      using MemoryStream c = new(_fileContents);
      using BinaryReader cache = new(c);
      Owner._logger?.LogMessage("Cache hit on contents");
      return cache.ReadBytes((int)requestedRead);
    }

    var contents = new byte[dataToRead];
    using MemoryStream cms = new(contents);

    uint sectorsOccupied = (dataToRead + Owner.PVD.LogicalBlockSize - 1) / Owner.PVD.LogicalBlockSize;
    var items = GetOccupiedSectors(sectorsOccupied);

    while (items.Count > 0)
    {
      using var next = items.Dequeue();
      var nextRead = dataToRead;
      if (nextRead > blockSize)
        nextRead = blockSize;

      next.Reader.ReadBytes(Owner.HEADER_SIZE);

      //This should ideally never be so big that it causes an issue
      //ISO9660 demands sector sizes be 2048 or smaller anyway
      cms.Write(next.Reader.ReadBytes((int)nextRead));
      dataToRead -= nextRead;
      next.Dispose();

      if (dataToRead == 0)
        break;
    }

    //only cache reads less than 4kb
    if (contents.Length < 4096)
    {
      _fileContentsFetched = true;
      _fileContents = contents;
    }

    return contents;
  }

  internal Queue<PhysicalSector> GetOccupiedSectors(uint fetch = 0)
  {
    Queue<PhysicalSector> items = [];
    if (fetch == 0)
      fetch = PhysicalSectorsOccupied;

    for (uint i = SectorIndex; i < SectorIndex + fetch; i++)
    {
      if (Owner.TryGetSectorRaw(i, out var raw))
        items.Enqueue(raw);
      else
        throw new InvalidOperationException($"Attempted to read an invalid sector #{i}");
    }

    return items;
  }
}
