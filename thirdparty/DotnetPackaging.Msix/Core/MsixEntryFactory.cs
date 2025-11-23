using DotnetPackaging.Msix.Core.Compression;

namespace DotnetPackaging.Msix.Core;

public static class MsixEntryFactory
{
    public static MsixEntry Compress(string entryName, IByteSource data)
    {
        if (MakeAppxDeflate.USE_EXTERNAL_DEFLATE_VIA_MAKEAPPX)
        {
            IByteSource compressedByteSource = ByteSource.FromByteObservable(MakeAppxDeflate.GetMakeAppxVersionOfDeflate(data));

            CompressionLevel compressionLevel = CompressionLevel.Optimal;

            MsixEntry msixEntry = new MsixEntry
            {
                Original = data,
                Compressed = compressedByteSource,
                FullPath = entryName,
                CompressionLevel = compressionLevel
            };

            return msixEntry;
        }
        else
        {
            IByteSource compressedByteSource = ByteSource.FromByteObservable(data.Bytes.Compressed());

            CompressionLevel compressionLevel = CompressionLevel.Optimal;

            MsixEntry msixEntry = new MsixEntry
            {
                Original = data,
                Compressed = compressedByteSource,
                FullPath = entryName,
                CompressionLevel = compressionLevel
            };

            return msixEntry;
        }
    }
}