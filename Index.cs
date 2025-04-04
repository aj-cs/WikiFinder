using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq; 
using System.Net.Http; 
using System.Text.Json; 
using System.Threading.Tasks;

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
    // Flag to enable/disable delta encoding
    private bool deltaEncodingEnabled = true;
    public void SetDeltaEncoding(bool enabled)
    {
        deltaEncodingEnabled = enabled;
    }
    private Dictionary<string, BitArray> bitIndex;
    private bool bitIndexBuilt = false;
    // Inverted index
    private Dictionary<string, Dictionary<int, (List<int> Positions, int Count)>> invertedIndex; 
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
            foreach (var docId in kv.Value.Keys)
                bits.Set(docId, true);
            bitIndex[kv.Key] = bits;
        }
    }

    public Index(string filename)
    {
        root = new TrieNode("");
        invertedIndex = new Dictionary<string, Dictionary<int, (List<int>, int)>>();
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

    private static string FilterWord(ReadOnlySpan<char> input)
    {
        Span<char> buffer = stackalloc char[input.Length];
        int index = 0;
        foreach (char c in input)
        {
            if (char.IsLetterOrDigit(c))
                buffer[index++] = char.ToLower(c);
        }
        return new string(buffer[..index]);
    }

    private void ProcessLine(string line, string currentTitle)
    {
        ReadOnlySpan<char> span = line.AsSpan();
        while (!span.IsEmpty)
        {
            int nextSpace = span.IndexOf(' ');
            ReadOnlySpan<char> word = nextSpace >= 0 ? span[..nextSpace] : span;
            
            InsertWord(word.ToString(), currentTitle, documentPositionCounter);
            documentPositionCounter++;

            if (nextSpace < 0) break;
            span = span[(nextSpace + 1)..];
        }
    }
    private void InsertWord(string word, string title, int position)
    {
        string filtered = FilterWord(word);
        if (filtered.Length == 0)
            return;
            
        InsertIntoTrie(root, filtered, title, position);
        InsertIntoInvertedIndex(filtered, title, position);
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
        if (!invertedIndex.ContainsKey(word))
        {
            invertedIndex[word] = new Dictionary<int, (List<int> Positions, int Count)>();
        }
        if (!invertedIndex[word].ContainsKey(documentId))
        {
            invertedIndex[word][documentId] = (new List<int>(), 0);
        }
        var (positions, count) = invertedIndex[word][documentId];
        if (deltaEncodingEnabled && positions.Count > 0)
        {
            positions.Add(position - positions[^1]);
        }
        else
        {
            positions.Add(position);
        }
        invertedIndex[word][documentId] = (positions, count + 1);
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

    public bool Search(string query)
    {
        var words = query.Split(" ", StringSplitOptions.RemoveEmptyEntries);
        if (words.Length > 1)
        {
            if (query.Contains("&&") || (query.Contains("||")))
            {
                Console.WriteLine("Boolean search");
                return BooleanSearchBitsetIndex(query);
            }
            else
            {
                Console.WriteLine("Phrase search");
                return PhraseSearchIndex(query);
            }
        }
        else 
        {
            if (query.EndsWith("*"))
            {
                Console.WriteLine("Prefix search");
                query.Remove(query.Length - 1);
                return PrefixSearchTrie(query);
            }
            if (query.EndsWith("#"))
            {
                Console.WriteLine("Ranked search");
                query.Remove(query.Length - 1);
                return SearchRankedIndex((query));
            }
            else
            {
                Console.WriteLine("Normal search");
                return SearchIndex(query);
            }
            
        }
    }
    public bool SearchTrie(string searchStr)
    {
        string word = FilterWord(searchStr);
        TrieNode node = FindWord(word);
        if (node != null)
        {
            Console.WriteLine("Found " + searchStr + " in:");
            DocumentLog log = node.Log;
            while (log != null)
            {
                Console.WriteLine(documentTitles[log.DocumentId]);
                log = log.Next;
            }
            return true;
        }
        else
        {
            Console.WriteLine("No matches for " + searchStr + " found");
            return false;
        }
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
    public bool AutoComplete(string prefix)
    {
        prefix = FilterWord(prefix);
        if (prefix.Length == 0)
        {
            Console.WriteLine("No valid prefix provided.");
            return false;
        }
        
        var (node, matched) = FindPrefixNode(prefix);
        if (node == null)
        {
            Console.WriteLine("No matches for " + prefix + " found");
            return false;
        }

        List<(string word, TrieNode node)> completions = new List<(string, TrieNode)>();
        CollectWords(node, matched, completions);
        
        if (completions.Count == 0)
        {
            Console.WriteLine("No complete words found with prefix " + prefix);
            return false;
        }
        else
        {
            foreach (var (wordComp, trieNode) in completions)
            {
                Console.WriteLine("Found " + wordComp + " in:");
                DocumentLog log = trieNode.Log;
                while (log != null)
                {
                    Console.WriteLine(documentTitles[log.DocumentId]);
                    log = log.Next;
                }
            }
            return true;
        }
    }

    // NOTE: returns the (unique) documents where any word starting with the given prefix appears
    public bool PrefixSearchTrie(string prefix)
    {
        prefix = FilterWord(prefix);
        if (prefix.Length == 0)
        {
            Console.WriteLine("No valid prefix provided.");
            return false;
        }
        
        var (node, matched) = FindPrefixNode(prefix);
        if (node == null)
        {
            Console.WriteLine("No matches for " + prefix + " found");
            return false;
        }

        HashSet<int> documentIds = new HashSet<int>();
        List<(string word, TrieNode node)> completions = new List<(string, TrieNode)>();
        CollectWords(node, matched, completions);
        
        foreach (var (wordComp, trieNode) in completions)
        {
            DocumentLog log = trieNode.Log;
            while (log != null)
            {
                documentIds.Add(log.DocumentId);
                log = log.Next;
            }
        }
        
        if (documentIds.Count == 0)
        {
            Console.WriteLine($"No documents found for prefix: '{prefix}'");
            return false;
        }
        else
        {
            Console.WriteLine($"Documents containing a word starting with '{prefix}':");
            foreach (var docId in documentIds)
            {
                Console.WriteLine(documentTitles[docId]);
            }
            return true;
        }
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

    public bool PhraseSearchTrie(string phrase)
    {
        string[] words = phrase.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
        {
            Console.WriteLine("Invalid search provided.");
            return false;
        }
        if (words.Length == 1)
        {
            return SearchTrie(words[0]);
        }

        for (int i = 0; i < words.Length; i++)
        {
            words[i] = FilterWord(words[i]);
        }

        TrieNode firstNode = FindWord(words[0]);
        if (firstNode == null)
        {
            Console.WriteLine($"Phrase '{phrase}' not found - first word doesn't exist");
            return false;
        }

        Dictionary<int, List<int>> candidateDocs = new Dictionary<int, List<int>>();
        DocumentLog log = firstNode.Log;
        while (log != null)
        {
            candidateDocs[log.DocumentId] = new List<int>(log.Positions);
            log = log.Next;
        }

        for (int wordIndex = 1; wordIndex < words.Length; wordIndex++)
        {
            TrieNode node = FindWord(words[wordIndex]);
            if (node == null)
            {
                Console.WriteLine($"Phrase '{phrase}' not found - word '{words[wordIndex]}' doesn't exist");
                return false;
            }

            Dictionary<int, List<int>> newCandidates = new Dictionary<int, List<int>>();
            foreach (var docEntry in candidateDocs)
            {
                int docId = docEntry.Key;
                List<int> prevPositions = docEntry.Value;
                DocumentLog currLog = FindDocLog(node.Log, docId);

                if (currLog != null)
                {
                    
                    List<int> validPositions = MergePositions(prevPositions, currLog.Positions);
                    if (validPositions.Count > 0)
                    {
                        newCandidates[docId] = validPositions;
                    }
                }
            }

            candidateDocs = newCandidates;
            if (candidateDocs.Count == 0)
            {
                Console.WriteLine($"Phrase '{phrase}' not found in any document");
                return false;
            }
        }
        
        Console.WriteLine($"Found phrase '{phrase}' in:");
        foreach (var docId in candidateDocs.Keys)
        {
            Console.WriteLine(documentTitles[docId]);
        }
        return true;
    }
    
    //exact match search using inverted index
    public bool SearchIndex(string searchStr)
    {
        string word = FilterWord(searchStr);
        if (!invertedIndex.ContainsKey(word))
        {
            Console.WriteLine("No matches for " + searchStr + " found");
            return false;
        }

        Console.WriteLine("Found " + searchStr + " in:");
        foreach (var docId in invertedIndex[word].Keys)
        {
            Console.WriteLine(documentTitles[docId]);
        }
        return true;
    }

    // prefix search using inverted index
    public bool PrefixSearchIndex(string prefix)
    {
        prefix = FilterWord(prefix);
        if (prefix.Length == 0)
        {
            Console.WriteLine("No valid prefix provided.");
            return false;
        }

        var matches = invertedIndex.Keys.Where(k => k.StartsWith(prefix)).ToList();
        if (matches.Count == 0)
        {
            Console.WriteLine("No matches for " + prefix + " found");
            return false;
        }

        foreach (var word in matches)
        {
            Console.WriteLine("Found " + word + " in:");
            foreach (var docId in invertedIndex[word].Keys)
            {
                Console.WriteLine(documentTitles[docId]);
            }
        }

        return true;
    }

    // Phrase search using inverted index
    public bool PhraseSearchIndex(string phrase)
    {
        string[] words = phrase.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
        {
            Console.WriteLine("Invalid search provided.");
            return false;
        }
        
        if (words.Length == 1)
        {
            return SearchIndex(words[0]);
        }

        for (int i = 0; i < words.Length; i++)
        {
            words[i] = FilterWord(words[i]);
        }

        if (!invertedIndex.ContainsKey(words[0]))
        {
            Console.WriteLine($"Phrase '{phrase}' not found - first word doesn't exist");
            return false;
        }

        Dictionary<int, List<int>> candidateDocs = new Dictionary<int, List<int>>();
        foreach (var entry in invertedIndex[words[0]])
        {
            candidateDocs[entry.Key] = new List<int>(entry.Value.Positions);
        }

        for (int wordIndex = 1; wordIndex < words.Length; wordIndex++)
        {
            if (!invertedIndex.ContainsKey(words[wordIndex]))
            {
                Console.WriteLine($"Phrase '{phrase}' not found - word '{words[wordIndex]}' doesn't exist");
                return false;
            }

            Dictionary<int, List<int>> newCandidates = new Dictionary<int, List<int>>();
            foreach (var docEntry in candidateDocs)
            {
                int docId = docEntry.Key;
                List<int> prevPositions = docEntry.Value;

                if (invertedIndex[words[wordIndex]].TryGetValue(docId, out var currPosition))
                {
                    List<int> validPositions = MergePositions(prevPositions, currPosition.Positions);
                    if (validPositions.Count > 0)
                    {
                        newCandidates[docId] = validPositions;
                    }
                }
            }

            candidateDocs = newCandidates;
            if (candidateDocs.Count == 0)
            {
                Console.WriteLine($"Phrase '{phrase}' not found in any document");
                return false;
            }
        }

        Console.WriteLine($"Found phrase '{phrase}' in:");
        foreach (var docId in candidateDocs.Keys)
        {
            Console.WriteLine(documentTitles[docId]);
        }
        return true;
    }

    // ranked search using inverted index
    public bool SearchRankedIndex(string searchStr)
    {
        string word = FilterWord(searchStr);
        if (!invertedIndex.ContainsKey(word))
        {
            Console.WriteLine("No matches for " + searchStr + " found");
            return false;
        }
    
        // use the precomputed counts directly
        var docCounts = invertedIndex[word].ToDictionary(
            entry => entry.Key, 
            entry => entry.Value.Count
        );
    
        var sortedDocs = new List<KeyValuePair<int, int>>(docCounts);
        sortedDocs.Sort((a, b) => b.Value.CompareTo(a.Value));
        Console.WriteLine("Found " + searchStr + " in (ranked by occurrences):");
        foreach (var docPair in sortedDocs)
        {
            Console.WriteLine(documentTitles[docPair.Key] + " (occurrences: " + docPair.Value + ")");
        }
        return true;
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

    public bool RankedSearchTrie(string searchStr)
    {
        string word = FilterWord(searchStr);
        TrieNode node = FindWord(word);
        if (node == null)
        {
            Console.WriteLine("No matches for " + searchStr + " found");
            return false;
        }
        
        //dictionary to store document frequencies in descending order
        Dictionary<int, int> docCounts = new Dictionary<int, int>();
        DocumentLog log = node.Log;
        
        while (log != null)
        {
            docCounts[log.DocumentId] = log.Count;
            log = log.Next;
        }

        // convert to a list and sort by descending frequency
        var rankedResults = docCounts.ToList();
        rankedResults.Sort((a, b) => b.Value.CompareTo(a.Value));

        Console.WriteLine($"Ranked results for '{searchStr}':");
        foreach (var result in rankedResults)
        {
            Console.WriteLine($"{documentTitles[result.Key]} (relevance score: {result.Value})");
        }
        
        return rankedResults.Count > 0;
    }
//searchStr example "word1 AND word2 OR word3"
    public bool BooleanSearchNaiveIndex(string SearchStr)
    {
        var words = SearchStr.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        HashSet<int> result = null;
        string op = null;

        foreach (var word in words)
        {
            if (word == "&&" || word == "||") { op = word; continue; }

            string filterWord = FilterWord(word);
            if (!invertedIndex.TryGetValue(filterWord, out var posting))
            {
                Console.WriteLine($"Word '{filterWord}' not found.");
                return false;
            }

            var docs = new HashSet<int>(posting.Keys);
            if (result == null) result = docs;
            else if (op == "&&") result.IntersectWith(docs);
            else if (op == "||") result.UnionWith(docs);
            op = null;
        }

        if (result == null || result.Count == 0)
        {
            Console.WriteLine("No matching documents found.");
            return false;
        }

        Console.WriteLine($"Naive Index results for '{SearchStr}':");
        foreach (var id in result) 
            Console.WriteLine(documentTitles[id])
            ;
        return true;
    }

    public bool BooleanSearchBitsetIndex(string SearchStr)
    {
        BuildBitIndex();
        var words = SearchStr.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        BitArray result = null;
        string op = null;

        foreach (var word in words)
        {
            if (word == "&&" || word == "||") { op = word; continue; }

            string filterWord = FilterWord(word);
            bitIndex.TryGetValue(filterWord, out BitArray bits);
            bits ??= new BitArray(nextDocumentId);

            if (result == null) result = (BitArray)bits.Clone();
            else if (op == "&&") result.And(bits);
            else if (op == "||") result.Or(bits);

            op = null;
        }

        if (result == null) return false;
        var found = Enumerable.Range(0, nextDocumentId).Where(i => result.Get(i)).ToList();
        if (!found.Any())
        {
            Console.WriteLine("No matches.");
            return false;
        }

        Console.WriteLine($"Bitset Index results for '{SearchStr}':");
        foreach (int id in found)
            Console.WriteLine(documentTitles[id]);
        return true;
    }

    public bool BooleanSearchNaiveTrie(string SearchStr)
    {
        var words = SearchStr.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        HashSet<int> result = null;
        string op = null;

        foreach (var word in words)
        {
            if (word == "&&" || word == "||") { op = word; continue; }

            string filterWord = FilterWord(word);
            TrieNode node = FindWord(filterWord);
            var docs = new HashSet<int>();
            if (node != null)
            {
                for (var log = node.Log; log != null; log = log.Next)
                    docs.Add(log.DocumentId);
            }
            if (result == null) result = docs;
            else if (op == "&&") result.IntersectWith(docs);
            else if (op == "||") result.UnionWith(docs);
            op = null;
        }

        if (result == null || result.Count == 0)
        {
            Console.WriteLine("No matches.");
            return false;
        }
        Console.WriteLine($"Naive Trie results for '{SearchStr}':");
        foreach (var id in result)
            Console.WriteLine(documentTitles[id]);
        return true;
    }

    public bool BooleanSearchBitsetTrie(string SearchStr)
    {
        BuildBitIndex();
        var words = SearchStr.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        BitArray result = null;
        string op = null;

        foreach (var word in words)
        {
            if (word == "&&" || word == "||") { op = word; continue; }

            string filterWord = FilterWord(word);
            TrieNode node = FindWord(filterWord);
            var bits = new BitArray(nextDocumentId);
            if (node != null)
            {
                for (var log = node.Log; log != null; log = log.Next)
                    bits.Set(log.DocumentId, true);
            }

            if (result == null) result = bits;
            else if (op == "&&") result.And(bits);
            else if (op == "||") result.Or(bits);
            op = null;
        }
        if (result == null) return false;
        var found = Enumerable.Range(0, nextDocumentId).Where(i => result.Get(i)).ToList();
        if (!found.Any())
        {
            Console.WriteLine("No matches.");
            return false;
        }

        Console.WriteLine($"Bitset Trie results for '{SearchStr}':");
        foreach (var id in found) 
            Console.WriteLine(documentTitles[id]);
        return true;
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

        // remove from inverted index:
        foreach (var word in invertedIndex.Keys.ToList())
        {
            if (invertedIndex[word].ContainsKey(docId))
            {
                invertedIndex[word].Remove(docId);
                if (invertedIndex[word].Count == 0)
                    invertedIndex.Remove(word);
            }
        }
        RemovePublicationFromTrie(root, docId);

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

public (int wordCount, int entryCount, long dictBytes, long stringBytes, long entryBytes, long positionBytes, long totalBytes) GetInvertedIndexStats()
{
    int wordCount = invertedIndex.Count;
    int entryCount = 0;
    long dictBytes = 24; // main dictionary overhead
    long stringBytes = 0;
    long entryBytes = 0;
    long positionBytes = 0;

    foreach (var wordEntry in invertedIndex)
    {
        // word string
        stringBytes += wordEntry.Key.Length * 2; // 2 bytes per char
        dictBytes += 16; // dictionary overhead for inner dict

        foreach (var docEntry in wordEntry.Value)
        {
            entryCount++;

            // document entry
            entryBytes += 4 + 16 + 4; // int key (document id) + tuple overhead + count value
            positionBytes += 16 + docEntry.Value.Positions.Count * 4; // list overhead + int positions
        }
    }

    long totalBytes = dictBytes + stringBytes + entryBytes + positionBytes;
    return (wordCount, entryCount, dictBytes, stringBytes, entryBytes, positionBytes, totalBytes);
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
