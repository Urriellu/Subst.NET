using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace SubstNET
{
    public class Subst
    {
        /// <summary>Defines, redefines, or deletes MS-DOS device names.</summary>
        /// <remarks>
        /// <seealso cref="https://docs.microsoft.com/en-us/windows/desktop/api/fileapi/nf-fileapi-definedosdevicew"/>
        /// <seealso cref="https://www.pinvoke.net/default.aspx/kernel32.definedosdevice"/>
        /// </remarks>
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool DefineDosDevice(int dwFlags, string lpDeviceName, string? lpTargetPath);

        /// <summary>
        /// Retrieves information about MS-DOS device names. The function can obtain the current mapping for a particular MS-DOS device name. The function can also obtain a list of all existing MS-DOS device names.
        /// </summary>
        /// <remarks>
        /// MS-DOS device names are stored as junctions in the object namespace. The code that converts an MS-DOS path into a corresponding path uses these junctions to map MS-DOS devices and drive letters. The QueryDosDevice function enables an application to query the names of the junctions used to implement the MS-DOS device namespace as well as the value of each specific junction.
        /// <seealso cref="https://docs.microsoft.com/en-us/windows/desktop/api/fileapi/nf-fileapi-querydosdevicew"/>
        /// <seealso cref="https://www.pinvoke.net/default.aspx/kernel32.QueryDosDevice"/>
        /// </remarks>
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int QueryDosDevice(string lpDeviceName, StringBuilder lpTargetPath, int ucchMax);

        [Flags]
        enum DefineDosDeviceFlags
        {
            None = 0,

            /// <summary>
            /// Uses the lpTargetPath string as is. Otherwise, it is converted from an MS-DOS path to a path.
            /// </summary>
            DDD_RAW_TARGET_PATH = 1 << 0,

            /// <summary>
            /// Removes the specified definition for the specified device. 
            /// </summary>
            DDD_REMOVE_DEFINITION = 1 << 1,

            /// <summary>
            /// If this value is specified along with <see cref="DDD_REMOVE_DEFINITION"/>, the function will use an exact match to determine which mapping to remove.
            /// </summary>
            DDD_EXACT_MATCH_ON_REMOVE = 1 << 2,

            /// <summary>
            /// Do not broadcast the <see href="https://docs.microsoft.com/de-de/windows/desktop/winmsg/wm-settingchange">WM_SETTINGCHANGE</see> message. By default, this message is broadcast to notify the shell and applications of the change.
            /// </summary>
            DDD_NO_BROADCAST_SYSTEM = 1 << 3
        }

        /// <summary>Maps the given directory to a virtual drive.</summary>
        /// <param name="driveLetter">A drive letter (from A to Z) to be mapped to the given folder.</param>
        /// <param name="path">The directory to be mapped as a virtual drive.</param>
        public static void MapDrive(char driveLetter, string path)
        {
            driveLetter = char.ToUpperInvariant(driveLetter);
            if (driveLetter < 'A' || driveLetter > 'Z') throw new ArgumentOutOfRangeException(nameof(driveLetter), $"Drive '{driveLetter}' is invalid. It must be a letter from A to Z.");
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (!Directory.Exists(path)) throw new DirectoryNotFoundException();
            bool pinvokeResult = DefineDosDevice((int)DefineDosDeviceFlags.DDD_RAW_TARGET_PATH, $"{driveLetter}:", "\\??\\" + new DirectoryInfo(path).FullName);
            if (!pinvokeResult) throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        /// <summary>Unmaps a given virtual drive.</summary>
        /// <param name="driveLetter">A drive letter (from A to Z) to be unmapped.</param>
        public static void UnmapDrive(char driveLetter)
        {
            driveLetter = char.ToUpperInvariant(driveLetter);
            if (driveLetter < 'A' || driveLetter > 'Z') throw new ArgumentOutOfRangeException(nameof(driveLetter), $"Drive '{driveLetter}' is invalid. It must be a letter from A to Z.");
            bool pinvokeResult = DefineDosDevice((int)DefineDosDeviceFlags.DDD_REMOVE_DEFINITION, $"{driveLetter}:", null);
            if (!pinvokeResult) throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        private const int MAX_PATH = 248;

        private static readonly int DRIVE_UNC_LENGTH = "\\??\\".Length;

        /// <summary>Gets the path to the directory mapped to a given drive.</summary>
        /// <param name="driveLetter">A drive letter (from A to Z).</param>
        /// <returns>Returns the mapped directory path. Returns <c>null</c> if the drive letter was not mapped.</returns>
        public static string GetDriveMapping(char driveLetter)
        {
            driveLetter = char.ToUpperInvariant(driveLetter);

            if (driveLetter < 'A' || driveLetter > 'Z') throw new ArgumentOutOfRangeException(nameof(driveLetter), $"Drive '{driveLetter}' is invalid. It must be a letter from A to Z.");

            var buffer = new StringBuilder(MAX_PATH + DRIVE_UNC_LENGTH);

            if (QueryDosDevice($"{driveLetter}:", buffer, buffer.Capacity) == 0)
            {
                int error = Marshal.GetLastWin32Error();

                if (error == 0 || error == 2)
                {
                    // ERROR_FILE_NOT_FOUND: The system cannot find the file specified.
                    return null;
                }

                throw new Win32Exception(error);
            }

            // there is a mapping but it is not a SUBST directory
            if (!buffer.ToString().StartsWith("\\??\\")) return null;

            return buffer.ToString().Substring(DRIVE_UNC_LENGTH);
        }

        public static IEnumerable<(char DriveLetter, string PathDirectoryMapped)> GetAvailableDrives()
        {
            for (char drive = 'A'; drive <= 'Z'; drive++)
            {
                var buffer = new StringBuilder(MAX_PATH + DRIVE_UNC_LENGTH);

                if (QueryDosDevice($"{drive}:", buffer, buffer.Capacity) == 0)
                {
                    int error = Marshal.GetLastWin32Error();

                    // ERROR_FILE_NOT_FOUND: The system cannot find the file specified.
                    if (error == 2)
                    {
                        // drive exists but is not a mapping
                    }
                }

                if (buffer.ToString().StartsWith("\\??\\"))
                {
                    string pathDirectoryMapped = buffer.ToString().Substring(DRIVE_UNC_LENGTH);
                    yield return (drive, pathDirectoryMapped);
                }
            }
        }
    }
}
