using System;
using System.IO;
namespace SearchEngineProject;

internal class Index1
{
    private WikiItem start;

    private class DocumentItem
    {
        public string Title;
        public DocumentItem? Next;

        public DocumentItem(string title, DocumentItem next)
        {
            Title = title;
            Next = next;
        }

    }
    private class WikiItem
    {
        public string Str;
        public DocumentItem DocumentList;
        public WikiItem? Next;

        public WikiItem(string s, WikiItem n)
        {
            Str = s;
            DocumentList = null;
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
                string currentTitle = null;
                start = null;

                while ((line = input.ReadLine()) != null)
                {
                    if (start == null || line == "---END.OF.DOCUMENT---")
                    {
                        currentTitle = input.ReadLine();
                        if (currentTitle == null)
                        {
                            break;
                        }
                    }
                    string[] words = line.Split(' ');
                    foreach (string word in words)
                    {
                        Console.WriteLine(word);
                        WikiItem node = FindOrCreateWord(word);
                        AddDocumentToWord(node, currentTitle);
                    }
                }
            }
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine("Error reading file " + filename);
        }
    }

    private void AddDocumentToWord(WikiItem node, string title)
    {
        DocumentItem doc = node.DocumentList;
        while (doc != null)
        {
            if (doc.Title.Equals(title, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            doc = doc.Next;
        }
        node.DocumentList = new DocumentItem(title, node.DocumentList);
    }

    private WikiItem FindOrCreateWord(string word)
    {
        WikiItem current = start;
        WikiItem previous = null;

        while (current != null)
        {
            if (current.Str.Equals(word, StringComparison.OrdinalIgnoreCase))
            {
                return current;
            }

            previous = current;
            current = current.Next;
        }

        WikiItem newWord = new WikiItem(word, null);
        if (previous == null)
        {
            start = newWord;
        }
        else
        {
            previous.Next = newWord;
        }
        return newWord;
    }

    public void Search(string searchStr)
    {
        WikiItem current = start;
        while (current != null)
        {
            if (current.Str.Equals(searchStr, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"The search string '{searchStr}' appears in: ");
                DocumentItem doc = current.DocumentList;
                while (doc != null)
                {
                    Console.WriteLine($"- {doc.Title}");
                    doc = doc.Next;
                }
            }
            current = current.Next;
        }
        Console.WriteLine($"'{searchStr}' could not be found in any document");
    }
}
