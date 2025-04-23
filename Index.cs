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
        public ReadOnlyMemory<char> Segment;  // pooled slice of characters
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
    private char[] buffer;  // shared buffer for normalization

    public Index(string filename)
    {
        titleToId = new Dictionary<string, int>();
        idToTitle = new List<string>();
        buffer = ArrayPool<char>.Shared.Rent(1024);
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
                    var span = raw.AsMemory();
                    var trimmed = Normalize(span);
                    if (!trimmed.IsEmpty)
                        Insert(trimmed, titleToId[currentTitle]);
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

    private void Insert(ReadOnlyMemory<char> word, int docId)
    {
        root = InsertNode(root, word, docId);
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
            // compare first char
            if (word.Span[0] < node.Segment.Span[0])
                node.Left = InsertNode(node.Left, word, docId);
            else
                node.Right = InsertNode(node.Right, word, docId);
        }
        else if (common < node.Segment.Length)
        {
            // split node
            var nodeSuffix = node.Segment.Slice(common);
            var child = new Node(nodeSuffix)
            {
                Left = node.Equal,
                Equal = node.Equal,
                IsWordEnd = node.IsWordEnd,
                DocIds = node.DocIds
            };
            // reset current node
            node.Segment = node.Segment.Slice(0, common);
            node.IsWordEnd = false;
            node.DocIds = new List<int>();
            node.Equal = child;

            var wordSuffix = word.Slice(common);
            if (wordSuffix.IsEmpty)
            {
                node.IsWordEnd = true;
                node.DocIds.Add(docId);
            }
            else
            {
                node.Equal = InsertNode(node.Equal, wordSuffix, docId);
            }
        }
        else
        {
            // full match of segment
            var rem = word.Slice(common);
            if (rem.IsEmpty)
            {
                node.IsWordEnd = true;
                if (!node.DocIds.Contains(docId))
                    node.DocIds.Add(docId);
            }
            else
            {
                node.Equal = InsertNode(node.Equal, rem, docId);
            }
        }
        return node;
    }

    private int CommonPrefixLength(ReadOnlySpan<char> s1, ReadOnlySpan<char> s2)
    {
        int len = Math.Min(s1.Length, s2.Length);
        int i = 0;
        while (i < len && s1[i] == s2[i]) i++;
        return i;
    }

    public bool Search(string query)
    {
        var word = Normalize(query.AsMemory());
        var node = FindNode(root, word);
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
        if (rem.IsEmpty) return node;
        return FindNode(node.Equal, rem);
    }

    public List<string> PrefixSearchDocuments(string prefix)
    {
        var docs = new HashSet<int>();
        CollectDocsWithPrefix(root, Normalize(prefix.AsMemory()), docs);
        var result = new List<string>();
        foreach (var id in docs)
            result.Add(idToTitle[id]);
        return result;
    }

    private void CollectDocsWithPrefix(Node node, ReadOnlyMemory<char> prefix, HashSet<int> docs)
    {
        if (node == null) return;
        int common = CommonPrefixLength(node.Segment.Span, prefix.Span);
        if (common == 0)
        {
            if (prefix.Span[0] < node.Segment.Span[0])
                CollectDocsWithPrefix(node.Left, prefix, docs);
            else
                CollectDocsWithPrefix(node.Right, prefix, docs);
        }
        else if (common < prefix.Length && common == node.Segment.Length)
        {
            CollectDocsWithPrefix(node.Equal, prefix.Slice(common), docs);
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
            foreach (var id in node.DocIds)
                docs.Add(id);
        CollectAllDocs(node.Left, docs);
        CollectAllDocs(node.Equal, docs);
        CollectAllDocs(node.Right, docs);
    }

    public List<(string word, List<string> documents)> PrefixSearch(string prefix)
    {
        var results = new List<(string, List<string>)>();
        CollectWordsWithPrefix(root, Normalize(prefix.AsMemory()), "", results);
        return results;
    }

    private void CollectWordsWithPrefix(Node node, ReadOnlyMemory<char> prefix, string acc, List<(string, List<string>)> outp)
    {
        if (node == null) return;
        CollectWordsWithPrefix(node.Left, prefix, acc, outp);
        int common = CommonPrefixLength(node.Segment.Span, prefix.Span);
        if (common == node.Segment.Length)
        {
            var newAcc = acc + node.Segment.ToString();
            if (prefix.Length <= common)
            {
                if (node.IsWordEnd)
                    outp.Add((newAcc, MapDocs(node.DocIds)));
                CollectSubtreeWords(node.Equal, newAcc, outp);
            }
            else
            {
                CollectWordsWithPrefix(node.Equal, prefix.Slice(common), newAcc, outp);
            }
        }
        CollectWordsWithPrefix(node.Right, prefix, acc, outp);
    }

    private void CollectSubtreeWords(Node node, string acc, List<(string, List<string>)> outp)
    {
        if (node == null) return;
        CollectSubtreeWords(node.Left, acc, outp);
        var newAcc = acc + node.Segment.ToString();
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

    private ReadOnlyMemory<char> Normalize(ReadOnlyMemory<char> raw)
    {
        int start = 0, end = raw.Length;
        var span = raw.Span;
        while (start < end && char.IsWhiteSpace(span[start])) start++;
        while (end > start && char.IsWhiteSpace(span[end - 1])) end--;
        var slice = raw.Slice(start, end - start);
        int len = slice.Length;
        if (buffer.Length < len)
        {
            ArrayPool<char>.Shared.Return(buffer);
            buffer = ArrayPool<char>.Shared.Rent(len);
        }
        for (int i = 0; i < len; i++)
            buffer[i] = char.ToLowerInvariant(slice.Span[i]);
        return new ReadOnlyMemory<char>(buffer, 0, len);
    }// New: a built-in demo output method
     // Demo printer
    public void PrintDemo(string term, string prefix)
    {
        Console.WriteLine($"Search(\"{term}\") → {Search(term)}");

        var docsByPrefix = PrefixSearchDocuments(prefix);
        Console.WriteLine($"\nDocuments containing words starting with \"{prefix}\":");
        foreach (var title in docsByPrefix)
            Console.WriteLine($"  • {title}");

        var detailed = PrefixSearch(prefix);
        Console.WriteLine($"\nAll words beginning with \"{prefix}\" and their documents:");
        foreach (var (word, titles) in detailed)
        {
            Console.WriteLine($"  {word}:");
            foreach (var t in titles)
                Console.WriteLine($"    – {t}");
        }
    }
}
