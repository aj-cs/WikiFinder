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
                string line;
                WikiItem current = null;


                while ((line = input.ReadLine()) != null)
                {
                    string[] words = line.Split(' ');

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

    public void Search(string searchStr)
    {
        WikiItem current = start;
        string lastTitle = null;

        while (current != null)
        {
            if (current.Str.Equals(searchStr, StringComparison.OrdinalIgnoreCase))
            {
                // we search backwards in the linked list to find the latest title
                WikiItem findTitle = current;

                string title = null;

                while (findTitle != null)
                {
                    if (findTitle.Str == "---END.OF.DOCUMENT---" || findTitle == start)
                    {
                        title = findTitle.Next?.Str ?? "Error: Unknown Title";
                        break;
                    }
                    findTitle = findTitle.Next;
                }
                if (title != null & title != lastTitle)
                {
                    Console.WriteLine($"- {title}");
                    lastTitle = title;
                }
            }
            current = current.Next;
        }

        if (lastTitle == null)
        {
            Console.WriteLine($"{searchStr} not found in any documents.");
        }

    }
}
