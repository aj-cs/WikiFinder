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
    // Inverted index
    private Dictionary<string, Dictionary<int, (List<int> Positions, int Count)>> invertedIndex; 
    // variables to keep track of and compress documents 
    private Dictionary<string, int> documentIdMap; // maps document titles to IDs
    private List<string> documentTitles;           // list of document titles
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

    public Index(string filename)
    {
        root = new TrieNode("");
        invertedIndex = new Dictionary<string, Dictionary<int, (List<int>, int)>>();
        documentIdMap = new Dictionary<string, int>();
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
                        // Console.WriteLine(currentTitle);
                        titleRead = true;
                    }
                    else
                    {
                        string[] words = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        foreach (string word in words)
                        {
                            InsertWord(word, currentTitle, documentPositionCounter++);
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

    private string FilterWord(string word)
    {
        string result = "";
        foreach (char ch in word.ToLower())
        {
            if (char.IsLetterOrDigit(ch))
                result += ch;
        }
        return result;
    }

    private void InsertWord(string word, string title, int position)
    {
        string filtered = FilterWord(word);
        if (filtered.Length == 0)
            return;
            
        //InsertIntoTrie(root, filtered, title, position);
        InsertIntoInvertedIndex(filtered, title, position);
    }
    private void InsertIntoInvertedIndex(string word, string title, int position)
    {
        int documentId;
        if (!documentIdMap.ContainsKey(title))
        {
            documentId = nextDocumentId++;
            documentIdMap[title] = documentId;
            documentTitles.Add(title);
        }
        else
        {
            documentId = documentIdMap[title];
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
        positions.Add(position);
        invertedIndex[word][documentId] = (positions,count + 1); 
    }

    //recursive insertion into the tree
    // node is the current node and key is the substring that we need to insert
    private void InsertIntoTrie(TrieNode node, string key, string title, int position)
    {
        int documentId;
        if (!documentIdMap.ContainsKey(title))
        {
            documentId = nextDocumentId++;
            documentIdMap[title] = documentId;
            documentTitles.Add(title);
        }
        else
        {
            documentId = documentIdMap[title];
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
    
        // Use the precomputed counts directly
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
            Console.WriteLine("{documentTitles[result.Key]} (relevance score: {result.Value})");
        }
        
        return rankedResults.Count > 0;
    }
}