using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DedupeCompressor;

namespace Tester
{
    internal class Program
    {
        
        static void Main(string[] args)
        {
            
            var comp = new DataCompressor();
            comp.CompactCompletedEvent += Comp_CompactCompletedEvent;
            comp.Compact("datafile.db", "rootFolder");

            var entries = comp.GetAllEntries("datafile.db");
            var extractList = new List<ExtractInfo>();
            foreach (var item in entries)
            {
                var extractEntry = new ExtractInfo() { FileInfo = item, ExtractPath = Path.Combine(@"C:\Users\Micah\source\repos\tmp\src\Tester\bin\Debug\output", Path.GetFileName(item.FilePath)) };
                extractList.Add(extractEntry);
            }
            comp.Unpack("datafile.db", extractList);
            var foo = comp.ValidateUnpacked(extractList);
            var result = foo.All(x => x == true);
            Console.ReadLine();
        }

        private static void Comp_CompactCompletedEvent()
        {
            Console.WriteLine("Compact Completed");
        }

        private static void Comp_FileCompactedEvent(ProgressCompact obj)
        {
            Console.WriteLine($"MAINPROG:{obj.FilePackPath}");
        }
    }
}
