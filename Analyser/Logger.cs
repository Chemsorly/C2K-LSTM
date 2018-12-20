using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyser
{
    public static class Logger
    {
        static List<String> ErrorMessages { get; } = new List<string>();
        static List<String> LogMessages { get; } = new List<string>();

        public static void AddLogMessage(String pMessage, bool pVerbose)
        {
            LogMessages.Add(pMessage);
            if(pVerbose)
                Console.WriteLine(pMessage);
        }

        public static void AddErrorMessage(String pMessage) {
            if (!ErrorMessages.Contains(pMessage))
            {
                ErrorMessages.Add(pMessage);
                Console.WriteLine(pMessage);
            }                
        }

        /// <summary>
        /// returns a copy of the error list, effectively rendering it ineditable (except the strings)
        /// </summary>
        /// <returns>a list of error messages</returns>
        public static List<String> GetErrorMessages()
        {
            return ErrorMessages.ToList();
        }

        public static void WriteErrorLogToFilesystem(String pPath) { System.IO.File.WriteAllLines(pPath, ErrorMessages); }
        public static void WriteLogToFilesystem(String pPath) { System.IO.File.WriteAllLines(pPath, LogMessages); }

    }
}
