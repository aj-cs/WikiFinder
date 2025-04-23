using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;

namespace SearchEngineProject;
// Compressed Ternary Search Tree (TST) with path compression using pooled char slices
// Compressed Ternary Search Tree (TST) with path compression using pooled char slices
public class Index
{
    private class Node
    {
        public ReadOnlyMemory<char> Segment;  // immutable slice of characters
        public Node Left, Equal, Right;
        public List<int> DocIds;
        public bool IsWordEnd;

        public Node(ReadOnlyMemory<char> seg)
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
            if (line == "---END.OF.DOCUMENT---")
            {
                readTitle = false;
                currentTitle = null;
                continue;
            }
            if (!readTitle)
            {
                currentTitle = line;
                RegisterTitle(currentTitle);
                readTitle = true;
            }
            else
            {
                var words = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                foreach (var raw in words)
                {
                    var memory = Normalize(raw);
                    if (!memory.IsEmpty)
                        root = InsertNode(root, memory, titleToId[currentTitle]);
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

    private Node InsertNode(Node node, ReadOnlyMemory<char> word, int docId)
    {
        if (node == null)
        {
            var newNode = new Node(word) { IsWordEnd = true };
            newNode.DocIds.Add(docId);
            return newNode;
        }

        int common = CommonPrefixLength(node.Segment.Span, word.Span);
        if (common == 0)
        {
            if (word.Span[0] < node.Segment.Span[0])
                node.Left = InsertNode(node.Left, word, docId);
            else
                node.Right = InsertNode(node.Right, word, docId);
        }
        else if (common < node.Segment.Length)
        {
            // split node
            var suffixSegment = node.Segment.Slice(common);
            var child = new Node(suffixSegment)
            {
                Left = node.Equal,
                Equal = node.Equal,
                IsWordEnd = node.IsWordEnd,
                DocIds = node.DocIds
            };
            // reassign current node
            node.Segment = node.Segment.Slice(0, common);
            node.IsWordEnd = false;
            node.DocIds = new List<int>();
            node.Equal = child;

            // insert remainder of word
            var suffixWord = word.Slice(common);
            if (suffixWord.IsEmpty)
            {
                node.IsWordEnd = true;
                node.DocIds.Add(docId);
            }
            else
            {
                node.Equal = InsertNode(node.Equal, suffixWord, docId);
            }
        }
        else
        {
            // full match
            var remainder = word.Slice(common);
            if (remainder.IsEmpty)
            {
                node.IsWordEnd = true;
                if (!node.DocIds.Contains(docId)) node.DocIds.Add(docId);
            }
            else
            {
                node.Equal = InsertNode(node.Equal, remainder, docId);
            }
        }
        return node;
    }

    private int CommonPrefixLength(ReadOnlySpan<char> s1, ReadOnlySpan<char> s2)
    {
        int max = Math.Min(s1.Length, s2.Length);
        int i = 0;
        while (i < max && s1[i] == s2[i]) i++;
        return i;
    }

    public bool Search(string term)
    {
        var query = Normalize(term);
        var node = FindNode(root, query);
        return node != null && node.IsWordEnd;
    }

    private Node FindNode(Node node, ReadOnlyMemory<char> query)
    {
        if (node == null || query.IsEmpty) return null;
        int common = CommonPrefixLength(node.Segment.Span, query.Span);
        if (common == 0)
        {
            if (query.Span[0] < node.Segment.Span[0])
                return FindNode(node.Left, query);
            else
                return FindNode(node.Right, query);
        }
        if (common < node.Segment.Length) return null;
        var rem = query.Slice(common);
        return rem.IsEmpty ? node : FindNode(node.Equal, rem);
    }

    public List<string> PrefixSearchDocuments(string prefix)
    {
        var docs = new HashSet<int>();
        CollectDocs(root, Normalize(prefix), docs);
        var titles = new List<string>();
        foreach (var id in docs) titles.Add(idToTitle[id]);
        return titles;
    }

    private void CollectDocs(Node node, ReadOnlyMemory<char> prefix, HashSet<int> docs)
    {
        if (node == null) return;
        int common = CommonPrefixLength(node.Segment.Span, prefix.Span);
        if (common == 0)
        {
            if (prefix.Span[0] < node.Segment.Span[0]) CollectDocs(node.Left, prefix, docs);
            else CollectDocs(node.Right, prefix, docs);
        }
        else if (common < prefix.Length && common == node.Segment.Length)
        {
            CollectDocs(node.Equal, prefix.Slice(common), docs);
        }
        else if (common == prefix.Length)
        {
            // collect subtree
            GatherDocs(node, docs);
        }
    }

    private void GatherDocs(Node node, HashSet<int> docs)
    {
        if (node == null) return;
        if (node.IsWordEnd) foreach (var id in node.DocIds) docs.Add(id);
        GatherDocs(node.Left, docs);
        GatherDocs(node.Equal, docs);
        GatherDocs(node.Right, docs);
    }

    public List<(string word, List<string> documents)> PrefixSearch(string prefix)
    {
        var results = new List<(string, List<string>)>();
        BuildWords(root, Normalize(prefix), string.Empty, results);
        return results;
    }

    private void BuildWords(Node node, ReadOnlyMemory<char> prefix, string acc, List<(string, List<string>)> outp)
    {
        if (node == null) return;
        BuildWords(node.Left, prefix, acc, outp);
        int common = CommonPrefixLength(node.Segment.Span, prefix.Span);
        if (common == node.Segment.Length)
        {
            var newAcc = acc + node.Segment.ToString();
            if (prefix.Length <= common)
            {
                if (node.IsWordEnd) outp.Add((newAcc, MapDocs(node.DocIds)));
                CollectSubtree(node.Equal, newAcc, outp);
            }
            else
            {
                BuildWords(node.Equal, prefix.Slice(common), newAcc, outp);
            }
        }
        BuildWords(node.Right, prefix, acc, outp);
    }

    private void CollectSubtree(Node node, string acc, List<(string, List<string>)> outp)
    {
        if (node == null) return;
        CollectSubtree(node.Left, acc, outp);
        var newAcc = acc + node.Segment.ToString();
        if (node.IsWordEnd) outp.Add((newAcc, MapDocs(node.DocIds)));
        CollectSubtree(node.Equal, newAcc, outp);
        CollectSubtree(node.Right, acc, outp);
    }

    private List<string> MapDocs(List<int> ids)
    {
        var list = new List<string>(ids.Count);
        foreach (var id in ids) list.Add(idToTitle[id]);
        return list;
    }

    private ReadOnlyMemory<char> Normalize(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return ReadOnlyMemory<char>.Empty;
        raw = raw.Trim().ToLowerInvariant();
        return raw.ToCharArray().AsMemory();
    }

    // Demo printer
    public void PrintDemo(string term, string prefix)
    {
        Console.WriteLine($"Search(\"{term}\") → {Search(term)}");
        var docsByPrefix = PrefixSearchDocuments(prefix);
        Console.WriteLine($"\nDocuments containing words starting with \"{prefix}\":");
        foreach (var t in docsByPrefix) Console.WriteLine($"  • {t}");
        var detailed = PrefixSearch(prefix);
        Console.WriteLine($"\nAll words beginning with \"{prefix}\" and their documents:");
        foreach (var (w, titles) in detailed)
        {
            Console.WriteLine($"  {w}:");
            foreach (var t in titles) Console.WriteLine($"    – {t}");
        }
    }
}
