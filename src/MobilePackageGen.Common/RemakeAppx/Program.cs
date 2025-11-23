using System.IO.Abstractions;
using System.Reactive.Linq;
using CSharpFunctionalExtensions;
using DotnetPackaging.Msix;
using Zafiro.DivineBytes;
using Serilog;
using RemakeAppx.Helpers;
using File = System.IO.File;

namespace RemakeAppx
{
    public class Program
    {
        public static async Task MakeAppx(string inputFolder, string outputFile, bool bundleMode, bool unsignedMode)
        {
            /*Console.WriteLine($"Making {outputFile} out of {inputFolder}");
            Console.WriteLine($"Bundle Mode: {bundleMode}");
            Console.WriteLine($"Unsigned Mode: {unsignedMode}");*/

            FileSystem fs = new();
            IDirectoryInfo directoryInfo = fs.DirectoryInfo.New(inputFolder);
            IODir ioDir = new(directoryInfo);

            await Msix.FromDirectory(ioDir, Maybe<ILogger>.None, bundleMode, unsignedMode, inputFolder)
                .Map(async source =>
                {
                    await using var fileStream = File.Open(outputFile, FileMode.Create);
                    return await source.DumpTo(fileStream);
                });
        }
    }
}
