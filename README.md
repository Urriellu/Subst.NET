# Subst.NET
.NET Library that maps a drive letter to a LOCAL directory on the same machine. Windows-only.

## Usage

```C#
using System;
using SubstNET;

class Program
{
    static void Main(string[] args)
    {
        Subst.MapDrive('X', @"C:\path\to\dir"); // the contents of this directory will appear as a drive on this Windows computer

        foreach ((char DriveLetter, string PathDirectoryMapped) in Subst.GetAvailableDrives())
        {
            Console.WriteLine(@$"{DriveLetter}:\ is mapped to {PathDirectoryMapped}");
        }

        Subst.UnmapDrive('X');
    }
}
```

## Features

- Extremely easy to use.
- Windows-only. It makes no sense to map/create drives on other Operating Systems. You may use hard links on Linux for a similar purpose.
- Support for mapping a directory as a drive.
- Support for retrieving the list of virtual drives mapped to directories.
- Support for unmapping virtual drives.
