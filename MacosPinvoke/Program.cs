using System;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace MacosPinvoke
{
    class Program
    {
        /*
         * vm_size_t = ulong
         * vm_offset_t = ulong
         * mach_msg_type_number_t = uint
         */
        [DllImport("libSystem.dylib")]
        static extern uint mach_task_self(); // current_task() will also work

        [DllImport("libSystem.dylib")]
        static extern uint task_for_pid(uint task, int pid, out uint port);

        [DllImport("libSystem.dylib")]
        static extern int vm_protect(uint task, IntPtr address, int size, bool set_maximum, int new_protection);

        [DllImport("libSystem.dylib")]
        static extern int vm_allocate(uint task, out IntPtr address, ulong size, int new_protection);

        [DllImport("libSystem.dylib")]
        static extern int vm_write(uint task, IntPtr toAddress, IntPtr fromAddress, int length);

        [DllImport("libSystem.dylib")]
        static extern int vm_read(uint task, IntPtr address, ulong length, out IntPtr copyTo, out uint dataCount);

        delegate int AddDlg(int val);
        static AddDlg Add;

        // C# .NET Core 2.1
        // Playing with memory on MacOS
        // by PuL9

        // ❗️you should enable unsafe code in project settings for this to compile

        static void Main(string[] args)
        {
            // i couldn't find any single line of C# code on the internet on working with memory on macos, so i had to figure it out myself
            // if you know how to create remote thread (CreateRemoteThread() from Windows) contact me please
            /*unsafe
            {
                // Reading int from memory. Reading strings, arrays etc.. is more complicated than that
                int val = 79;
                IntPtr adr = IntPtr.Zero;
                uint dataCount = 0;

                vm_read(mach_task_self(), new IntPtr(&val), 4, out adr, out dataCount);
                
                Console.WriteLine($"We readed this: {(*(int*)adr)} from this address: 0x{adr.ToString("X")}"); // data we readed from memory

                // if u wanna read memory from another process, check the cheat engine app patching method where i patched external process
            }*/


            /*unsafe
            {
                // this will patch CHEAT ENGINE TUTORIAL APP level 1 - hit will not decrease health. ROOT required
                var plist = Process.GetProcessesByName("tutorial-i386");
                Process p = null;
                if (plist.Length != 0)
                {
                    p = plist[0];
                }
                else
                {
                    Console.WriteLine("Cheat engine tutorial process not found");
                    return;
                }
                uint port = 0;
                task_for_pid(mach_task_self(), p.Id, out port);
                if (port == 0)
                {
                    Console.WriteLine("RUN THIS AS ROOT!!!");
                }
                byte[] nops = { 0x90, 0x90 }; // sub eax,edx 29 D0
                int result = vm_protect(port, new IntPtr(0x00038CAB), 2, false, 7);
                Console.WriteLine($"RWX: {(result == 0 ? "Success" : "Failed")}");
                result = vm_write(port, new IntPtr(0x00038CAB), GetObjectAddress(nops), 2);
                Console.WriteLine($"Patch: {(result == 0 ? "Success" : "Failed")}");
            }*/


            // ASM inline :D            
            byte[] funcBytes = { 0x55, 0x48, 0x89, 0xe5, 0x89, 0x7d, 0xfc, 0x8b, 0x45, 0xfc, 0x01, 0xc0, 0x5d, 0xc3 };
            IntPtr funcAdr = IntPtr.Zero;
            vm_allocate(mach_task_self(), out funcAdr, (ulong)funcBytes.Length, 0x0001);
            Console.WriteLine($"{funcBytes.Length} bytes allocated at: {funcAdr.ToString("X")}");
            vm_write(mach_task_self(), funcAdr, GetObjectAddress(funcBytes), funcBytes.Length);
            Console.WriteLine($"Function bytes copied to {funcAdr.ToString("X")}");
            int res = vm_protect(mach_task_self(), funcAdr, funcBytes.Length, false, 0x01 | 0x02 | 0x04);
            Console.WriteLine($"{funcAdr.ToString("X")} is now Read/Write/Execute");
            Add = Marshal.GetDelegateForFunctionPointer<AddDlg>(funcAdr);
            Console.WriteLine($"Function result: {Add(1)}");
        }

        static IntPtr GetObjectAddress(object obj)
        {
            return GCHandle.Alloc(obj, GCHandleType.Pinned).AddrOfPinnedObject();
        }
    }
}