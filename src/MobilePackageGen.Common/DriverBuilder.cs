using DiscUtils;
using Microsoft.Deployment.Compression.Cab;
using Microsoft.Deployment.Compression;
using System.Runtime.InteropServices;

namespace MobilePackageGen
{
    public class DriverBuilder
    {
        public static void BuildDrivers(IEnumerable<IDisk> disks, string destination_path, UpdateHistory.UpdateHistory? updateHistory)
        {
            Logging.Log();
            Logging.Log("Building Driver Files...");
            Logging.Log();

            BuildCabinets(disks, destination_path, updateHistory);

            Logging.Log();
            Logging.Log("Cleaning up...");
            Logging.Log();

            TempManager.CleanupTempFiles();
        }

        private static IEnumerable<IPartition> GetPartitionsWithServicing(IEnumerable<IDisk> disks)
        {
            List<IPartition> fileSystemsWithServicing = [];

            foreach (IDisk disk in disks)
            {
                foreach (IPartition partition in disk.Partitions)
                {
                    if (partition.Name != "BSP")
                    {
                        continue;
                    }

                    IFileSystem? fileSystem = partition.FileSystem;

                    if (fileSystem != null)
                    {
                        try
                        {
                            if (fileSystem.DirectoryExists(@"Windows\System32\DriverStore\FileRepository"))
                            {
                                fileSystemsWithServicing.Add(partition);
                            }
                        }
                        catch (Exception ex)
                        {
                            Logging.Log($"Error: Looking up file system servicing failed! {ex.Message}", LoggingLevel.Error);
                        }
                    }
                }
            }

            return fileSystemsWithServicing;
        }

        private static int GetPackageCount(IEnumerable<IDisk> disks)
        {
            int count = 0;

            IEnumerable<IPartition> partitionsWithCbsServicing = GetPartitionsWithServicing(disks);

            foreach (IPartition partition in partitionsWithCbsServicing)
            {
                IFileSystem fileSystem = partition.FileSystem!;

                IEnumerable<string> manifestFiles = fileSystem.GetFilesWithNtfsIssueWorkaround(@"Windows\System32\DriverStore\FileRepository", "*.inf", SearchOption.AllDirectories);

                count += manifestFiles.Count();
            }

            return count;
        }

        private static IEnumerable<CabinetFileInfo> GetCabinetFileInfoForCbsPackage(string inf, IPartition partition)
        {
            List<CabinetFileInfo> fileMappings = [];

            IFileSystem fileSystem = partition.FileSystem!;

            foreach (string entry in fileSystem.GetFiles(inf, "*", SearchOption.AllDirectories))
            {
                if (fileSystem.DirectoryExists(entry))
                {
                    continue;
                }

                fileMappings.Add(new CabinetFileInfo()
                {
                    FileName = string.Join("\\", entry.Split("\\")[5..]),
                    FileStream = fileSystem.OpenFile(entry, FileMode.Open, FileAccess.Read),
                    Attributes = fileSystem.GetAttributes(entry) & ~FileAttributes.ReparsePoint,
                    DateTime = fileSystem.GetLastWriteTime(entry)
                });
            }

            return fileMappings;
        }

