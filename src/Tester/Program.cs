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
            comp.Compact("datafile.db", "rootFolder", true);
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
