﻿using System.Runtime.InteropServices;

// https://raw.githubusercontent.com/arul-gupta/CatalogAPI/master/CatalogAPI/CatalogAPI/CatalogFunctions.cs

namespace Playground
{
    [StructLayout(LayoutKind.Sequential)]
    public struct CRYPTOAPI_BLOB
    {
        public uint cbData;
        public IntPtr pbData;
    } // CRYPT_INTEGER_BLOB, CRYPT_ATTR_BLOB, CRYPT_OBJID_BLOB, CRYPT_HASH_BLOB

    [StructLayout(LayoutKind.Sequential)]
    public struct GUID
    {
        int a;
        short b;
        short c;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        byte[] d;
    }

    [StructLayout(LayoutKind.Sequential)]
    public class CRYPTCATMEMBER
    {
        public uint cbStruct;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string pwszReferenceTag;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string pwszFileName;
        public GUID gSubjectType;
        public uint fdwMemberFlags;
        public IntPtr pIndirectData;
        public uint dwCertVersion;
        public uint dwReserved;
        public IntPtr hReserved;
        public CRYPTOAPI_BLOB sEncodedIndirectData;
        public CRYPTOAPI_BLOB sEncodedMemberInfo;
    }

    // https:// msdn.microsoft.com/en-us/library/windows/desktop/bb427419%28v=vs.85%29.aspx
    [StructLayout(LayoutKind.Sequential)]
    public struct CRYPTCATCDF
    {
        uint cbStruct;
        IntPtr hFile;
        uint dwCurFilePos;
        uint dwLastMemberOffset;
        bool fEOF;
        [MarshalAs(UnmanagedType.LPWStr)]
        string pwszResultDir;
        IntPtr hCATStore;
    }

    // https:// msdn.microsoft.com/en-us/library/windows/desktop/bb736433%28v=vs.85%29.aspx
    [StructLayout(LayoutKind.Sequential)]
    public struct SIP_INDIRECT_DATA
    {
        public CRYPT_ATTRIBUTE_TYPE_VALUE Data;
        public CRYPT_ALGORITHM_IDENTIFIER DigestAlgorithm;
        public CRYPTOAPI_BLOB Digest;
    }

    // https:// msdn.microsoft.com/en-us/library/windows/desktop/aa381151%28v=vs.85%29.aspx
    [StructLayout(LayoutKind.Sequential)]
    public struct CRYPT_ATTRIBUTE_TYPE_VALUE
    {
        [MarshalAs(UnmanagedType.LPStr)]
        public string pszObjId;
        public CRYPTOAPI_BLOB Value;
    }

    // https:// msdn.microsoft.com/en-us/library/windows/desktop/aa381133%28v=vs.85%29.aspx
    [StructLayout(LayoutKind.Sequential)]
    public struct CRYPT_ALGORITHM_IDENTIFIER
    {
        [MarshalAs(UnmanagedType.LPStr)]
        public string pszObjId;
        public CRYPTOAPI_BLOB Parameters;
    }

    class CatalogFunctions
    {
        public static IntPtr INVALID_HANDLE_VALUE = IntPtr.Zero;

        public static uint PROV_RSA_FULL = 1;
        public static uint CRYPTCAT_VERSION_1 = 0x100;
        public static long NTE_BAD_KEYSET = 0x80090016L;
        public static uint CRYPT_NEWKEYSET = 0x00000008;

        #region Catalog Create Functions
        // https:// msdn.microsoft.com/en-us/library/windows/desktop/bb410248%28v=vs.85%29.aspx
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void PFN_CDF_PARSE_ERROR_CALLBACK(
            [In] uint dwErrorArea,
            [In] uint dwLocalError,
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwszLine
        );

        // https:// msdn.microsoft.com/en-us/library/windows/desktop/bb427424%28v=vs.85%29.aspx
        [DllImport("wintrust.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool CryptCATCDFClose(
            [In] IntPtr pCDF
        );

        [DllImport("wintrust.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr CryptCATCDFOpen(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwszFilePath,
            [In, Optional] IntPtr pfnParseError
        );

        // https:// msdn.microsoft.com/en-us/library/windows/desktop/bb427423%28v=vs.85%29.aspx
        [DllImport("wintrust.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.LPWStr)]
        public static extern unsafe string CryptCATCDFEnumMembersByCDFTagEx(
            [In] IntPtr pCDF,
            [In, Out] void* pwszPrevCDFTag,
            [In] IntPtr pfnParseError,
            [In] CRYPTCATMEMBER ppMember,
            [In] bool fContinueOnError,
            [In] IntPtr pvReserved
        );
        #endregion

        [DllImport("kernel32.dll")]
        public static extern uint GetLastError();

        #region Read Catalog Functions
        [DllImport("Advapi32.dll", SetLastError = true)]
        public static extern bool CryptAcquireContext(
            out IntPtr hCryptProv,
            IntPtr container,
            IntPtr provider,
            uint providerType,
            uint flags);

        [DllImport("Advapi32.dll", SetLastError = true)]
        public static extern bool CryptReleaseContext(
            IntPtr hCryptProv,
            uint flags);

        [DllImport("Wintrust.dll", SetLastError = true)]
        public static extern bool CryptCATClose(IntPtr hProv);


        [DllImport("Wintrust.dll", SetLastError = true)]
        public static extern IntPtr CryptCATOpen(
            [param: MarshalAs(UnmanagedType.LPTStr)] string pwszFileName,
            uint fdwOpenFlags,
            IntPtr hProv,
            uint dwPublicVersion,
            uint dwEncodingType);

        [DllImport("Wintrust.dll")]
        public static extern unsafe IntPtr CryptCATEnumerateMember(
            IntPtr hCatalog,
            IntPtr pPrevMember);
        #endregion
    }
}