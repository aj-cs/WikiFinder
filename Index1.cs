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
                String line;
                WikiItem current = null;

                while ((line = input.ReadLine()) != null)
                {
                    String[] words = line.Split(' ');

                    foreach (string element in words)
                    {
                        Console.WriteLine(element);

                        WikiItem tmp = new WikiItem(element, null);

                        if (start == null)
                        {
                            start = tmp;
                            current = start;
                        }
                        else
                        {
                            current.Next = tmp;
                            current = tmp;
                        }
                    }
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
