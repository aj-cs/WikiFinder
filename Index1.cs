using System;
using System.IO;
namespace SearchEngineProject;

internal class Index1
{
    private WikiItem start;

    private class WikiItem
    {
        public string Str;
        public WikiItem Next;

        public WikiItem(string s, WikiItem n)
        {
            Str = s;
            Next = n;
        }
    }

    public Index1(string filename)
    {
        try
        {
            using (StreamReader input = new StreamReader(filename, System.Text.Encoding.UTF8))
            {
                string word = input.ReadLine();
                start = new WikiItem(word, null);
                WikiItem current = start;

                while ((word = input.ReadLine()) != null)
                {
                    Console.WriteLine(word);
                    WikiItem tmp = new WikiItem(word, null);
                    current.Next = tmp;
                    current = tmp;
                }
            }
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine("Error reading file " + filename);
        }
    }

    public bool Search(string searchStr)
    {
        WikiItem current = start;
        while (current != null)
        {
            if (current.Str.Equals(searchStr, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            current = current.Next;
        }

        return false;
    }

    public static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: Index1 <filename>");
            return;
        }

        Console.WriteLine("Preprocessing " + args[0]);
        Index1 index = new Index1(args[0]);

        while (true)
        {
            Console.WriteLine("Input search string or type exit to stop");
            string searchStr = Console.ReadLine();

            if (searchStr.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            if (index.Search(searchStr))
            {
                Console.WriteLine(searchStr + " exists");
            }
            else
            {
                Console.WriteLine(searchStr + " does not exist");
            }
        }
    }
}
