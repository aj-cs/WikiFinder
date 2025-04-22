using System;
using System.Collections.Generic;
using System.IO;

namespace SearchEngineProject;
//RENAME RADIX TO COMPACT TRIE CUZ RADIX TRIE IS BAD
public class Index
{
    private class TrieNode
    {
        public int PoolIndex;
        public int Offset;
        public int Length;
        public TrieNode[] ArrayChildren;
        public Dictionary<char, TrieNode> DictChildren;
        public List<int> DocIds;
        public bool IsEndOfWord;

        public TrieNode()
        {
            ArrayChildren = new TrieNode[26];
            DictChildren = new Dictionary<char, TrieNode>(2);
            DocIds = new List<int>();
            IsEndOfWord = false;
        }

        public TrieNode(int poolIndex, int offset, int length) : this()
        {
            PoolIndex = poolIndex;
            Offset = offset;
            Length = length;
        }
    }

    private TrieNode root = new TrieNode();
    private List<string> wordPool = new List<string>();
    private Dictionary<string, int> wordToPoolIndex = new Dictionary<string, int>();
    private Dictionary<string, int> titleToId = new Dictionary<string, int>();
    private List<string> idToTitle = new List<string>();

    public Index(string filename)
    {
        try
        {
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
        catch (FileNotFoundException)
        {
            throw;
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
        int poolIdx = GetPoolIndex(word);
        int titleId = titleToId[title];
        Insert(root, poolIdx, 0, word.Length, titleId);
    }

    private int GetPoolIndex(string word)
    {
        if (!wordToPoolIndex.TryGetValue(word, out int idx))
        {
            idx = wordPool.Count;
            wordPool.Add(word);
            wordToPoolIndex[word] = idx;
        }
        return idx;
    }

    private void Insert(TrieNode node, int poolIdx, int offset, int length, int titleId)
    {
        if (length == 0)
        {
            MarkNode(node, titleId);
            return;
        }
        char c = wordPool[poolIdx][offset];
        TrieNode child = null;
        int ci = c - 'a';
        if (ci >= 0 && ci < 26)
            child = node.ArrayChildren[ci];
        else
            node.DictChildren.TryGetValue(c, out child);

        if (child == null)
        {
            var newNode = new TrieNode(poolIdx, offset, length);
            newNode.IsEndOfWord = true;
            newNode.DocIds.Add(titleId);
            if (ci >= 0 && ci < 26)
                node.ArrayChildren[ci] = newNode;
            else
                node.DictChildren[c] = newNode;
            return;
        }

        int common = CommonPrefix(child, poolIdx, offset, length);
        if (common < child.Length)
        {
            // split child
            var splitNode = new TrieNode(child.PoolIndex,
                    child.Offset + common,
                    child.Length - common)
            {
                ArrayChildren = child.ArrayChildren,
                DictChildren = child.DictChildren,
                IsEndOfWord = child.IsEndOfWord,
                DocIds = child.DocIds
            };
            child.Length = common;
            child.IsEndOfWord = false;
            child.DocIds = new List<int>();
            child.ArrayChildren = new TrieNode[26];
            child.DictChildren = new Dictionary<char, TrieNode>(2);
            char splitChar = wordPool[splitNode.PoolIndex][splitNode.Offset];
            int sci = splitChar - 'a';
            if (sci >= 0 && sci < 26)
                child.ArrayChildren[sci] = splitNode;
            else
                child.DictChildren[splitChar] = splitNode;

            if (common == length)
            {
                MarkNode(child, titleId);
            }
            else
            {
                var newChild = new TrieNode(poolIdx, offset + common, length - common);
                newChild.IsEndOfWord = true;
                newChild.DocIds.Add(titleId);
                char nc = wordPool[newChild.PoolIndex][newChild.Offset];
                int nci = nc - 'a';
                if (nci >= 0 && nci < 26)
                    child.ArrayChildren[nci] = newChild;
                else
                    child.DictChildren[nc] = newChild;
            }
            return;
        }

        Insert(child, poolIdx, offset + common, length - common, titleId);
    }

    private void MarkNode(TrieNode node, int titleId)
    {
        node.IsEndOfWord = true;
        if (!node.DocIds.Contains(titleId))
            node.DocIds.Add(titleId);
    }

    private int CommonPrefix(TrieNode child, int poolIdx, int offset, int length)
    {
        int max = Math.Min(child.Length, length);
        var pool = wordPool;
        for (int i = 0; i < max; i++)
        {
            if (pool[child.PoolIndex][child.Offset + i] != pool[poolIdx][offset + i])
                return i;
        }
        return max;
    }

    public bool Search(string query)
    {
        var node = FindNode(root, query.ToLowerInvariant());
        return node != null && node.IsEndOfWord;
    }

    private TrieNode FindNode(TrieNode node, string query)
    {
        int idx = 0;
        while (node != null && idx < query.Length)
        {
            char c = query[idx];
            TrieNode child = null;
            int ci = c - 'a';
            if (ci >= 0 && ci < 26)
                child = node.ArrayChildren[ci];
            else
                node.DictChildren.TryGetValue(c, out child);

            if (child == null) return null;
            int match = 0;
            while (match < child.Length && idx + match < query.Length &&
                    wordPool[child.PoolIndex][child.Offset + match] == query[idx + match])
                match++;
            if (match < child.Length) return null;
            idx += match;
            node = child;
        }
        return node;
    }

    public List<string> PrefixSearchDocuments(string prefix)
    {
        var docs = new HashSet<int>();
        var node = FindNode(root, prefix.ToLowerInvariant().TrimEnd());
        if (node != null)
            CollectDocs(node, docs);
        var results = new List<string>();
        foreach (var id in docs)
            results.Add(idToTitle[id]);
        return results;
    }

    private void CollectDocs(TrieNode node, HashSet<int> docs)
    {
        if (node.IsEndOfWord)
            foreach (var id in node.DocIds)
                docs.Add(id);
        foreach (var child in node.ArrayChildren)
            if (child != null) CollectDocs(child, docs);
        foreach (var kv in node.DictChildren)
            CollectDocs(kv.Value, docs);
    }

    public List<(string word, List<string> documents)> PrefixSearch(string prefix)
    {
        var results = new List<(string, List<string>)>();
        var startNode = FindNode(root, prefix.ToLowerInvariant());
        if (startNode != null)
            CollectWords(startNode, prefix, results);
        return results;
    }

    private void CollectWords(TrieNode node, string current, List<(string, List<string>)> output)
    {
        if (node.IsEndOfWord)
        {
            var docs = new List<string>();
            foreach (var id in node.DocIds)
                docs.Add(idToTitle[id]);
            output.Add((current, docs));
        }
        foreach (var child in node.ArrayChildren)
            if (child != null)
                CollectWords(child, current + wordPool[child.PoolIndex].Substring(child.Offset, child.Length), output);
        foreach (var kv in node.DictChildren)
            CollectWords(kv.Value, current + kv.Key, output);
    }

    private string Normalize(string raw)
    {
        return raw.Trim().ToLowerInvariant();
    }
}
