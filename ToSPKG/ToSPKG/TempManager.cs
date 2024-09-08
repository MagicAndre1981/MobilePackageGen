﻿using System.Collections.Generic;
using System.IO;

namespace ToSPKG
{
    internal class TempManager
    {
        private static readonly List<string> tempFiles = new();

        internal static string GetTempFile()
        {
            string file = Path.GetTempFileName();
            File.Delete(file);
            tempFiles.Add(file);
            return file;
        }

        internal static void CleanupTempFiles()
        {
            foreach (string file in tempFiles)
            {
                File.Delete(file);
            }
            tempFiles.Clear();
        }
    }
}
