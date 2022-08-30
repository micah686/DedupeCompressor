using System;
using System.Collections.Generic;
using System.Text;
using LiteDB;
using System.IO;
using Extensions.Data;
using System.Linq;

namespace DedupeCompressor
{
    public class DataCompressor
    {
        #region Events and Handlers
        public event Action<ProgressUnpack> FileUnpackedEvent;
        public event Action<ProgressCompact> FileCompactedEvent;
        public event Action CompactCompletedEvent;
        public event Action UnpackCompletedEvent;
        private void FileUnpackedEventHandler(ProgressUnpack p)
        {

        }
        private void FileCompactedEventHandler(ProgressCompact p)
        {

        }
        private void CompactCompletedEventHandler()
        {

        }
        private void UnpackCompletedEventHandler()
        {

        }
        #endregion

        private const string DB_COL_NAME = "FileEntries";

        /// <summary>
        /// Compact directory into Database. This uses data deduplication, so there is only one instance of each file
        /// </summary>
        /// <param name="dbPath">Path to the database. Use something like "Data.db" if you want it in the same folder as the exe</param>
        /// <param name="folder">Folder you want to compact</param>
        /// <exception cref="Exception"></exception>
        /// <exception cref="DirectoryNotFoundException"></exception>
        public void Compact(string dbPath, string folder)
        {
            FileCompactedEvent += new Action<ProgressCompact>(FileCompactedEventHandler);
            CompactCompletedEvent += new Action(CompactCompletedEventHandler);
            if (string.IsNullOrEmpty(dbPath)) throw new Exception();
            using (var db = new LiteDatabase(dbPath))
            {
                if (!Directory.Exists(folder)) { throw new DirectoryNotFoundException(); }
                var col = db.GetCollection<CompactFileInfo>(DB_COL_NAME);
                var allFiles = Directory.GetFiles(folder, "*", SearchOption.AllDirectories);
                var currentHashes = new List<ulong>();
                var currentCount = 0;
                foreach (var filePath in allFiles)
                {
                    var hash = XXHash.XXH64(File.ReadAllBytes(filePath));
                    var compactInfo = new CompactFileInfo(filePath.Replace(folder, ""), hash);
                    col.Insert(compactInfo);
                    if (!currentHashes.Contains(hash))
                    {
                        currentHashes.Add(hash);
                        var storage = db.GetStorage<ulong>();
                        storage.Upload(hash, filePath);
                    }
                    currentCount++;
                    FileCompactedEvent(new ProgressCompact() { FilePackPath = filePath, CurrentFile = currentCount, TotalFiles = allFiles.Length });
                }
                col.EnsureIndex(x => x.Id);
            }
            CompactCompletedEvent();
        }

        /// <summary>
        /// Unpack the files from the database to the paths specified in the <see cref="ExtractInfo"/> object
        /// </summary>
        /// <param name="dbPath">Path to the database. Use something like "Data.db" if you want it in the same folder as the exe</param>
        /// <param name="extractEntries">List of <see cref="ExtractInfo"/> entries. This should have the entries from <see cref="GetAllEntries(string)"/>, as well as the path you want the files to be saved to.</param>
        /// <exception cref="FileNotFoundException"></exception>        
        public void Unpack(string dbPath, IEnumerable<ExtractInfo> extractEntries)
        {
            FileUnpackedEvent += new Action<ProgressUnpack>(FileUnpackedEventHandler);
            UnpackCompletedEvent += new Action(UnpackCompletedEventHandler);
            if (!File.Exists(dbPath)) throw new FileNotFoundException();
            using (var db = new LiteDatabase(dbPath))
            {
                var currentCount = 0;
                var totalEntries = extractEntries.Count();
                foreach (var extract in extractEntries)
                {
                    var hash = extract.FileInfo.Hash;
                    var storage = db.GetStorage<ulong>();
                    storage.Download(hash, extract.ExtractPath, true);
                    currentCount++;
                    FileUnpackedEvent(new ProgressUnpack() { FileExtractPath = extract.ExtractPath, CurrentFile = currentCount, TotalFiles = totalEntries, Success = true });
                }
            }
            UnpackCompletedEvent();
        }

        /// <summary>
        /// Get a list of all file entries within the database
        /// </summary>
        /// <param name="dbPath">Path to the database. Use something like "Data.db" if you want it in the same folder as the exe</param>
        /// <returns></returns>
        public CompactFileInfo[] GetAllEntries(string dbPath)
        {
            List<CompactFileInfo> entries = new List<CompactFileInfo>();
            using (var db = new LiteDatabase(dbPath))
            {
                var col = db.GetCollection<CompactFileInfo>(DB_COL_NAME);
                entries.AddRange(col.FindAll());
            }
            return entries.ToArray();
        }

        
        public ulong FastHashFile(string fileName)
        {
            if (File.Exists(fileName))
            {
                return XXHash.XXH64(File.ReadAllBytes(fileName));
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// Validates that each file was correctly unpacked
        /// </summary>
        /// <param name="extractEntries">List of <see cref="ExtractInfo"/> entries.</param>
        /// <returns></returns>
        public List<bool> ValidateUnpacked(IEnumerable<ExtractInfo> extractEntries)
        {
            var validList = new List<bool>();
            foreach (var entry in extractEntries)
            {
                if (File.Exists(entry.ExtractPath))
                {
                    var fileHash = XXHash.XXH64(File.ReadAllBytes(entry.ExtractPath));
                    if(fileHash == entry.FileInfo.Hash)
                    {
                        validList.Add(true);
                    }
                    else
                    {
                        validList.Add(false);
                    }
                }
                else
                {
                    validList.Add(false);
                }
            }
            return validList;
        }

    }
}
