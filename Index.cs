using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq; 
using System.Net.Http; 
using System.Text.Json; 
using System.Buffers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
namespace SearchEngineProject;
public class Index
{
    private record Token(string Raw, string Norm, int DocId, int TermPos, int CharStart, int CharEnd);

    private static readonly HashSet<string> StopWords =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "a", "an", "the", "and", "or", "but", "is", "are", "was", "were",
            "to", "in", "on", "at", "for", "with", "of", "by", "as", "that",
            // add more stop words
        };
    private static readonly Porter2Stemmer.EnglishPorter2Stemmer Stemmer
        = new();
    //compact trie root node
    private TrieNode root;
    // bloom filter 
    private readonly BloomFilter _bloomFilter;
    private class BloomFilter
    {
        public readonly BitArray bits;
        public readonly int m, k, n;

        public BloomFilter(int expectedItems, double falsePositiveRate)
        {
            if (expectedItems <= 0 ) throw new ArgumentException(nameof(expectedItems)); // only positive numbers for expected
            if (falsePositiveRate <= 0 || falsePositiveRate >= 1) throw new ArgumentOutOfRangeException(nameof(falsePositiveRate)); // FP rate can only go between 0 and 100 percent.
            n = expectedItems;
            m = (int)Math.Ceiling(-n * Math.Log(falsePositiveRate) / (Math.Log(2) * Math.Log(2)));
            k = (int)Math.Round((m / (double)n) * Math.Log(2));
            bits = new BitArray(m);
        }
        
    }
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
        // MurmurHash3 ported from Austin Appleby's C++ implementation
        // https://github.com/aappleby/smhasher/blob/master/src/MurmurHash3.cpp
    private static uint MurmurHash3(byte[] data, uint seed)
    {
        const uint c1 = 0xcc9e2d51, c2 = 0x1b873593;
        uint h1 = seed;
        int len = data.Length, roundedEnd = len & ~3;
        for (int i = 0; i < roundedEnd; i += 4)
        {
            uint k1 = (uint)(data[i] | data[i+1]<<8 | data[i+2]<<16 | data[i+3]<<24);
            k1 = Rotl32(k1 * c1, 15) * c2;
            h1 = Rotl32(h1 ^ k1, 13) * 5 + 0xe6546b64;
        }
        uint k2 = 0;
        switch (len & 3)
        {
            case 3: k2 ^= (uint)data[roundedEnd+2] << 16; goto case 2;
            case 2: k2 ^= (uint)data[roundedEnd+1] << 8; goto case 1;
            case 1:
                k2 ^= data[roundedEnd];
                k2 = Rotl32(k2 * c1, 15) * c2;
                h1 ^= k2;
                break;
        }
        h1 ^= (uint)len;
        return FMix(h1);
    }

    private static uint Rotl32(uint x, byte r) => (x << r) | (x >> (32 - r));
    private static uint FMix(uint h)
    {
        h ^= h >> 16; h *= 0x85ebca6b;
        h ^= h >> 13; h *= 0xc2b2ae35;
        h ^= h >> 16;
        return h;
    }
    
    // flag to enable/disable delta encoding
    private bool deltaEncodingEnabled = true;
    public void SetDeltaEncoding(bool enabled)
    {
        deltaEncodingEnabled = enabled;
    }
    private Dictionary<string, BitArray> bitIndex;
    private bool bitIndexBuilt = false;

    private class Posting
    {
        public int DocumentId;
        public int Count; 
        public List<int> Positions;

        public Posting(int docId, int firstPos)
        {
            DocumentId = docId;
            Count = 1;
            Positions = new List<int> { firstPos }; 
        }
    }
    // Inverted index
    private Dictionary<string, List<Posting>> invertedIndex;
    // variables to keep track of and compress documents 
    private Dictionary<int, string> documentIdMap; // maps document IDs to names
    private List<string> documentTitles;           // list of document titles (index corresponds to document ID)
    private int nextDocumentId;                    // assigns IDs to Documents
    private int documentPositionCounter;           // position counter for each document
    
    private class DocumentLog
    {
        public int DocumentId;
        public List<int> Positions;
        public DocumentLog Next;
        public int Count;
        public DocumentLog(int documentId, int position, DocumentLog next)
        {
            DocumentId = documentId;
            Positions = new List<int> { position };
            Next = next;
            Count = 1;
        }
    }

    private void BuildBitIndex()
    {
        if (bitIndexBuilt)
            return;
        bitIndexBuilt = true;
        int docCount = nextDocumentId;
        bitIndex = new Dictionary<string, BitArray>();

        foreach (var kv in invertedIndex)
        {
            var bits = new BitArray(docCount);
            foreach (var post in kv.Value)
                bits.Set(post.DocumentId, true);
            bitIndex[kv.Key] = bits;
        }
    }
    
    public Index(string filename)
    {
        root = new TrieNode("");
        invertedIndex = new Dictionary<string,List<Posting>>();
        _bloomFilter = new BloomFilter(100000, 0.1); 
        documentIdMap = new Dictionary<int, string>();
        documentTitles = new List<string>();
        nextDocumentId = 0;
        documentPositionCounter = 0; 
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
                        documentPositionCounter = 0; // reset position counter for next document
                        continue;
                    }
                    if (!titleRead)
                    {
                        currentTitle = line;
                        titleRead = true;
                    }
                    else
                    {
                        ProcessLine(line, currentTitle);
                    }
                }
            }
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine("Error reading file " + filename);
        }
    }
    

    private static readonly Regex TokenRegex =
        // letters + digits with optional internal ’ ’ - characters
        new(@"\p{L}[\p{L}\p{Nd}’'-]*", RegexOptions.Compiled);

    private void ProcessLine(string line, string currentTitle)
    {
        if (string.IsNullOrWhiteSpace(line)) return;

        int docId = GetOrCreateDocId(currentTitle);

        foreach (Match m in TokenRegex.Matches(line.Normalize(NormalizationForm.FormC)))
        {
            string raw  = m.Value;
            string norm = Stemmer.Stem(raw.ToLowerInvariant()).Value;

            if (StopWords.Contains(norm) || norm.Length == 0)
                continue;                              // drop stop-words

            int termPos   = documentPositionCounter++;
            var token     = new Token(raw, norm, docId, termPos, m.Index, m.Index + raw.Length);

            InsertToken(token);                        // one unified entry point
        }
    }
    private int GetOrCreateDocId(string title)
    {
        int id = documentTitles.IndexOf(title);
        if (id != -1) return id;
        id = nextDocumentId++;
        documentTitles.Add(title);
        documentIdMap[id] = title;
        return id;
    }

    private void InsertToken(Token t)
    {
        if (!invertedIndex.TryGetValue(t.Norm, out var postings))
            invertedIndex[t.Norm] = postings = new List<Posting>();

        Posting p = postings.FirstOrDefault(x => x.DocumentId == t.DocId);
        if (p == null)
        {
            p = new Posting(t.DocId, t.TermPos);
            postings.Add(p);
        }
        else
        {
            int last = p.Positions[^1];
            p.Positions.Add(deltaEncodingEnabled ? t.TermPos - last : t.TermPos);
            p.Count++;
        }
        
        InsertBloom(t.Norm);
        // InsertIntoTrie(root, t.Norm, documentTitles[t.DocId], t.TermPos);
    }
    private static string FilterWord(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "";

        var sb = new StringBuilder(input.Length);
        foreach (char c in input)
            if (char.IsLetterOrDigit(c)) sb.Append(char.ToLowerInvariant(c));

        if (sb.Length == 0) return "";

        string stem = Stemmer.Stem(sb.ToString()).Value;
        if (StopWords.Contains(stem)) return "";
        return stem;
    }
    private void InsertIntoInvertedIndex(string word, string title, int position)
    {
        int documentId = documentTitles.IndexOf(title);
        if (documentId == -1)
        {
            documentId = nextDocumentId++;
            documentTitles.Add(title);
            documentIdMap[documentId] = title;
        }
        if (!invertedIndex.TryGetValue(word, out var postings))
        {
            postings = new List<Posting>();
            invertedIndex[word] = postings;
        }
        // find existing posting for this doc
        foreach (var p in postings)
        {
            if (p.DocumentId == documentId)
            {
                p.Count++;
                int last = p.Positions[^1];
                p.Positions.Add(deltaEncodingEnabled ? position - last : position);
                return;
            }
        }

        // first time this term in this document
        postings.Add(new Posting(documentId,
            deltaEncodingEnabled ? position : position
        ));
    }

    // helper method to decode delta-encoded positions
    private List<int> DecodePositions(List<int> encoded)
    {
        if (!deltaEncodingEnabled) return encoded;
        List<int> decoded = new List<int>(encoded.Count);
        int current = 0;
        foreach (int delta in encoded)
        {
            current += delta;
            decoded.Add(current);
        }
        return decoded;
    }
    
    public void InsertBloom(string word)
    {
        BloomFilter bloom = _bloomFilter;
        var data = System.Text.Encoding.UTF8.GetBytes(word);
        uint hash1 = MurmurHash3(data, 0);
        uint hash2 = MurmurHash3(data, 1);
        for (int i = 0; i < bloom.k; i++)
            bloom.bits[(int)((hash1 + (uint)i * hash2) % (uint)bloom.m)] = true;
    }

    public bool MayContain(string word)
    {
        BloomFilter bloom = _bloomFilter;
        var data = System.Text.Encoding.UTF8.GetBytes(word);
        uint hash1 = MurmurHash3(data, 0);
        uint hash2 = MurmurHash3(data, 1);
        for (int i = 0; i < bloom.k; i++)
            if (!bloom.bits[(int)((hash1 + (uint)i * hash2) % (uint)bloom.m)])
                return false;
        return true;
    }

    //recursive insertion into the tree
    // node is the current node and key is the substring that we need to insert
    private void InsertIntoTrie(TrieNode node, string key, string title, int position)
    {
        int documentId = documentTitles.IndexOf(title);
        if (documentId == -1)
        {
            documentId = nextDocumentId++;
            documentTitles.Add(title);
            documentIdMap[documentId] = title;
        }
        
        // when key is empty, mark current node as a word
        if (key.Length == 0)
        {
            node.EndOfWord = true;
            node.Count++;
            DocumentLog existing = FindDocLog(node.Log, documentId);
            if (existing != null)
            {
                if (!existing.Positions.Contains(position))
                {
                    existing.Positions.Add(position);
                    existing.Count++;
                }
            }
            else
            {
                node.Log = new DocumentLog(documentId, position, node.Log);
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
            newNode.Log = new DocumentLog(documentId, position, null);
            node.Children[firstChar] = newNode;
            return;
        }
        // compute the longest common prefix length between the childs segment and the key
        int commonLength = CommonPrefixLength(child.Segment, key);

        if (commonLength < child.Segment.Length)
        {
            // partial match which means we need to split the child
            // create a new node for the remainder of the child's segment
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
            
            //reset children dict and add split node
            child.Children = new Dictionary<char, TrieNode>();
            child.Children[childSuffix[0]] = splitNode;
            // if the key exactly matches the common prefix
            if (commonLength == key.Length)
            {
                child.EndOfWord = true;
                child.Count++;
                DocumentLog existing = FindDocLog(child.Log, documentId);
                if (existing != null)
                {
                    if (deltaEncodingEnabled && existing.Positions.Count > 0)
                    {
                        int lastPos = existing.Positions[^1];
                        existing.Positions.Add(position - lastPos);
                    }
                    else
                    {
                        existing.Positions.Add(position);
                    }
                    existing.Count++;
                }
                else
                {
                    child.Log = new DocumentLog(documentId, position, child.Log);
                }
            }
            else
            {
                // insert the remainder of the key as a new child
                string keySuffix = key.Substring(commonLength);
                TrieNode newChild = new TrieNode(keySuffix);
                newChild.EndOfWord = true;
                newChild.Count = 1;
                newChild.Log = new DocumentLog(documentId, position, null);
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
                //key exactly matches the child's segment
                child.EndOfWord = true;
                child.Count++;
                DocumentLog existing = FindDocLog(child.Log, documentId);
                if (existing != null)
                {
                    if (!existing.Positions.Contains(position))
                    {
                        existing.Positions.Add(position);
                        existing.Count++;
                    }
                }
                else
                {
                    child.Log = new DocumentLog(documentId, position, child.Log);
                }
                return;
            }
            // proceed with insertion of child node
            InsertIntoTrie(child, keyRemainder, title, position);
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

    private DocumentLog FindDocLog(DocumentLog log, int documentId)
    {
        while (log != null)
        {
            if (log.DocumentId == documentId)
                return log;
            log = log.Next;
        }
        return null;
    }

    private bool DocExists(DocumentLog log, int documentId)
    {
        while (log != null)
        {
            if (log.DocumentId == documentId)
                return true;
            log = log.Next;
        }
        return false;
    }

    // find node corresponding to complete word
    private TrieNode FindWord(string word)
    {
        word = FilterWord(word);
        TrieNode current = root;
        while (word.Length > 0)
        {
            char firstChar = word[0];
            if (!current.Children.TryGetValue(firstChar, out TrieNode child))
            {
                return null;
            }

            // the child's segment must match the beginning of the word
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
    
    // NOTE: exact match search

    public List<string> Search(string query)
    {
        var words = query.Split(" ", StringSplitOptions.RemoveEmptyEntries);
        var result = new List<string>();

        if (words.Length > 1)
        {
            if (query.Contains("&&") || query.Contains("||"))
            {
                result = BooleanSearchNaiveIndex(query);
                return result;
            }
            else
            {
                result = PhraseSearchIndex(query);
                return result;
            }
        }
        else
        {
            if (query.EndsWith("*"))
            {
                query = query.Substring(0, query.Length - 1);
                result = PrefixSearchTrie(query);
                return result;
            }
            else if (query.EndsWith("#"))
            {
                query = query.Substring(0, query.Length - 1);
                result = SearchRankedIndex(query).Select(x => x.Item1).ToList();
                return result;
            }
            else
            {
                result = SearchIndex(query);
                return result;
                
            }
        }
    }
    public List<string> SearchTrie(string searchStr)
    {
        var results = new List<string>();
        string word = FilterWord(searchStr);
        TrieNode node = FindWord(word);

        if (node != null)
        {
            // Collect all document IDs from this node’s DocumentLog chain
            for (var log = node.Log; log != null; log = log.Next)
            {
                results.Add(documentTitles[log.DocumentId]);
            }
        }

        return results;
    }


    // helper method to find node corresponding to prefix
    private (TrieNode node, string matched) FindPrefixNode(string prefix)
    {
        prefix = FilterWord(prefix);
        TrieNode current = root;
        string matched = "";
        while (prefix.Length > 0)
        {
            char firstChar = prefix[0];
            if (!current.Children.TryGetValue(firstChar, out TrieNode child))
            {
                return (null, matched);
            }
            int commonLength = CommonPrefixLength(child.Segment, prefix);
            matched += child.Segment.Substring(0, commonLength);
            if (commonLength < child.Segment.Length)
            {
                // if the prefix ends in the middle of the child's segment it's a match
                if (commonLength == prefix.Length)
                    return (child, matched);
                else 
                    return (null, matched);
            }
            // child's segment fully matches so we continue with remainder of prefix
            prefix = prefix.Substring(commonLength);
            current = child;
        }
        return (current, matched);
    }
    
    // NOTE: auto-completion, lists all words starting with the given prefix and their documents
    public List<string> AutoComplete(string prefix)
{
    prefix = FilterWord(prefix);
    var results = new HashSet<string>();
    
    if (prefix.Length == 0) return results.ToList();

    var (node, matched) = FindPrefixNode(prefix);
    if (node == null) return results.ToList();

    var completions = new List<(string word, TrieNode node)>();
    CollectWords(node, matched, completions);

    foreach (var (wordComp, trieNode) in completions)
    {
        for (var log = trieNode.Log; log != null; log = log.Next)
        {
            results.Add(documentTitles[log.DocumentId]);
        }
    }

    return results.ToList();
}

    // NOTE: returns the (unique) documents where any word starting with the given prefix appears
    public List<string> PrefixSearchTrie(string prefix)
    {
        var results = new List<string>();
        prefix = FilterWord(prefix);
        if (prefix.Length == 0) return results;

        var (node, matched) = FindPrefixNode(prefix);
        if (node == null) return results;

        var documentIds = new HashSet<int>();
        var completions = new List<(string word, TrieNode node)>();
        CollectWords(node, matched, completions);
        
        foreach (var (wordComp, trieNode) in completions)
        {
            for (var log = trieNode.Log; log != null; log = log.Next)
            {
                documentIds.Add(log.DocumentId);
            }
        }
        
        foreach (var docId in documentIds)
        {
            results.Add(documentTitles[docId]);
        }
        return results;
    }

    // recursively collects complete words (nodes marked as isEndOfWord) from a given node
    private void CollectWords(TrieNode node, string prefix, List<(string, TrieNode)> result)
    {
        if (node.EndOfWord)
            result.Add((prefix, node));
            
        foreach (var child in node.Children.Values)
        {
            CollectWords(child, prefix + child.Segment, result);
        }
    }

    public List<string> PhraseSearchTrie(string phrase)
{
    var results = new List<string>();
    string[] words = phrase.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    if (words.Length == 0) return results;
    
    if (words.Length == 1)
    {
        return SearchTrie(words[0]);
    }

    // filter each word
    for (int i = 0; i < words.Length; i++)
    {
        words[i] = FilterWord(words[i]);
    }

    TrieNode firstNode = FindWord(words[0]);
    if (firstNode == null) return results;

    // get all doc IDs for the first word
    var candidateDocs = new Dictionary<int, List<int>>();
    for (var log = firstNode.Log; log != null; log = log.Next)
    {
        // make a copy of positions
        candidateDocs[log.DocumentId] = new List<int>(log.Positions);
    }

    // Intersect position lists for subsequent words
    for (int wordIndex = 1; wordIndex < words.Length; wordIndex++)
    {
        TrieNode node = FindWord(words[wordIndex]);
        if (node == null)
        {
            // No match for this word => no phrase match
            return new List<string>();
        }

        var newCandidates = new Dictionary<int, List<int>>();
        foreach (var docEntry in candidateDocs)
        {
            int docId = docEntry.Key;
            List<int> prevPositions = docEntry.Value;

            var currLog = FindDocLog(node.Log, docId);
            if (currLog != null)
            {
                var validPositions = MergePositions(prevPositions, currLog.Positions);
                if (validPositions.Count > 0)
                {
                    newCandidates[docId] = validPositions;
                }
            }
        }

        candidateDocs = newCandidates;
        if (candidateDocs.Count == 0) return new List<string>();
    }

    // convert final doc IDs to titles
    foreach (var docId in candidateDocs.Keys)
    {
        results.Add(documentTitles[docId]);
    }
    return results;
}
    
    //exact match search using inverted index
    public List<string> SearchIndex(string searchStr)
    {
        var results = new List<string>();
        string word = FilterWord(searchStr);
        if (!invertedIndex.TryGetValue(word, out var postings))
        {
            return results;
        }

        //adds all matching doc titles
        foreach (var posting in postings)
        {
            results.Add(documentTitles[posting.DocumentId]);
        }
        return results;
    }

    // prefix search using inverted index
    public List<string> PrefixSearchIndex(string prefix)
    {
        var results = new List<string>();
        prefix = FilterWord(prefix);
        if (prefix.Length == 0) return results;

        var matches = invertedIndex.Keys.Where(k => k.StartsWith(prefix)).ToList();
        if (matches.Count == 0) return results;

        foreach (var word in matches)
        {
            foreach (var posting in invertedIndex[word])
            {
                results.Add(documentTitles[posting.DocumentId]);
            }
        }
        return results;
    }

    // phrase search using inverted index
    public List<string> PhraseSearchIndex(string phrase)
    {
        var results = new List<string>();
        string[] words = phrase.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        if (words.Length == 0) return results;
        if (words.Length == 1)
            return SearchIndex(words[0]);
        for (int i = 0; i < words.Length; i++)
            words[i] = FilterWord(words[i]);
        if (!invertedIndex.ContainsKey(words[0]))
            return results;
        
        var first = invertedIndex[words[0]];
        var candidates = first.ToDictionary(
            entry => entry.DocumentId,
            entry => new List<int>(entry.Positions));
        for (int i = 1; i < words.Length; i++)
        {
            if (!invertedIndex.TryGetValue(words[i], out var postings))
                return new List<string>();
            var newCandidates = new Dictionary<int, List<int>>();
            foreach (var p in postings)
            {
                if (candidates.TryGetValue(p.DocumentId, out var prev))
                {
                    var validPositions = MergePositions(prev, p.Positions);
                    if (validPositions.Count > 0)
                        newCandidates[p.DocumentId] = validPositions;
                }
            }
            candidates = newCandidates;
            if (candidates.Count == 0) return results;
        }

        foreach (var documentId in candidates.Keys)
            results.Add(documentTitles[documentId]);
        return results;
    }

    // ranked search using inverted index
    public List<(string,int)> SearchRankedIndex(string searchStr)
    {
        var results = new List<(string,int)>();
        string word = FilterWord(searchStr);
        if (!invertedIndex.TryGetValue(word, out var postings))
            return new List<(string,int)>();
        
        foreach (var posting in postings.OrderByDescending(p => p.Count))
            results.Add((documentTitles[posting.DocumentId],posting.Count));
        return results;
    }
    // merge positions of two words in a document 
    // assumes positions are sorted, which they are by default for the positions within documentlog
    private List<int> MergePositions(List<int> prevPositions, List<int> currPositions)
    {
        //check if delta encoding is enabled or not ||||||||||||||||||||||
        prevPositions = DecodePositions(prevPositions);
        currPositions = DecodePositions(currPositions);
        List<int> result = new List<int>();
        int i = 0, j = 0;
        while (i < prevPositions.Count && j < currPositions.Count)
        {
            if (currPositions[j] == prevPositions[i] + 1)
            {
                result.Add(currPositions[j]);
                i++;
                j++;
            }
            else if (currPositions[j] < prevPositions[i] + 1)
            {
                j++;
            }
            else {
                i++;
            }
        }
        return result;
    }

    public List<string> RankedSearchTrie(string searchStr)
    {
        var results = new List<string>();
        string word = FilterWord(searchStr);
        TrieNode node = FindWord(word);

        if (node == null)
        {
            return results;
        }

        // Collect doc frequency from DocumentLog
        var docCounts = new Dictionary<int, int>();
        for (var log = node.Log; log != null; log = log.Next)
        {
            docCounts[log.DocumentId] = log.Count;
        }

        // Sort descending by frequency
        var rankedResults = docCounts.OrderByDescending(x => x.Value).ToList();
        foreach (var pair in rankedResults)
        {
            results.Add($"{documentTitles[pair.Key]} (relevance: {pair.Value})");
        }

        return results;
    }
    
//searchStr example "word1 AND word2 OR word3"
    public List<string> BooleanSearchNaiveIndex(string searchStr)
    {
        var results = new List<string>();
        var words = searchStr.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        HashSet<int> resultSet = null;
        string op = null;
        foreach (var w in words)
        {
            if (w == "&&" || w == "||"){op = w; continue;}

            string filterWord = FilterWord(w);
            if (!invertedIndex.TryGetValue(filterWord, out var posting))
            {
                // If AND is in effect, everything fails if one term is missing
                // Return empty
                return new List<string>();
            }

            var docs = new HashSet<int>(posting.Select(p => p.DocumentId));
            if (resultSet == null) resultSet = docs;
            else if (op == "&&")   resultSet.IntersectWith(docs);
            else if (op == "||")   resultSet.UnionWith(docs);
            op = null;
        }

        if (resultSet != null)
            foreach (int docId in resultSet)
                results.Add(documentTitles[docId]);
        return results;
    }

    public List<string> BooleanSearchBitsetIndex(string searchStr)
    {
        var results = new List<string>();
        BuildBitIndex(); // ensure bitIndex is built

        var words = searchStr.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        BitArray resultBits = null;
        string op = null;

        foreach (var w in words)
        {
            if (w == "&&" || w == "||") op = w; continue;

            string word = FilterWord(w);
            bitIndex.TryGetValue(word, out BitArray bits);
            bits ??= new BitArray(nextDocumentId);

            if (resultBits == null) resultBits = (BitArray)bits.Clone();
            else if (op == "&&") resultBits.And(bits);
            else if (op == "||") resultBits.Or(bits);
            op = null;
        }

        if (resultBits != null)
        {
            for (int i = 0; i < nextDocumentId; i++)
            {
                if (resultBits[i])
                {
                    results.Add(documentTitles[i]);
                }
            }
            
        }

        return results;
    }

    public List<string> BooleanSearchNaiveTrie(string searchStr)
    {
        var results = new List<string>();
        var words = searchStr.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        HashSet<int> resultSet = null;
        string op = null;

        foreach (var w in words)
        {
            if (w == "&&" || w == "||")
            {
                op = w;
                continue;
            }

            string filterWord = FilterWord(w);
            TrieNode node = FindWord(filterWord);

            var docs = new HashSet<int>();
            if (node != null)
            {
                for (var log = node.Log; log != null; log = log.Next)
                {
                    docs.Add(log.DocumentId);
                }
            }

            if (resultSet == null)
            {
                resultSet = docs;
            // Build a bit array for matches
            }
            else if (op == "&&")
            {
                resultSet.IntersectWith(docs);
            }
            else if (op == "||")
            {
                resultSet.UnionWith(docs);
            }

            op = null;
        }

        if (resultSet != null)
        {
            foreach (var id in resultSet)
            {
                results.Add(documentTitles[id]);
            }
        }
        return results;
    }

    public List<string> BooleanSearchBitsetTrie(string searchStr)
    {
        var results = new List<string>();
        BuildBitIndex();

        var words = searchStr.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        BitArray resultBits = null;
        string op = null;

        foreach (var w in words)
        {
            if (w == "&&" || w == "||")
            {
                op = w;
                continue;
            }

            string filterWord = FilterWord(w);
            TrieNode node = FindWord(filterWord);
            var bits = new BitArray(nextDocumentId);
            if (node != null)
            {
                for (var log = node.Log; log != null; log = log.Next)
                {
                    bits.Set(log.DocumentId, true);
                }
            }

            if (resultBits == null)
            {
                resultBits = bits;
            }
            else if (op == "&&")
            {
                resultBits.And(bits);
            }
            else if (op == "||")
            {
                resultBits.Or(bits);
            }
            op = null;
        }

        if (resultBits == null) return results;

        var foundDocs = Enumerable.Range(0, nextDocumentId).Where(i => resultBits.Get(i)).ToList();
        foreach (int docId in foundDocs)
        {
            results.Add(documentTitles[docId]);
        }

        return results;
    }

    
    public void AddPublication(string title, string content)
    {
        documentPositionCounter = 0;
        var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None); // split content into lines. Processing in chunks
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;
            ProcessLine(line, title);
        }
    }
    
    // asynchronously fetches a Wikipedia article by title, parses its content,
    // and adds it as a publication to both indexes. TODO:SHOULD RETURN PLAINTEXT AS CONTENT
public async Task AddPublicationFromWikipediaAsync(string title)
{
    string url = $"https://en.wikipedia.org/w/api.php?action=query&prop=revisions&rvprop=content&rvslots=main&formatversion=2&redirects=1&format=json&titles={Uri.EscapeDataString(title)}";    
    using (HttpClient client = new HttpClient())
    {
        try
        {
            string json = await client.GetStringAsync(url);
            using JsonDocument doc = JsonDocument.Parse(json);
            var rootElement = doc.RootElement;

            if (rootElement.TryGetProperty("query", out var queryElement) &&
                queryElement.TryGetProperty("pages", out var pagesElement) &&
                pagesElement.GetArrayLength() > 0)
            {
                var page = pagesElement[0];
                
                if (page.TryGetProperty("pageid", out var pageIdElement) && pageIdElement.GetInt32() == -1)
                {
                    Console.WriteLine($"Page '{title}' not found or invalid.");
                    return;
                }
                if (page.TryGetProperty("revisions", out var revisionsElement) &&
                    revisionsElement.GetArrayLength() > 0)
                {
                    var revision = revisionsElement[0];

                    if (revision.TryGetProperty("slots", out var slotsElement) &&
                        slotsElement.TryGetProperty("main", out var mainSlot) &&
                        mainSlot.TryGetProperty("content", out var contentElement))
                    {
                        string content = contentElement.GetString();

                        // Insert into index
                        AddPublication(title, content);
                    }
                    else
                    {
                        Console.WriteLine($"No content found in main slot for '{title}'.");
                    }
                }
                else
                {
                    Console.WriteLine($"No revisions found for '{title}'.");
                }
            }
            else
            {
                Console.WriteLine("No valid 'query'/'pages' data found in the response.");
            }

            // in case wikipedia is down or sum. 
        }
        catch (Exception e)
        {
            throw new Exception($"Error fetching data from Wikipedia: {e.Message}");
        }
    }
}

public bool RemovePublication(string title)
{
    int docId = documentTitles.IndexOf(title);
    if (docId == -1)
        return false;
    
    foreach (var term in invertedIndex.Keys.ToList())
    {
        var postings = invertedIndex[term];
        postings.RemoveAll(p => p.DocumentId == docId);

        // if no postings remain for the term, drop the term entirely
        if (postings.Count == 0)
            invertedIndex.Remove(term);
    }
    RemovePublicationFromTrie(root, docId);

    // mark title slot as deleted
    documentTitles[docId] = null;
    documentIdMap.Remove(docId);

    return true;
}


// helper method to remove a publication's document logs from the trie recursively.
    private void RemovePublicationFromTrie(TrieNode node, int docId)
    {
        if (node == null)
            return;
        node.Log = RemoveDocLog(node.Log, docId);
        if (node.Log == null)
        {
            node.EndOfWord = false;
            node.Count = 0;
        }

        // eecursively process children.
        foreach (var child in node.Children.Values)
        {
            RemovePublicationFromTrie(child, docId);
        }
    }

// h elper method to remove entries with a given document id from a DocumentLog chain.
    private DocumentLog RemoveDocLog(DocumentLog log, int docId)
    {
        if (log == null)
            return null;
        // remove any leading nodes with the docId.
        while (log != null && log.DocumentId == docId)
        {
            log = log.Next;
        }
        if (log == null)
            return null;
        DocumentLog head = log;
        DocumentLog current = head;
        while (current.Next != null)
        {
            if (current.Next.DocumentId == docId)
                current.Next = current.Next.Next;
            else
                current = current.Next;
        }
        return head;
    }
    
public (int nodeCount, long nodeBytes, long stringBytes, long dictBytes, long docLogBytes, long positionBytes, long totalBytes) GetTrieStats()
{
    return CountTrieNodesRecursive(root);
}

private (int nodeCount, long nodeBytes, long stringBytes, long dictBytes, long docLogBytes, long positionBytes, long totalBytes) CountTrieNodesRecursive(TrieNode node)
{
    if (node == null)
        return (0, 0, 0, 0, 0, 0, 0);

    int count = 1; // count current node
    long nodeBytes = 16 + 4 + 1; // object overhead + count + boolean endofword
    long stringBytes = node.Segment.Length * 2; // string (2 bytes per char)
    long dictBytes = 24; // dictionary overhead
    long docLogBytes = 8; // documentlog reference
    long positionBytes = 0;

    // count memory for documentlog chain
    DocumentLog log = node.Log;
    while (log != null)
    {
        docLogBytes += 16 + 4 + 4 + 8; // object overhead + documentid + count + next reference
        positionBytes += 16 + log.Positions.Count * 4; // list overhead + int positions
        log = log.Next;
    }

    // recursively process children
    long childNodeBytes = 0, childStringBytes = 0, childDictBytes = 0, childDocLogBytes = 0, childPositionBytes = 0;
    foreach (var child in node.Children.Values)
    {
        var (childCount, nodeB, stringB, dictB, docLogB, positionB, _) = CountTrieNodesRecursive(child);
        count += childCount;
        childNodeBytes += nodeB;
        childStringBytes += stringB;
        childDictBytes += dictB;
        childDocLogBytes += docLogB;
        childPositionBytes += positionB;
    }

    return (count, 
            nodeBytes + childNodeBytes,
            stringBytes + childStringBytes, 
            dictBytes + childDictBytes,
            docLogBytes + childDocLogBytes,
            positionBytes + childPositionBytes,
            nodeBytes + stringBytes + dictBytes + docLogBytes + positionBytes +
            childNodeBytes + childStringBytes + childDictBytes + childDocLogBytes + childPositionBytes);
}

public (int wordCount, int entryCount, long dictBytes,
    long stringBytes, long entryBytes, long positionBytes, long totalBytes)
    GetInvertedIndexStats()
{
    int wordCount = invertedIndex.Count;
    int entryCount = 0;

    // overhead of the outer Dictionary itself
    long dictBytes = 24;
    // one List<Posting> per term
    long listOverheadBytes = 0;
    long stringBytes = 0;
    long entryBytes = 0;
    long positionBytes = 0;
    foreach (var kv in invertedIndex)
    {
        string term = kv.Key;
        var postings = kv.Value;
        // account for the term string
        stringBytes += term.Length * 2;
        // account for the postings list object overhead
        // List<T> has ~16 B object header + array reference + count/int fields
        listOverheadBytes += 16;

        // each Posting in the list
        foreach (var p in postings)
        {
            entryCount++;
            // posting object: assuming 16 B header + 2 ints (DocumentId,Count = 8 B)
            entryBytes += 16 + 8;
            // positions list overhead + 4 B per stored int
            positionBytes += 16 + p.Positions.Count * 4;
        }
    }
    dictBytes += listOverheadBytes;
    long totalBytes = dictBytes + stringBytes + entryBytes + positionBytes;
    return (wordCount, entryCount, dictBytes,
        stringBytes, entryBytes, positionBytes, totalBytes);
}

public int getTotalUniqueWordCount()
{
    return invertedIndex.Count;
}
public void CompareDataStructures()
{
    var (trieNodes, trieNodeBytes, trieStringBytes, trieDictBytes, trieDocLogBytes, triePositionBytes, trieTotalBytes) = GetTrieStats();
    var (wordCount, entryCount, invDictBytes, invStringBytes, invEntryBytes, invPositionBytes, invTotalBytes) = GetInvertedIndexStats();

    Console.WriteLine("\ndata structure size comparison:");
    Console.WriteLine("-------------------------------");
    
    Console.WriteLine("\ntrie memory breakdown:");
    Console.WriteLine($"  node overhead: {trieNodeBytes / 1024.0} KB ({trieNodeBytes * 100.0 / trieTotalBytes}%)");
    Console.WriteLine($"  string data: {trieStringBytes / 1024.0} KB ({trieStringBytes * 100.0 / trieTotalBytes}%)");
    Console.WriteLine($"  dictionary structures: {trieDictBytes / 1024.0} KB ({trieDictBytes * 100.0 / trieTotalBytes}%)");
    Console.WriteLine($"  document logs: {trieDocLogBytes / 1024.0} KB ({trieDocLogBytes * 100.0 / trieTotalBytes}%)");
    Console.WriteLine($"  position lists: {triePositionBytes / 1024.0} KB ({triePositionBytes * 100.0 / trieTotalBytes}%)");
    Console.WriteLine($"  total: {trieNodes} nodes, {trieTotalBytes / 1024.0} KB");
    
    Console.WriteLine("\ninverted index memory breakdown:");
    Console.WriteLine($"  dictionary overhead: {invDictBytes / 1024.0} KB ({invDictBytes * 100.0 / invTotalBytes}%)");
    Console.WriteLine($"  word strings: {invStringBytes / 1024.0} KB ({invStringBytes * 100.0 / invTotalBytes}%)");
    Console.WriteLine($"  document entries: {invEntryBytes / 1024.0} KB ({invEntryBytes * 100.0 / invTotalBytes}%)");
    Console.WriteLine($"  position lists: {invPositionBytes / 1024.0} KB ({invPositionBytes * 100.0 / invTotalBytes}%)");
    Console.WriteLine($"  total: {wordCount} words, {entryCount} entries, {invTotalBytes / 1024.0} KB");
    
    Console.WriteLine($"\nspace ratio: {trieTotalBytes / (double)invTotalBytes}x");
}
    
    
}
