using System;
using System.Collections.Generic;
using System.IO;

namespace SearchEngineProject;
//RENAME RADIX TO COMPACT TRIE CUZ RADIX TRIE IS BAD
// Compressed Ternary Search Tree (TST) with path compression for multi-character segments
public class Index
{
    private class Node
    {
        public string Segment;       // one or more characters
        public Node Left, Equal, Right;
        public List<int> DocIds;
        public bool IsWordEnd;

        public Node(string seg)
        {
            Segment = seg;
            Left = Equal = Right = null;
            DocIds = new List<int>();
            IsWordEnd = false;
        }
    }

    private Node root;
    private Dictionary<string, int> titleToId;
    private List<string> idToTitle;

    public Index(string filename)
    {
        root = null;
        titleToId = new Dictionary<string, int>();
        idToTitle = new List<string>();
        LoadDocuments(filename);
    }

    private void LoadDocuments(string filename)
    {
        using var reader = new StreamReader(filename, System.Text.Encoding.UTF8);
        string line;
        string currentTitle = null;
        bool readTitle = false;
        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (line == "---END.OF.DOCUMENT---") { readTitle = false; currentTitle = null; continue; }
            if (!readTitle)
            {
                currentTitle = line;
                RegisterTitle(currentTitle);
                readTitle = true;
            }
            else
            {
                foreach (var raw in line.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    var word = Normalize(raw);
                    if (word.Length > 0)
                        Insert(word, titleToId[currentTitle]);
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

    private void Insert(string word, int docId)
    {
        root = InsertNode(root, word, docId);
    }

    private Node InsertNode(Node node, string word, int docId)
    {
        if (node == null)
        {
            node = new Node(word)
            {
                IsWordEnd = true,
                DocIds = new List<int> { docId }
            };
        }
        else
        {
            int common = CommonPrefixLength(node.Segment, word);
            if (common == 0)
            {
                // go left or right by lexicographic compare
                if (string.Compare(word, node.Segment, StringComparison.Ordinal) < 0)
                    node.Left = InsertNode(node.Left, word, docId);
                else
                    node.Right = InsertNode(node.Right, word, docId);
            }
            else if (common < node.Segment.Length)
            {
                // split current node
                string nodeSuffix = node.Segment.Substring(common);
                var child = new Node(nodeSuffix)
                {
                    Left = node.Equal,
                    Equal = node.Equal,
                    IsWordEnd = node.IsWordEnd,
                    DocIds = node.DocIds
                };
                node.Segment = node.Segment.Substring(0, common);
                node.IsWordEnd = false;
                node.DocIds = new List<int>();
                node.Equal = child;

                // insert remainder of word
                string wordSuffix = word.Substring(common);
                if (wordSuffix.Length == 0)
                {
                    node.IsWordEnd = true;
                    if (!node.DocIds.Contains(docId)) node.DocIds.Add(docId);
                }
                else
                {
                    node.Equal = InsertNode(node.Equal, wordSuffix, docId);
                }
            }
            else
            {
                // full match of node.Segment, continue down equal
                string rem = word.Substring(common);
                if (rem.Length == 0)
                {
                    node.IsWordEnd = true;
                    if (!node.DocIds.Contains(docId)) node.DocIds.Add(docId);
                }
                else
                {
                    node.Equal = InsertNode(node.Equal, rem, docId);
                }
            }
        }
        return node;
    }

    private int CommonPrefixLength(string s1, string s2)
    {
        int len = Math.Min(s1.Length, s2.Length);
        int i = 0;
        while (i < len && s1[i] == s2[i]) i++;
        return i;
    }

    public bool Search(string query)
    {
        var node = FindNode(root, query);
        return node != null && node.IsWordEnd;
    }

    private Node FindNode(Node node, string query)
    {
        if (node == null || query.Length == 0) return null;
        int common = CommonPrefixLength(node.Segment, query);
        if (common == 0)
        {
            if (string.Compare(query, node.Segment, StringComparison.Ordinal) < 0)
                return FindNode(node.Left, query);
            else
                return FindNode(node.Right, query);
        }
        if (common < node.Segment.Length) return null;
        string rem = query.Substring(common);
        if (rem.Length == 0) return node;
        return FindNode(node.Equal, rem);
    }

    public List<string> PrefixSearchDocuments(string prefix)
    {
        var docs = new HashSet<int>();
        CollectDocsWithPrefix(root, prefix, docs);
        var result = new List<string>();
        foreach (int id in docs) result.Add(idToTitle[id]);
        return result;
    }

    private void CollectDocsWithPrefix(Node node, string prefix, HashSet<int> docs)
    {
        if (node == null) return;
        int common = CommonPrefixLength(node.Segment, prefix);
        if (common == 0)
        {
            if (string.Compare(prefix, node.Segment, StringComparison.Ordinal) < 0)
                CollectDocsWithPrefix(node.Left, prefix, docs);
            else
                CollectDocsWithPrefix(node.Right, prefix, docs);
        }
        else if (common < prefix.Length && common == node.Segment.Length)
        {
            string sub = prefix.Substring(common);
            CollectDocsWithPrefix(node.Equal, sub, docs);
        }
        else if (common == prefix.Length)
        {
            CollectAllDocs(node, docs);
        }
    }

    private void CollectAllDocs(Node node, HashSet<int> docs)
    {
        if (node == null) return;
        if (node.IsWordEnd)
            foreach (int id in node.DocIds) docs.Add(id);
        CollectAllDocs(node.Left, docs);
        CollectAllDocs(node.Equal, docs);
        CollectAllDocs(node.Right, docs);
    }

    public List<(string word, List<string> docs)> PrefixSearch(string prefix)
    {
        var results = new List<(string, List<string>)>();
        CollectWordsWithPrefix(root, prefix, "", results);
        return results;
    }

    private void CollectWordsWithPrefix(Node node, string prefix, string acc, List<(string, List<string>)> outp)
    {
        if (node == null) return;
        CollectWordsWithPrefix(node.Left, prefix, acc, outp);
        int common = CommonPrefixLength(node.Segment, prefix);
        if (common == node.Segment.Length)
        {
            string newAcc = acc + node.Segment;
            if (prefix.Length <= common)
            {
                if (node.IsWordEnd)
                    outp.Add((newAcc, MapDocs(node.DocIds)));
                CollectSubtreeWords(node.Equal, newAcc, outp);
            }
            else
            {
                string sub = prefix.Substring(common);
                CollectWordsWithPrefix(node.Equal, sub, newAcc, outp);
            }
        }
        CollectWordsWithPrefix(node.Right, prefix, acc, outp);
    }

    private void CollectSubtreeWords(Node node, string acc, List<(string, List<string>)> outp)
    {
        if (node == null) return;
        CollectSubtreeWords(node.Left, acc, outp);
        string newAcc = acc + node.Segment;
        if (node.IsWordEnd)
            outp.Add((newAcc, MapDocs(node.DocIds)));
        CollectSubtreeWords(node.Equal, newAcc, outp);
        CollectSubtreeWords(node.Right, acc, outp);
    }

    private List<string> MapDocs(List<int> ids)
    {
        var list = new List<string>(ids.Count);
        foreach (var i in ids) list.Add(idToTitle[i]);
        return list;
    }

    private string Normalize(string raw)
    {
        return raw.Trim().ToLowerInvariant();
    }
}
