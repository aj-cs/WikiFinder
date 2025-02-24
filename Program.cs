using System;
namespace SearchEngineProject;
class Program
{
    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: Index1 <filename>");
            return;
        }

        Console.WriteLine("Preprocessing " + args[0]);
        Index index = new Index(args[0]);

        while (true)
        {
            Console.WriteLine("Input search string or type exit to stop");
            string searchStr = Console.ReadLine();

            if (searchStr.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            index.PrefixSearchDocuments(searchStr);
            //Console.WriteLine($"\nAuto-completion of words starting with '{searchStr}': ");
            //index.PrefixSearch(searchStr);
        }
    }
}

