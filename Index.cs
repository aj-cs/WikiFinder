using System;
using System.IO;
namespace SearchEngineProject
{
    public class Index
    {
        private const int TSize = 11831; // Use a prime num. 11831 
        private WikiItem[] table;

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

        public Index(string filename)
        {
            table = new WikiItem[TSize];
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
                        if (line.Equals("---END.OF.DOCUMENT---"))
                        {
                            titleRead = false;
                            currentTitle = "";
                            continue;
                        }
                        if (!titleRead)
                        {
                            
                            currentTitle = line;
                            Console.WriteLine(currentTitle);
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
            int index = Math.Abs(word.GetHashCode()) % TSize; // we could implement our own hash function
            WikiItem current = table[index];

            while (current != null)
            {
                if (current.Str.Equals(word))
                {
                    if (!DocExists(current.Log, title))
                    {   
                        current.Log = new DocumentLog(title, current.Log); 
                    }
                    return;
                }

                current = current.Next;
            }
            // in case no word exists within the table yet
            table[index] = new WikiItem(word, new DocumentLog(title, null), table[index]);
            
        }

        private WikiItem FindWord(string word)
        {
            int index = Math.Abs(word.GetHashCode()) % TSize; // case sensitive atm 
            WikiItem current = table[index];
            while (current != null)
            {
                if (current.Str.Equals(word))
                    return current;
                current = current.Next;
            }
            return null;
        }

        private bool DocExists(DocumentLog log, string title)
        {
            while (log != null)
            {
                if (log.Title.Equals(title))
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
