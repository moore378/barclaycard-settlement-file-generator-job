using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace MjhGeneral
{
    // Based on code from http://social.msdn.microsoft.com/Forums/en-US/clr/thread/6c8d3529-a493-49b9-93d7-07a3a2d715dc
    public static class MiniDump
    {
        [Flags]
        public enum MINIDUMP_TYPE 
        {
            MiniDumpNormal = 0x00000000,
            MiniDumpWithDataSegs = 0x00000001,
            MiniDumpWithFullMemory = 0x00000002,
            MiniDumpWithHandleData = 0x00000004,
            MiniDumpFilterMemory = 0x00000008,
            MiniDumpScanMemory = 0x00000010,
            MiniDumpWithUnloadedModules = 0x00000020,
            MiniDumpWithIndirectlyReferencedMemory = 0x00000040,
            MiniDumpFilterModulePaths = 0x00000080,
            MiniDumpWithProcessThreadData = 0x00000100,
            MiniDumpWithPrivateReadWriteMemory = 0x00000200,
            MiniDumpWithoutOptionalData = 0x00000400,
            MiniDumpWithFullMemoryInfo = 0x00000800,
            MiniDumpWithThreadInfo = 0x00001000,
            MiniDumpWithCodeSegs = 0x00002000,
            MiniDumpWithoutAuxiliaryState = 0x00004000,
            MiniDumpWithFullAuxiliaryState = 0x00008000,
            MiniDumpWithPrivateWriteCopyMemory = 0x00010000,
            MiniDumpIgnoreInaccessibleMemory = 0x00020000,
            MiniDumpWithTokenInformation = 0x00040000,
            MiniDumpWithModuleHeaders = 0x00080000,
            MiniDumpFilterTriage = 0x00100000,
            MiniDumpValidTypeFlags = 0x001fffff
        }

        public const MINIDUMP_TYPE AllData =
            MINIDUMP_TYPE.MiniDumpWithDataSegs 
            | MINIDUMP_TYPE.MiniDumpWithFullMemory
            | MINIDUMP_TYPE.MiniDumpWithHandleData 
            | MINIDUMP_TYPE.MiniDumpWithIndirectlyReferencedMemory
            | MINIDUMP_TYPE.MiniDumpWithProcessThreadData
            | MINIDUMP_TYPE.MiniDumpWithPrivateReadWriteMemory
            | MINIDUMP_TYPE.MiniDumpWithFullMemoryInfo
            | MINIDUMP_TYPE.MiniDumpWithThreadInfo
            | MINIDUMP_TYPE.MiniDumpWithCodeSegs
            | MINIDUMP_TYPE.MiniDumpWithFullAuxiliaryState
            | MINIDUMP_TYPE.MiniDumpWithPrivateWriteCopyMemory
            | MINIDUMP_TYPE.MiniDumpIgnoreInaccessibleMemory
            ;

        [DllImport("dbghelp.dll")]
        static extern bool MiniDumpWriteDump(
            IntPtr hProcess,
            Int32 ProcessId,
            IntPtr hFile,
            MINIDUMP_TYPE DumpType,
            IntPtr ExceptionParam,
            IntPtr UserStreamParam,
            IntPtr CallackParam);
        
        public static void Dump(String fileToDump, MINIDUMP_TYPE flags = AllData)
        {
            FileStream fsToDump = null;
            if (File.Exists(fileToDump))
                File.Delete(fileToDump);
//                fsToDump = File.Open(fileToDump, FileMode.Append);
            //else
                fsToDump = File.Create(fileToDump);
            Process thisProcess = Process.GetCurrentProcess();
            MiniDumpWriteDump(thisProcess.Handle, thisProcess.Id,
                fsToDump.SafeFileHandle.DangerousGetHandle(), flags, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            fsToDump.Flush();
            fsToDump.Close();
        }
    }
}
