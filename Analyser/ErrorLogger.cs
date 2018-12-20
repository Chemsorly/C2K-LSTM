using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyser
{
    public static class ErrorLogger
    {
        static List<String> ErrorMessages { get; } = new List<string>();

        public static void AddErrorMessage(String pMessage) {
            if (!ErrorMessages.Contains(pMessage))
            {
                ErrorMessages.Add(pMessage);
                Console.WriteLine(pMessage);
            }                
        }

        public static void WriteLogToFilesystem(String pPath) { System.IO.File.WriteAllLines(pPath, ErrorMessages); }

    }
}
