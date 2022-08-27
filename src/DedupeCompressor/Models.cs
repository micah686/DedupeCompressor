using System;
using System.Collections.Generic;
using System.Text;

namespace DedupeCompressor
{
    public class FileInfo
    {
        public FileInfo() { }
        public FileInfo(string filePath, ulong hash, bool useGZip)
        {
            FilePath = filePath;
            Hash = hash;
            UseGzip = useGZip;
        }
        public int Id { get; set; }
        public string FilePath { get; set; }
        public ulong Hash { get; set; }
        public readonly bool UseGzip = false;

    }

    public class ExtractInfo
    {
        public FileInfo FileInfo { get; set; }
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
        public string Message { get; set; }
    }
    
    
}
