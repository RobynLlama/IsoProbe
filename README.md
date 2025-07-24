# IsoProbe

![Language: C#](https://img.shields.io/badge/Language-C%23-blue?style=flat-square&logo=sharp)
![License: LGPLv3](https://img.shields.io/badge/License-LGPLv3-orange?style=flat-square&logo=gnuemacs)

![IsoProbe Logo](banner.png)

IsoProbe is a work-in-progress ISO-9660 filesystem library and frontend application for browsing and extracting files from ISO images.

## Features

- Basic terminal UI for browsing ISO-9660 filesystems

### Commands

- **HELP**: Displays detailed usage information for all commands. More comprehensive than this document, so use it if you get stuck.
- **LOAD**: Loads an ISO disk image into memory.
- **CLOSE**: Closes the currently open disk, allowing you to open another. Note: `LOAD` automatically closes any open disk before loading a new one, but you can use `CLOSE` to manage this manually.
- **LS**: Lists the contents of the current directory.
- **CD**: Changes the current directory.
- **PEEK**: Displays a hexdump of the first 128 bytes of a specified file on the open disk.
- **FILEDUMP**: Extracts a specified file from the disk and saves it to a file on the local drive. **Warning:** This will overwrite existing files without prompting.
- **EXIT**: Immediately terminates the application.

## Known Issues

- Joliet extensions are mostly supported.
  - Detection of SVD triggers Joliet support, which works for ~99% of cases but may cause issues with some non-standard disks.
- Rock Ridge extensions are not supported yet.
- Multi-extent files are not supported and will log a warning on attempted access. This will only effect files larger than 4gb
- Debug logging cannot be disabled.
