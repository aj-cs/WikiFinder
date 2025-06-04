using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using SearchEngine.Analysis;
using SearchEngine.Core.Interfaces;
using System.Linq;

namespace SearchEngine.Core;

public class CompactTrieIndex : IExactPrefixIndex
{
    private class TrieNode
    {
        public int PoolIndex, Offset, Length;
        public TrieNode[] ArrayChildren = new TrieNode[26];
        public Dictionary<char, TrieNode> DictChildren = new(2);
        public List<int> DocIds = new();
        public bool IsEndOfWord;

        public TrieNode() { }
        public TrieNode(int poolIndex, int offset, int length)
        {
            PoolIndex = poolIndex;
            Offset = offset;
            Length = length;
        }
    }

    private readonly TrieNode root = new();
    private readonly List<string> wordPool = new();
    private readonly Dictionary<string, int> wordToPoolIndex = new();
    
    // locks for thread safety
    private readonly ReaderWriterLockSlim _trielock = new ReaderWriterLockSlim();
    private readonly object _poolLock = new object();

    public CompactTrieIndex() { }

    private int GetPoolIndex(string word)
    {
        lock (_poolLock)
        {
        if (!wordToPoolIndex.TryGetValue(word, out int idx))
        {
            idx = wordPool.Count;
            wordPool.Add(word);
            wordToPoolIndex[word] = idx;
        }
        return idx;
        }
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
        _trielock.EnterReadLock();
        try
    {
        var node = FindNode(root, query.ToLowerInvariant());
        return node != null && node.IsEndOfWord;
        }
        finally
        {
            _trielock.ExitReadLock();
        }
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

    public List<int> PrefixSearchDocuments(string prefix)
    {
        _trielock.EnterReadLock();
        try
        {
            prefix = prefix.ToLowerInvariant();
        var docs = new HashSet<int>();
        var node = FindNode(root, prefix);
        if (node != null)
        {
            CollectDocs(node, docs);
        }
        return new List<int>(docs);
        }
        finally
        {
            _trielock.ExitReadLock();
        }
    }

    private void CollectDocs(TrieNode node, HashSet<int> docs)
    {
        if (node.IsEndOfWord)
            foreach (var id in node.DocIds)
            {
                docs.Add(id);
            }
        foreach (var child in node.ArrayChildren)
        {
            if (child != null) CollectDocs(child, docs);
        }
        foreach (var kv in node.DictChildren)
        {
            CollectDocs(kv.Value, docs);
        }
    }

    public List<(string word, List<int> docIds)> PrefixSearch(string prefix)
    {
        _trielock.EnterReadLock();
        try
        {
        prefix = prefix.ToLowerInvariant(); 
            var node = FindNode(root, prefix);
            if (node == null) return new List<(string, List<int>)>();
        
            var builder = new List<(string, List<int>)>();
            var currentWord = prefix.Substring(0, prefix.Length - (node.Length - CommonPrefix(node, 0, 0, 0)));
            CollectWords(node, currentWord, builder);
            return builder;
        }
        finally
        {
            _trielock.ExitReadLock();
        }
    }

    public void AddDocument(int docId, IEnumerable<Token> tokens)
    {
        // process each token
        var uniqueTerms = tokens
            .Where(t => !string.IsNullOrEmpty(t.Term))
            .Select(t => t.Term)
            .Distinct()
            .ToList();

        // process each term in sequence with write lock
        _trielock.EnterWriteLock();
        try
        {
            foreach (var term in uniqueTerms)
            {
                int poolIdx = GetPoolIndex(term);
                Insert(root, poolIdx, 0, term.Length, docId);
            }
        }
        finally
        {
            _trielock.ExitWriteLock();
        }
    }

    // add batch document processing
    public void AddDocumentsBatch(IEnumerable<(int docId, IEnumerable<Token> tokens)> documents)
    {
        // Stage 1: Parallel processing of documents to extract unique terms for each document.
        var docTermLists = new ConcurrentBag<(int docId, List<string> terms)>();
        Parallel.ForEach(documents, doc =>
        {
            var lowerCaseUniqueTerms = doc.tokens
                .Where(t => !string.IsNullOrEmpty(t.Term))
                .Select(t => t.Term.ToLowerInvariant()) // Ensure consistent casing early
                .Distinct()
                    .ToList();
            if (lowerCaseUniqueTerms.Any())
            {
                docTermLists.Add((doc.docId, lowerCaseUniqueTerms));
    }
        });

        if (!docTermLists.Any()) return;

        // Stage 2: Collect all unique terms across all documents and ensure they are in the word pool.
        // This populates termToPoolIndexMap. GetPoolIndex handles its own locking.
        var termToPoolIndexMap = new Dictionary<string, int>();
        var allUniqueTerms = new HashSet<string>();

        foreach (var docEntry in docTermLists)
        {
            foreach (var term in docEntry.terms)
        {
                allUniqueTerms.Add(term);
            }
        }
        
        foreach (var term in allUniqueTerms)
        {
            termToPoolIndexMap[term] = GetPoolIndex(term); // GetPoolIndex is internally locked
        }

        // Stage 3: Prepare (poolIndex, termLength, docId) for trie insertion.
        var trieInsertData = new List<(int poolIndex, int termLength, int docId)>();
        foreach (var docEntry in docTermLists)
        {
            var docId = docEntry.docId;
            foreach (var term in docEntry.terms) // term here is already lowercased
        {
                if (termToPoolIndexMap.TryGetValue(term, out int poolIndex))
            {
                    trieInsertData.Add((poolIndex, term.Length, docId));
            }
        }
    }

        // Stage 4: Insert all data into the trie under a single write lock.
        if (trieInsertData.Any())
    {
            _trielock.EnterWriteLock();
            try
            {
                foreach (var (poolIndex, termLength, docId) in trieInsertData)
        {
                    // The Insert method uses wordPool[poolIndex][offset + i] to get characters.
                    // The offset for Insert is 0 as we are inserting the whole word from the pool.
                    Insert(root, poolIndex, 0, termLength, docId);
            }
        }
            finally
        {
                _trielock.ExitWriteLock();
        }
    }
    }

    public void RemoveDocument(int docId, IEnumerable<Token> tokens)
    {
        _trielock.EnterWriteLock();
        try
        {
            foreach (var token in tokens)
            {
                if (string.IsNullOrEmpty(token.Term)) continue;
                
                // check if the word exists in the pool
                if (wordToPoolIndex.TryGetValue(token.Term, out int poolIdx))
                {
                    RemoveWord(root, poolIdx, 0, token.Term.Length, docId);
                }
            }
        }
        finally
        {
            _trielock.ExitWriteLock();
        }
    }

    private bool RemoveWord(TrieNode node, int poolIndex, int offset, int length, int docId)
    {
        if (length == 0)
        {
            node.DocIds.Remove(docId);
            if (node.DocIds.Count == 0)
            {
                node.IsEndOfWord = false;
            }
        }
        else
        {
            char c = wordPool[poolIndex][offset];
            TrieNode child = null;
            int ci = c - 'a';
            if (ci >= 0 && ci < 26)
            {
                child = node.ArrayChildren[ci];
            }
            else
            {
                node.DictChildren.TryGetValue(c, out child);
            }
            if (child == null)
            {
                return false;
            }

            bool prune = RemoveWord(child, poolIndex, offset + 1, length - 1, docId);
            if (prune)
            {
                if (ci >= 0 && ci < 26)
                {
                    node.ArrayChildren[ci] = null;
                }
                else
                {
                    node.DictChildren.Remove(c);
                }
            }
        }
        bool hasNoDocs = !node.IsEndOfWord;
        bool hasNoChildren = true;
        foreach (var child in node.ArrayChildren)
        {
            if (child != null)
            {
                hasNoChildren = false;
                break;
            }
        }

        if (hasNoChildren && node.DictChildren.Count > 0)
        {
            hasNoChildren = false;
        }

        return hasNoDocs && hasNoChildren;
    }

    public void Clear()
    {
        _trielock.EnterWriteLock();
        try
        {
            // clear all children of root
            Array.Clear(root.ArrayChildren, 0, root.ArrayChildren.Length);
            root.DictChildren.Clear();
            root.DocIds.Clear();
            root.IsEndOfWord = false;
            
            // clear the word pool
            wordPool.Clear();
            wordToPoolIndex.Clear();
        }
        finally
        {
            _trielock.ExitWriteLock();
        }
    }

    public List<(int docId, int count)> ExactSearchDocuments(string term)
    {
        _trielock.EnterReadLock();
        try
        {
            term = term.ToLowerInvariant();
            var node = FindNode(root, term);
            if (node == null || !node.IsEndOfWord) 
                return new List<(int docId, int count)>();
                
            // convert to required format with count always set to 1
            return node.DocIds.Select(id => (id, 1)).ToList();
        }
        finally
        {
            _trielock.ExitReadLock();
        }
    }

    public List<(int docId, int count)> BooleanSearchNaive(string expr)
    {
        var terms = expr.Split(new[] { "&&", "||" }, StringSplitOptions.RemoveEmptyEntries)
                       .Select(t => t.Trim())
                       .Where(t => !string.IsNullOrWhiteSpace(t))
                       .ToList();

        var operators = new List<string>();
        int i = 0;
        while (i < expr.Length - 1)
        {
            if (i + 2 <= expr.Length && expr.Substring(i, 2) == "&&")
            {
                operators.Add("&&");
                i += 2;
            }
            else if (i + 2 <= expr.Length && expr.Substring(i, 2) == "||")
            {
                operators.Add("||");
                i += 2;
            }
            else
            {
                i++;
            }
        }

        var docSets = new List<HashSet<int>>();
        foreach (var term in terms)
        {
            var node = FindNode(root, term.ToLowerInvariant());
            if (node != null && node.IsEndOfWord)
            {
                docSets.Add(new HashSet<int>(node.DocIds));
            }
            else
            {
                docSets.Add(new HashSet<int>());
            }
        }

        var result = docSets[0];
        for (int j = 0; j < operators.Count; j++)
        {
            if (operators[j] == "&&")
            {
                result.IntersectWith(docSets[j + 1]);
            }
            else // "||"
            {
                result.UnionWith(docSets[j + 1]);
            }
        }

        return result.Select(id => (id, 1)).ToList();
    }

    private void CollectWords(TrieNode node, string prefix, List<(string, List<int>)> output)
    {
        if (node.IsEndOfWord)
        {
            output.Add((prefix, new List<int>(node.DocIds)));
        }
        foreach (var c in node.ArrayChildren)
        {
            if (c != null)
            {
                var w = wordPool[c.PoolIndex].Substring(c.Offset, c.Length);
                CollectWords(c, prefix + w, output);
            }
        }
        foreach (var kv in node.DictChildren)
        {
            CollectWords(kv.Value, prefix + kv.Key, output);
        }
    }

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