        private static void BuildCabinets(IEnumerable<IDisk> disks, string outputPath, UpdateHistory.UpdateHistory? updateHistory)
        {
            int packagesCount = GetPackageCount(disks);

            IEnumerable<IPartition> partitionsWithCbsServicing = GetPartitionsWithServicing(disks);

            int i = 0;

            foreach (IPartition partition in partitionsWithCbsServicing)
            {
                IFileSystem fileSystem = partition.FileSystem!;

                IEnumerable<string> manifestFiles = fileSystem.GetFilesWithNtfsIssueWorkaround(@"Windows\System32\DriverStore\FileRepository", "*.inf", SearchOption.AllDirectories);

                foreach (string manifestFile in manifestFiles)
                {
                    try
                    {
                        string folder = string.Join("\\", manifestFile.Split("\\")[..^1]);

                        (string cabFileName, string cabFile) = BuildMetadataHandler.GetPackageNamingForINF(manifestFile, updateHistory);

                        if (string.IsNullOrEmpty(cabFileName) && string.IsNullOrEmpty(cabFile))
                        {
                            string packageName = Path.GetFileNameWithoutExtension(manifestFile);

                            string partitionName = partition.Name.Replace("\0", "-");

                            cabFileName = Path.Combine(partitionName, packageName);

                            cabFile = Path.Combine(outputPath, $"{cabFileName}.cab");
                        }
                        else
                        {
                            cabFile = Path.Combine(outputPath, cabFile);
                        }

                        string componentStatus = $"Creating package {i + 1} of {packagesCount} - {Path.GetFileName(cabFileName)}";
                        if (componentStatus.Length > Console.BufferWidth - 24 - 1)
                        {
                            componentStatus = $"{componentStatus[..(Console.BufferWidth - 24 - 4)]}...";
                        }

                        Logging.Log(componentStatus);
                        string progressBarString = Logging.GetDISMLikeProgressBar(0);
                        Logging.Log(progressBarString, returnLine: false);

                        string fileStatus = "";

                        if (!File.Exists(cabFile))
                        {
                            IEnumerable<CabinetFileInfo> fileMappings = GetCabinetFileInfoForCbsPackage(folder, partition);

                            uint oldPercentage = uint.MaxValue;
                            uint oldFilePercentage = uint.MaxValue;
                            string oldFileName = "";

                            // Cab Creation is only supported on Windows
                            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                            {
                                if (fileMappings.Count() > 0)
                                {
                                    if (Path.GetDirectoryName(cabFile) is string directory && !Directory.Exists(directory))
                                    {
                                        Directory.CreateDirectory(directory);
                                    }

                                    CabInfo cab = new(cabFile);
                                    cab.PackFiles(null, fileMappings.Select(x => x.GetFileTuple()).ToArray(), fileMappings.Select(x => x.FileName).ToArray(), CompressionLevel.Min, (object? _, ArchiveProgressEventArgs archiveProgressEventArgs) =>
                                    {
                                        string fileNameParsed;
                                        if (string.IsNullOrEmpty(archiveProgressEventArgs.CurrentFileName))
                                        {
                                            fileNameParsed = $"Unknown ({archiveProgressEventArgs.CurrentFileNumber})";
                                        }
                                        else
                                        {
                                            fileNameParsed = archiveProgressEventArgs.CurrentFileName;
                                        }

                                        uint percentage = (uint)Math.Floor((double)archiveProgressEventArgs.CurrentFileNumber * 50 / archiveProgressEventArgs.TotalFiles) + 50;

                                        if (percentage != oldPercentage)
                                        {
                                            oldPercentage = percentage;
                                            string progressBarString = Logging.GetDISMLikeProgressBar(percentage);

                                            Logging.Log(progressBarString, returnLine: false);
                                        }

                                        if (fileNameParsed != oldFileName)
                                        {
                                            Logging.Log();
                                            Logging.Log(new string(' ', fileStatus.Length));
                                            Logging.Log(Logging.GetDISMLikeProgressBar(0), returnLine: false);

                                            Console.SetCursorPosition(0, Console.CursorTop - 2);

                                            oldFileName = fileNameParsed;

                                            oldFilePercentage = uint.MaxValue;

                                            fileStatus = $"Adding file {archiveProgressEventArgs.CurrentFileNumber + 1} of {archiveProgressEventArgs.TotalFiles} - {fileNameParsed}";
                                            if (fileStatus.Length > Console.BufferWidth - 24 - 1)
                                            {
                                                fileStatus = $"{fileStatus[..(Console.BufferWidth - 24 - 4)]}...";
                                            }

                                            Logging.Log();
                                            Logging.Log(fileStatus);
                                            Logging.Log(Logging.GetDISMLikeProgressBar(0), returnLine: false);

                                            Console.SetCursorPosition(0, Console.CursorTop - 2);
                                        }

                                        uint filePercentage = (uint)Math.Floor((double)archiveProgressEventArgs.CurrentFileBytesProcessed * 100 / archiveProgressEventArgs.CurrentFileTotalBytes);

                                        if (filePercentage != oldFilePercentage)
                                        {
                                            oldFilePercentage = filePercentage;
                                            string progressBarString = Logging.GetDISMLikeProgressBar(filePercentage);

                                            Logging.Log();
                                            Logging.Log();
                                            Logging.Log(progressBarString, returnLine: false);

                                            Console.SetCursorPosition(0, Console.CursorTop - 2);
                                        }
                                    });
                                }
                            }

                            foreach (CabinetFileInfo fileMapping in fileMappings)
                            {
                                fileMapping.FileStream.Close();
                            }
                        }
                        else
                        {
                            Logging.Log($"CAB already exists! Skipping. {cabFile}", LoggingLevel.Warning);
                        }

                        if (i != packagesCount - 1)
                        {
                            Console.SetCursorPosition(0, Console.CursorTop - 1);

                            Logging.Log(new string(' ', componentStatus.Length));
                            Logging.Log(Logging.GetDISMLikeProgressBar(100));

                            if (string.IsNullOrEmpty(fileStatus))
                            {
                                Logging.Log(new string(' ', fileStatus.Length));
                                Logging.Log(new string(' ', 60));
                            }
                            else
                            {
                                Logging.Log(new string(' ', fileStatus.Length));
                                Logging.Log(Logging.GetDISMLikeProgressBar(100));
                            }

                            Console.SetCursorPosition(0, Console.CursorTop - 4);
                        }
                        else
                        {
                            Logging.Log($"\r{Logging.GetDISMLikeProgressBar(100)}");

                            if (string.IsNullOrEmpty(fileStatus))
                            {
                                Logging.Log();
                                Logging.Log(new string(' ', 60));
                            }
                            else
                            {
                                Logging.Log();
                                Logging.Log(Logging.GetDISMLikeProgressBar(100));
                            }
                        }

                        i++;
                    }
                    catch (Exception ex)
                    {
                        Logging.Log($"Error: CAB creation failed! {ex.Message}", LoggingLevel.Error);
                        //throw;
                    }
                }
            }
        }
    }
}
