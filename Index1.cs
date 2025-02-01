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
                        if (element != "")
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


        }
        catch (FileNotFoundException)
        {
            Console.WriteLine("Error reading file " + filename);
        }
        }

    //TODO Make sure to include full title of document in title var 
        public bool Search(string searchStr)
        {
            WikiItem current = start;
            bool foundAny = false;

            while (current != null)
            {
                string title = current.Str; //first item in linked list is the title
                bool inDocument = false;

                current = current.Next;

                while (current != null && !current.Str.Equals("---END.OF.DOCUMENT---"))
                {
                    if (current.Str.Equals(searchStr, StringComparison.OrdinalIgnoreCase))
                    {
                        inDocument = true;
                    }

                    current = current.Next;
                }

                if(inDocument)
                {
                    Console.WriteLine($"Found {searchStr} in {title}");
                    foundAny = true;
                }

                if (current != null && current.Str.Equals("---END.OF.DOCUMENT---"))
                {
                    current = current.Next;
                }
        }

            if (!foundAny)
            {
                Console.WriteLine("No matches for " + searchStr + " found");
            }
            return foundAny;
        }
    
}
