using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace BotManager
{
    class Program
    {

        public static int m;

        public static bool IsProcessOpen(string name)
        {

            foreach (Process clsProcess in Process.GetProcesses())
            {
                if (clsProcess.ProcessName == name)
                {
                    return true;
                }
            }
            return false;
        }

        public static string[] SplitIt1(string buf)
        {
            string[] seps = new string[] { "[", "]" };
            string[] buf1 = buf.Split(seps, StringSplitOptions.RemoveEmptyEntries);
            return buf1;
        }

        static void Main(string[] args)
        {
            string cfgtxt = System.IO.File.ReadAllText("Mgrcfg.txt");
            string[] cfg = SplitIt1(cfgtxt);
            m = Int32.Parse(cfg[1]);

            while (true)
            {
                Console.Clear();
                if ((IsProcessOpen("Fatumbot") == false) && (System.IO.File.Exists("Fatumbot.exe")))
                {
                    Console.WriteLine("Fatum is down");
                    System.Diagnostics.Process.Start("Fatumbot.exe"); 
                }
                if ((IsProcessOpen("Fatumbot") == false)) { Console.WriteLine("Fatum is down"); } else { Console.WriteLine("Fatum is working"); }
                if (System.IO.File.Exists("Fatumbot.exe") == false) { Console.WriteLine("Fatumbot.exe doesn't exist"); }
                System.Threading.Thread.Sleep(60000 * m);

            }
        }
    }
}
