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
        /// <param name="useGzip">[Optional] Use Gzip compression on the files in order to save space.</param>
        /// <exception cref="Exception"></exception>
        /// <exception cref="DirectoryNotFoundException"></exception>
        public void Compact(string dbPath, string folder, bool useGzip = false)
        {
            FileCompactedEvent += new Action<ProgressCompact>(FileCompactedEventHandler);
            CompactCompletedEvent += new Action(CompactCompletedEventHandler);

            if (string.IsNullOrEmpty(dbPath)) throw new Exception();
            using (var db = new LiteDatabase(dbPath))
            {
                if (!Directory.Exists(folder)) { throw new DirectoryNotFoundException(); }
                var col = db.GetCollection<FileInfo>(DB_COL_NAME);
                var allFiles = Directory.GetFiles(folder, "*", SearchOption.AllDirectories);
                var currentHashes = new List<ulong>();
                var currentCount = 0;
                foreach (var filePath in allFiles)
                {
                    var fileBytes = File.ReadAllBytes(filePath);
                    var hash = XXHash.XXH64(fileBytes);
                    var fileInfoEntry = new FileInfo(filePath, hash, useGzip);
                    col.Insert(fileInfoEntry);

                    if (!currentHashes.Contains(hash))
                    {
                        currentHashes.Add(hash);
                        var fs = db.FileStorage;
                        fs.Upload($"$/{hash}", $"{hash}", useGzip ? new MemoryStream(GZipProcessor.GZCompress(fileBytes)) : new MemoryStream(fileBytes));
                    }
                    currentCount++;
                    FileCompactedEvent(new ProgressCompact() { FilePackPath = filePath, CurrentFile = currentCount, TotalFiles = allFiles.Length });                    
                }
                col.EnsureIndex(x => x.Id);
            }
            CompactCompletedEvent();
        }

        /// <summary>
        /// Get a list of all file entries within the database
        /// </summary>
        /// <param name="dbPath">Path to the database. Use something like "Data.db" if you want it in the same folder as the exe</param>
        /// <returns></returns>
        public FileInfo[] GetAllEntries(string dbPath)
        {
            List<FileInfo> entries = new List<FileInfo>();
            using (var db = new LiteDatabase(dbPath))
            {
                var col = db.GetCollection<FileInfo>(DB_COL_NAME);
                entries.AddRange(col.FindAll());
            }
            return entries.ToArray();
        }

        /// <summary>
        /// Get a list of all file entries within the database that match your query. The queries only support StartsWith() for now
        /// </summary>
        /// <param name="dbPath">Path to the database. Use something like "Data.db" if you want it in the same folder as the exe</param>
        /// <param name="queries">List of queries you want to check against the database.</param>
        /// <returns></returns>
        public FileInfo[] GetEntries(string dbPath, string[] queries)
        {
            List<FileInfo> entries = new List<FileInfo>();
            using (var db = new LiteDatabase(dbPath))
            {
                var col = db.GetCollection<FileInfo>(DB_COL_NAME);
                foreach (var query in queries)
                {
                    var data = col.Query().Where(x => x.FilePath.StartsWith(query)).ToList();
                    entries.AddRange(data);
                }
                entries.AddRange(col.FindAll());
            }
            return entries.ToArray();
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
                var fs = db.FileStorage;
                var currentCount = 0;
                var totalEntries = extractEntries.Count();
                foreach (var extract in extractEntries)
                {
                    var fsInfo = fs.FindById($"$/{extract.FileInfo.Hash}");
                    if(fsInfo != null)
                    {
                        if (extract.FileInfo.UseGzip)
                        {
                            var ms = new MemoryStream();
                            fsInfo.CopyTo(ms);
                            File.WriteAllBytes(extract.ExtractPath, GZipProcessor.GZDecompress(ms.ToArray()));
                        }
                        else
                        {
                            fsInfo.SaveAs(extract.ExtractPath, true);
                            
                        }
                        currentCount++;
                        FileUnpackedEvent(new ProgressUnpack() { FileExtractPath = extract.ExtractPath, CurrentFile = currentCount, TotalFiles = totalEntries, Success = true });
                    }
                    else
                    {
                        FileUnpackedEvent(new ProgressUnpack() { FileExtractPath = extract.ExtractPath, CurrentFile = currentCount, TotalFiles = totalEntries, Success = false });
                        //null
                    }
                }
            }
            UnpackCompletedEvent();
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
