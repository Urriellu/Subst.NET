using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace SubstNET.Test
{
    [TestClass]
    public class SubsTests
    {
        readonly Random rnd = new Random();

        const string lettersAndNumbers = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        string RandomString(int length, string chars = lettersAndNumbers) => new string(Enumerable.Repeat(chars, length).Select(s => s[rnd.Next(s.Length)]).ToArray());

        [TestMethod, Timeout(60 * 1000)]
        public void SubstTest()
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT) throw new Exception($"This library only works on Windows. Unix-like OSs do not have 'drives', therefore this library is unnecessary (you can easily use hard mounts instead).");

            // UNMAP ALL EXISTING MAPPINGS
            (char DriveLetter, string PathDirectoryMapped)[] mappedDrives = Subst.GetAvailableDrives().ToArray();
            foreach (var mappedDrive in mappedDrives) Subst.UnmapDrive(mappedDrive.DriveLetter);
            mappedDrives = Subst.GetAvailableDrives().ToArray();
            Assert.AreEqual(mappedDrives.Length, 0);

            string pathDirectoryToMap = Path.GetTempFileName();
            File.Delete(pathDirectoryToMap);

            // PREPARE TEMPORARY DIRECTORY TO BE MAPPED
            Directory.CreateDirectory(pathDirectoryToMap);
            int amountFiles = rnd.Next(5, 10);
            for (int i = 0; i < amountFiles; i++)
            {
                string pathfile = Path.Combine(pathDirectoryToMap, RandomString(rnd.Next(3, 20)) + ".txt");
                string contents = RandomString(rnd.Next(200, 20000), lettersAndNumbers + "\n");
                File.WriteAllText(pathfile, contents);
            }
            DirectoryInfo diOriginal = new DirectoryInfo(pathDirectoryToMap);
            FileInfo[] files = diOriginal.GetFiles();
            if (files.Length == 0) throw new Exception("No sample files were created.");
            if (files.Length != amountFiles) throw new Exception("An incorrect amount of sample files were created.");

            // MAP DIRECTORY
            DriveInfo[] currentDrives = DriveInfo.GetDrives();
            const string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            if (letters.All(letter => currentDrives.Any(d => d.Name[0] == letter))) throw new Exception($"All Drive letters are taken.");
            char? driveletter = null;
            while (!driveletter.HasValue || currentDrives.Any(d => d.Name[0] == driveletter.Value)) driveletter = letters[rnd.Next(letters.Length)];
            Subst.MapDrive(driveletter.Value, pathDirectoryToMap);

            // CHECK DIRECTORY MAPPED CORRECTLY
            DirectoryInfo diMappedDrive = new DirectoryInfo($"{driveletter}:\\");
            Assert.AreEqual(diMappedDrive.GetFiles().Length, amountFiles);
            foreach (var originalfile in diOriginal.GetFiles())
            {
                string pathMappedFile = Path.Combine($"{driveletter}:\\{originalfile.Name}");
                FileInfo fiMappedFile = new FileInfo(pathMappedFile);
                Assert.IsTrue(fiMappedFile.Exists);
                Assert.AreEqual(originalfile.Length, fiMappedFile.Length);
            }
            currentDrives = DriveInfo.GetDrives();
            Assert.IsTrue(currentDrives.Any(d => d.Name.StartsWith(driveletter.Value)));

            // CHECK LIBRARY INDICATES DIRECTORY IS MAPPED
            mappedDrives = Subst.GetAvailableDrives().ToArray();
            Assert.AreEqual(mappedDrives.Length, 1);
            Assert.AreEqual(mappedDrives[0].DriveLetter, driveletter.Value);
            Assert.AreEqual(mappedDrives[0].PathDirectoryMapped, pathDirectoryToMap);

            // UNMAP
            Subst.UnmapDrive(driveletter.Value);

            // CHECK DIRECTORY IS NOT MAPPED ANYMORE
            Assert.IsFalse(Directory.Exists($"{driveletter}:\\"));

            // CHECK LIBRARY INDICATES NOTHING IS MAPPED
            mappedDrives = Subst.GetAvailableDrives().ToArray();
            Assert.AreEqual(mappedDrives.Length, 0);

            // DELETE TEMP DIR
            Directory.Delete(pathDirectoryToMap, recursive: true);
        }
    }
}
