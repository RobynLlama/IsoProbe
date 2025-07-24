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

  /// <summary>
  /// Reads the contents of the logical sector and attempts to
  /// parse it as a series of DataRecords
  /// </summary>
  /// <returns></returns>
  public List<DataRecord> GetDirectoryContents()
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

  /// <summary>
  /// Reads the contents from this record's extent.
  /// This is generally the file data, but for directories
  /// it will be the raw listing of all items contained within
  /// </summary>
  /// <returns></returns>
  /// <exception cref="InvalidDataException"></exception>
  public byte[] GetFileContents()
  {
    if (_fileContentsFetched)
      return _fileContents;

    var blockSize = Owner.PVD.LogicalBlockSize;
    var dataToRead = Parent?.DataLength ?? PhysicalSectorsOccupied * blockSize;
    _fileContents = new byte[dataToRead];
    using MemoryStream cms = new(_fileContents);
    var items = GetOccupiedSectors();

    while (items.Count > 0)
    {
      using var next = items.Dequeue();
      var nextRead = dataToRead;
      if (nextRead > blockSize)
        nextRead = blockSize;

      next.Reader.ReadBytes(ECMAFS.HEADER_SIZE);
      //This should ideally never be so big that it causes an issue
      //ISO9660 demands sector sizes be 2048 or smaller anyway
      cms.Write(next.Reader.ReadBytes((int)nextRead));
      dataToRead -= nextRead;
    }

    _fileContentsFetched = true;

    return _fileContents;
  }

  internal Queue<PhysicalSector> GetOccupiedSectors()
  {
    Queue<PhysicalSector> items = [];

    for (uint i = SectorIndex; i < SectorIndex + PhysicalSectorsOccupied; i++)
    {
      if (Owner.TryGetSectorRaw(i, out var raw))
        items.Enqueue(raw);
      else
        throw new InvalidOperationException($"Attempted to read an invalid sector #{i}");
    }

    return items;
  }
}
