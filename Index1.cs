using System;
using System.Diagnostics;
using System.IO;
namespace SearchEngineProject;

internal class Index1
{
    private int TableCapacity = 1213; // Use a prime num. 11831 
    private const double LoadFactor =  0.75;
    private WikiItem[] table;
    private int counter;

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
        Stopwatch sw = new Stopwatch();
        sw.Start();
        table = new WikiItem[TableCapacity];
        try
        {
            using (StreamReader input = new StreamReader(filename, System.Text.Encoding.UTF8))
            {
                string line;
                string currentTitle = "";
                bool titleRead = false;
                while ((line = input.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line))
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
                        //Console.WriteLine(currentTitle);
                        titleRead = true;
                    }
                    else
                    {
                        string[] words = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        foreach (string word in words)
                        {
                            InsertWord(word, currentTitle);
                        }
                    }
                }
                sw.Stop();
                Console.WriteLine("Indexing took " + sw.Elapsed);
            }
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine("Error reading file " + filename);
        }
    }

    private void InsertWord(string word, string title)
    {
        word = word.ToLower();
        int index = Math.Abs(word.GetHashCode()) % TableCapacity; // we could implement our own hash function
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
        counter++;
        
        if (counter > TableCapacity * LoadFactor)
        {
            Rehash();
            //Console.WriteLine(TableCapacity);
        }
    }

    private void Rehash()
    {
        int newSize = TableCapacity * 2;
        WikiItem[] newTable = new WikiItem[newSize];
        
        for (int i = 0; i < TableCapacity; i++)
        {
            WikiItem current = table[i];
            while (current != null) 
            {
                WikiItem next = current.Next; 
                int index = Math.Abs(current.Str.GetHashCode()) % newSize;
                current.Next = newTable[index];
                newTable[index] = current;
                current = next;
            }
        }
        
        table = newTable;
        TableCapacity = newSize;
    }

    private WikiItem FindWord(string word)
    {
        word = word.ToLower();
        int index = Math.Abs(word.GetHashCode()) % TableCapacity; // case sensitive atm 
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
        Stopwatch sw = new Stopwatch();
        sw.Start();
        WikiItem current = FindWord(searchStr);
        if (current != null)
        {
            //Console.WriteLine("Found " + searchStr + " in:");
            DocumentLog log = current.Log;
            while (log != null)
            {
                //Console.WriteLine(log.Title);
                log = log.Next;
            }
            sw.Stop();
            Console.WriteLine($"Search took: {sw.Elapsed}");
            return true;
        }
        else
        {
            sw.Stop();
            Console.WriteLine($"Search took: {sw.Elapsed}");
            Console.WriteLine("No matches for " + searchStr + " found");
            return false;
        }
    }
}

