using System;
using System.Collections.Generic;
using System.IO;

namespace SearchEngineProject;
//RENAME RADIX TO COMPACT TRIE CUZ RADIX TRIE IS BAD
public class Index
{
    private class TSTNode
    {
        public char Character;
        public TSTNode Left, Equal, Right;
        public List<int> DocIds;
        public bool IsEndOfWord;

        public TSTNode(char character)
        {
            Character = character;
            Left = Equal = Right = null;
            DocIds = new List<int>();
            IsEndOfWord = false;
        }
    }

    private TSTNode root;
    private Dictionary<string, int> titleToId;
    private List<string> idToTitle;

    public Index(string filename)
    {
        root = null;
        titleToId = new Dictionary<string, int>();
        idToTitle = new List<string>();

        using var input = new StreamReader(filename, System.Text.Encoding.UTF8);
        string line;
        string currentTitle = null;
        bool titleRead = false;

        while ((line = input.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (line.Equals("---END.OF.DOCUMENT---", StringComparison.Ordinal))
            {
                titleRead = false;
                currentTitle = null;
                continue;
            }
            if (!titleRead)
            {
                currentTitle = line;
                RegisterTitle(currentTitle);
                titleRead = true;
            }
            else
            {
                foreach (var rawWord in line.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    var word = Normalize(rawWord);
                    if (word.Length > 0)
                        InsertWord(word, currentTitle);
                }
            }
        }
    }

    private void RegisterTitle(string title)
    {
        if (!titleToId.ContainsKey(title))
        {
            titleToId[title] = idToTitle.Count;
            idToTitle.Add(title);
        }
    }

    private void InsertWord(string word, string title)
    {
        int docId = titleToId[title];
        root = Insert(root, word, 0, docId);
    }

    private TSTNode Insert(TSTNode node, string word, int idx, int docId)
    {
        char c = word[idx];
        if (node == null)
            node = new TSTNode(c);
        if (c < node.Character)
            node.Left = Insert(node.Left, word, idx, docId);
        else if (c > node.Character)
            node.Right = Insert(node.Right, word, idx, docId);
        else
        {
            if (idx < word.Length - 1)
                node.Equal = Insert(node.Equal, word, idx + 1, docId);
            else
            {
                node.IsEndOfWord = true;
                if (!node.DocIds.Contains(docId))
                    node.DocIds.Add(docId);
            }
        }
        return node;
    }

    public bool Search(string query)
    {
        var node = FindNode(root, query, 0);
        return node != null && node.IsEndOfWord;
    }

    private TSTNode FindNode(TSTNode node, string word, int idx)
    {
        if (node == null || idx >= word.Length) return null;
        char c = word[idx];
        if (c < node.Character)
            return FindNode(node.Left, word, idx);
        else if (c > node.Character)
            return FindNode(node.Right, word, idx);
        else
        {
            if (idx == word.Length - 1)
                return node;
            return FindNode(node.Equal, word, idx + 1);
        }
    }

    public List<string> PrefixSearchDocuments(string prefix)
    {
        var docs = new HashSet<int>();
        var startNode = FindPrefixNode(root, prefix, 0);
        if (startNode != null)
            CollectDocs(startNode, docs);
        var results = new List<string>();
        foreach (var id in docs)
            results.Add(idToTitle[id]);
        return results;
    }

    private TSTNode FindPrefixNode(TSTNode node, string prefix, int idx)
    {
        if (node == null || idx >= prefix.Length) return null;
        char c = prefix[idx];
        if (c < node.Character)
            return FindPrefixNode(node.Left, prefix, idx);
        else if (c > node.Character)
            return FindPrefixNode(node.Right, prefix, idx);
        else
        {
            if (idx == prefix.Length - 1)
                return node;
            return FindPrefixNode(node.Equal, prefix, idx + 1);
        }
    }

    private void CollectDocs(TSTNode node, HashSet<int> docs)
    {
        if (node == null) return;
        if (node.IsEndOfWord)
            foreach (var id in node.DocIds)
                docs.Add(id);
        CollectDocs(node.Left, docs);
        CollectDocs(node.Equal, docs);
        CollectDocs(node.Right, docs);
    }

    public List<(string word, List<string> documents)> PrefixSearch(string prefix)
    {
        var results = new List<(string, List<string>)>();
        var node = FindPrefixNode(root, prefix, 0);
        if (node != null)
            CollectWords(node, prefix, results);
        return results;
    }

    private void CollectWords(TSTNode node, string current, List<(string, List<string>)> output)
    {
        if (node == null) return;
        CollectWords(node.Left, current, output);
        var next = current + node.Character;
        if (node.IsEndOfWord)
        {
            var docs = new List<string>();
            foreach (var id in node.DocIds)
                docs.Add(idToTitle[id]);
            output.Add((next, docs));
        }
        CollectWords(node.Equal, next, output);
        CollectWords(node.Right, current, output);
    }

    private string Normalize(string raw)
    {
        return raw.Trim().ToLowerInvariant();
    }
}
