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
}
