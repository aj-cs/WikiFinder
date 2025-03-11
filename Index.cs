using System;
using System.Collections.Generic;
using System.IO;

namespace SearchEngineProject;
//RENAME RADIX TO COMPACT TRIE CUZ RADIX TRIE IS BAD
public class Index
{
    private TrieNode root;

    private class TrieNode
    {
        public string Segment;
        public Dictionary<char, TrieNode> Children;
        public DocumentLog Log;
        public int Count;
        public bool EndOfWord;

        public TrieNode(string segment)
        {
            Segment = segment;
            Children = new Dictionary<char, TrieNode>();
            Log = null;
            Count = 0;
            EndOfWord = false;
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
        root = new TrieNode("");
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
                        // Console.WriteLine(currentTitle);
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
            }
        }
        catch (FileNotFoundException)
        {
            //Console.WriteLine("Error reading file " + filename);
        }
    }

    private void InsertWord(string word, string title)
    {
        word = word.ToLower();
        Insert(root, word, title);
    }
    //recursive insertion into the tree
    // node is the current node and key is the substring that we need to insert
    private void Insert(TrieNode node, string key, string title)
    {
        // when key is empty, mark current node as a word
        if (key.Length == 0)
        {
            node.EndOfWord = true;
            node.Count++;
            if (node.Log == null)
            {
                node.Log = new DocumentLog(title, null);
            }
            else if (!DocExists(node.Log, title))
            {
                node.Log = new DocumentLog(title, node.Log);
            }
            return;
        }

        char firstChar = key[0];

        // check if a child with a segment starting with firstChar exists
        if (!node.Children.TryGetValue(firstChar, out TrieNode child))
        {
            // no child exists so we create a new one with the entire key
            TrieNode newNode = new TrieNode(key);
            newNode.EndOfWord = true;
            newNode.Count = 1;
            newNode.Log = new DocumentLog(title, null);
            node.Children[firstChar] = newNode;
            return;
        }
        // compute the longest common prefix length between the childs segment and the key
        int commonLength = CommonPrefixLength(child.Segment, key);

        if (commonLength < child.Segment.Length)
        {
            // partial match which means we gotta split the child
            // create a new node for the remainder of the childs segment
            string childSuffix = child.Segment.Substring(commonLength);
            TrieNode splitNode = new TrieNode(childSuffix)
            {
                Children = child.Children,
                EndOfWord = child.EndOfWord,
                Count = child.Count,
                Log = child.Log
            };

            // update the child node by setting its segment to the common prefix
            child.Segment = child.Segment.Substring(0, commonLength);

            //resett children dict and add split node
            child.Children = new Dictionary<char, TrieNode>();
            child.Children[childSuffix[0]] = splitNode;
            // if the key exactly matches the common pref
            if (commonLength == key.Length)
            {
                child.EndOfWord = true;
                child.Count++;
                if (child.Log == null)
                {
                    child.Log = new DocumentLog(title, null);
                }
                else if (!DocExists(child.Log, title))
                {
                    child.Log = new DocumentLog(title, child.Log);
                }
            }
            else
            {
                // insert the remainder of the key as a new child
                string keySuffix = key.Substring(commonLength);
                TrieNode newChild = new TrieNode(keySuffix);
                newChild.EndOfWord = true;
                newChild.Count = 1;
                newChild.Log = new DocumentLog(title, null);
                child.Children[keySuffix[0]] = newChild;
            }
            return;
        }
        // commonLength == child.Segment.Length (child seg fully matches pref of key)
        else
        {
            string keyRemainder = key.Substring(commonLength);
            if (keyRemainder.Length == 0)
            {
                //key exactly matches the childs segment
                child.EndOfWord = true;
                child.Count++;
                if (child.Log == null)
                {
                    child.Log = new DocumentLog(title, null);
                }
                else if (!DocExists(child.Log, title))
                {
                    child.Log = new DocumentLog(title, child.Log);
                }
                return;
            }
            // proceed with insertion of child node
            Insert(child, keyRemainder, title);
        }
    }
    // returns the length of the common prefix between two strings
    private int CommonPrefixLength(string s1, string s2)
    {
        int len = Math.Min(s1.Length, s2.Length);
        int i = 0;
        while (i < len && s1[i] == s2[i])
            i++;
        return i;
    }


    // find node corresponding to complete word
    private TrieNode FindWord(string word)
    {
        word = word.ToLower();
        TrieNode current = root;
        while (word.Length > 0)
        {
            char firstChar = word[0];
            if (!current.Children.TryGetValue(firstChar, out TrieNode child))
            {
                return null;
            }

            // the childs segment must matches the beginning of the word
            if (word.StartsWith(child.Segment))
            {
                word = word.Substring(child.Segment.Length);
                current = child;
            }
            else
            {
                return null;
            }
        }
        return current.EndOfWord ? current : null;

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

    // NOTE: exact match search (unchanged in terms of implementation compared to like iondex4)
    public bool Search(string searchStr)
    {
        TrieNode current = FindWord(searchStr);
        if (current != null)
        {
            // Console.WriteLine("Found " + searchStr + " in:");
            DocumentLog log = current.Log;
            while (log != null)
            {
                // Console.WriteLine(log.Title);
                log = log.Next;
            }
            return true;
        }
        else
        {
            // Console.WriteLine("No matches for " + searchStr + " found");
            return false;
        }
    }

    // helper method to find node corresponding to prefix returns tuple of (matching node, accumualted string that's been matched)
    private (TrieNode node, string matched) FindPrefixNode(string prefix)
    {
        prefix = prefix.ToLower();
        TrieNode current = root;
        string matched = "";
        while (prefix.Length > 0)
        {
            char firstChar = prefix[0];
            if (!current.Children.TryGetValue(firstChar, out TrieNode child))
            {
                return (null, null);
            }
            int commonLength = CommonPrefixLength(child.Segment, prefix);
            matched += child.Segment.Substring(0, commonLength);
            if (commonLength < child.Segment.Length)
            {
                // if the prefix ends in the middle of the child's segment it's a match
                if (commonLength == prefix.Length)
                {
                    return (child, matched);
                }
                else { return (null, null); }
            }
            // childs segement fully matches so we continue with remainder of prefix
            prefix = prefix.Substring(commonLength);
            current = child;
        }
        return (current, matched);
    }
    // NOTE: auto-completion, lists all words starting with the given prefix and their documents
    //TODO: make searches return boolean instead of void
    public bool PrefixSearch(string prefix)
    {
        var (node, matched) = FindPrefixNode(prefix);
        if (node == null)
        {
            // Console.WriteLine("No matches for " + prefix + " found");
            return false;
        }

        List<(string word, TrieNode node)> completions = new List<(string, TrieNode)>();

        // matched is the portion of the pref that was found, we use it as the starting point

        CollectWords(node, matched, completions);

        if (completions.Count == 0)
        {
            // Console.WriteLine("No complete words found with prefix " + prefix);
            return false;
        }
        else
        {
            foreach (var (word, radNode) in completions)
            {
                // Console.WriteLine("Found " + word + " in:");
                DocumentLog log = radNode.Log;
                while (log != null)
                {
                    // Console.WriteLine(log.Title);
                    log = log.Next;
                }
            }
        }
        return true;
    }

    // NOTE: returns the (unique) documents  where any word starting with the given prefix appears
    public bool PrefixSearchDocuments(string prefix)
    {
        var (node, matched) = FindPrefixNode(prefix);
        if (node == null)
        {
            // Console.WriteLine("No matches for " + prefix + " found");
            return false;
        }

        // hashset to avoid duplicate titles
        HashSet<string> documents = new HashSet<string>();

        List<(string word, TrieNode node)> completions = new List<(string, TrieNode)>();
        CollectWords(node, matched, completions);

        foreach (var (word, radNode) in completions)
        {
            DocumentLog log = radNode.Log;
            while (log != null)
            {
                documents.Add(log.Title);
                log = log.Next;
            }
        }

        if (documents.Count == 0)
        {
            // Console.WriteLine($"No documents found for prefix: '{prefix}'");
            return false;
        }
        else
        {
            // Console.WriteLine($"Documents containing a word starting with '{prefix}'");

            foreach (var doc in documents)
            {
                // Console.WriteLine(doc);
            }
        }
        return true;
    }

    /*
     * helper to locate node corresponding to the last character of the prefix
     */

    // recursively collects complete words (nodes marked as isEndOfWord) from a given node
    //  prefix is the accumulated string that leads to that node
    private void CollectWords(TrieNode node, string prefix, List<(string, TrieNode)> completions)
    {
        if (node.EndOfWord)
        {
            completions.Add((prefix, node));
        }
        foreach (var child in node.Children.Values)
        {
            // append the childs segement to the current prefix and recusre
            CollectWords(child, prefix + child.Segment, completions);
        }

    }
}

