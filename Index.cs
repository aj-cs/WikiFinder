using System;
using System.IO;
namespace SearchEngineProject;

public class Index
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

    public Index(string filename)
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
                            //Console.WriteLine(element);

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
    
        public bool Search(string searchStr)
        {
            WikiItem current = start;
            bool foundAny = false;
            string title = "";

            while (current != null)
            {
                bool inDocument = false;

                while (current != null && !current.Str.Contains(".")) // include the full title which ends on a period (.)
                {
                    title += " " + current.Str;
                    current = current.Next;
                }

                if (current != null) //make sure that the title also includes the word with the period
                {
                    title += " " + current.Str;
                    current = current.Next;
                }

                while (current != null && !current.Str.Equals("---END.OF.DOCUMENT---")) // search for the given search string
                {
                    if (current.Str.Equals(searchStr, StringComparison.OrdinalIgnoreCase))
                    {
                        inDocument = true;
                    }

                    current = current.Next;
                }

                if(inDocument)
                {
                    //Console.WriteLine($"Found {searchStr} in{title}");
                    foundAny = true;
                }

                if (current != null && current.Str.Equals("---END.OF.DOCUMENT---"))
                {
                    current = current.Next;
                }

                title = "";
            }

            if (!foundAny)
            {
                //Console.WriteLine("No matches for " + searchStr + " found");
            }
            return foundAny;
        }
    
}
