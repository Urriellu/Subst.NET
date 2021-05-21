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
