using System;
using System.IO;
namespace SearchEngineProject
{
    internal class Index1
    {
        private WikiItem start;

        private class WikiItem
        {
            public string Str;
            public DocumentLog Log;
            public WikiItem Next;
            public WikiItem(string s, DocumentLog l, WikiItem n)
            {
                Str = s;
                Log = l;
                Next = n;
            }
            
        }

        private class DocumentLog
        {
            public string Title;
            public DocumentLog Next;
            public DocumentLog(string title, DocumentLog next)
            {
                Title = title;
                Next = next;
            }
        }

        public Index1(string filename)
        {
            try
            {
                using (StreamReader input = new StreamReader(filename, System.Text.Encoding.UTF8))
                {
                    string line;
                    string currentTitle = "";
                    bool titleRead = false;
                    while ((line = input.ReadLine()) != null)
                    {
                        if (line == "")
                            continue;
                        if (line.Equals("---END.OF.DOCUMENT---", StringComparison.OrdinalIgnoreCase))
                        {
                            titleRead = false;
                            currentTitle = "";
                            continue;
                        }
                        if (!titleRead)
                        {
                            currentTitle = line;
                            titleRead = true;
                        }
                        else
                        {
                            string[] words = line.Split(' ');
                            foreach (string word in words)
                            {
                                if (word != "")
                                    InsertWord(word, currentTitle);
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

        private void InsertWord(string word, string title)
        {
            WikiItem current = FindWord(word);
            if (current == null)
            {
                current = new WikiItem(word, new DocumentLog(title, null), start);
                start = current;
            }
            else
            {
                if (!DocExists(current.Log, title))
                    current.Log = new DocumentLog(title, current.Log);
            }
        }

        private WikiItem FindWord(string word)
        {
            WikiItem current = start;
            while (current != null)
            {
                if (current.Str.Equals(word, StringComparison.OrdinalIgnoreCase))
                    return current;
                current = current.Next;
            }
            return null;
        }

        private bool DocExists(DocumentLog log, string title)
        {
            while (log != null)
            {
                if (log.Title.Equals(title, StringComparison.OrdinalIgnoreCase))
                    return true;
                log = log.Next;
            }
            return false;
        }

        public bool Search(string searchStr)
        {
            WikiItem current = FindWord(searchStr);
            if (current != null)
            {
                Console.WriteLine("Found " + searchStr + " in:");
                DocumentLog log = current.Log;
                while (log != null)
                {
                    Console.WriteLine(log.Title);
                    log = log.Next;
                }
                return true;
            }
            else
            {
                Console.WriteLine("No matches for " + searchStr + " found");
                return false;
            }
        }
    }
}
