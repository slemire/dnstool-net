using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DnsTool
{
    internal class Helpers
    {
        public static Dictionary<string, string> ParseTheArguments(string[] args)
        {
            try
            {
                Dictionary<string, string> ret = new Dictionary<string, string>();
                if (args.Length % 2 == 0 && args.Length > 0)
                {
                    for (int i = 0; i < args.Length; i = i + 2)
                    {
                        ret.Add(args[i].Substring(1).ToLower(), args[i + 1]);

                    }
                }
                return ret;
            }
            catch (ArgumentException)
            {
                Console.WriteLine("");
                Console.WriteLine("[-] You specified duplicate switches.");
                return null;
            }
        }        
    }
}
