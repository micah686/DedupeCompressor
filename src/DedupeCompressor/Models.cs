using System;
using System.Collections.Generic;
using System.Text;

namespace DedupeCompressor
{
    public class CompactFileInfo
    {
        public CompactFileInfo() { }
        public CompactFileInfo(string filePath, ulong hash)
        {
            FilePath = filePath;
            Hash = hash;
        }
        public int Id { get; set; }
        public string FilePath { get; set; }
        public ulong Hash { get; set; }

    }

    public class ExtractInfo
    {
        public CompactFileInfo FileInfo { get; set; }
        public string ExtractPath { get; set; }
    }
    

    public class ProgressCompact
    {
        public string FilePackPath { get; set; }
        public int TotalFiles { get; set; }
        public int CurrentFile { get; set; }
    }
    public class ProgressUnpack
    {
        public string FileExtractPath { get; set; }
        public int TotalFiles { get; set; }
        public int CurrentFile { get; set; }
        public bool Success { get; set; }
    }
    
    
}
