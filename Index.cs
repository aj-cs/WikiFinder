using System;
using System.Collections.Generic;
using System.IO;

namespace SearchEngineProject
{
    internal class Index
    {
        private TrieNode root;

        /* ternary trie node stores one character,
        *pointers to left, mid, and right subtrees,
        * and (if this node terminates a word) a document log.
        */
        private class TrieNode
        {
            public char c;
            public TrieNode left, mid, right;
            public DocumentLog log;
            public int count;
            public bool isEndOfWord;

            public TrieNode(char c)
            {
                this.c = c;
                left = mid = right = null;
                log = null;
                count = 0;
                isEndOfWord = false;
            }
        }

        // Document ID mapping system to store document titles in a more efficient way
        private Dictionary<string, int> documentIdMap;
        private List<string> documentTitles;
        private int nextDocumentId;

        private int documentPositionCounter;
        
        // Linked list that stores document IDs
        private class DocumentLog
        {
            public int DocumentId;
            public List<int> Positions;
            public DocumentLog Next;
            public DocumentLog(int documentId, int position, DocumentLog next)
            {
                DocumentId = documentId;
                Positions = new List<int> { position };
                Next = next;
            }
        }

        public Index(string filename)
        {
            root = null;
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
                            documentPositionCounter = 0;
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
                            for (int i = 0; i < words.Length; i++)
                            {
                                InsertWord(words[i], currentTitle, documentPositionCounter++);
                            }
                        }
                    }
                }
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("Error reading file " + filename);
            }
        }

        // filters a word to contain only letters and digits
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

        // InsertWord filters the word and then recursively inserts it
        private void InsertWord(string word, string title, int position)
        {
            string filtered = FilterWord(word);
            if (filtered.Length == 0)
                return;
            root = Insert(root, filtered, 0, title, position);
        }

        // recursive insertion into the ternary trie
        private TrieNode Insert(TrieNode node, string word, int d, string title, int position)
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

            char c = word[d];
            if (node == null)
                node = new TrieNode(c);
            if (c < node.c)
                node.left = Insert(node.left, word, d, title, position);
            else if (c > node.c)
                node.right = Insert(node.right, word, d, title, position);
            else
            {
                if (d < word.Length - 1)
                    node.mid = Insert(node.mid, word, d + 1, title, position);
                else
                {
                    node.isEndOfWord = true;
                    node.count++;
                    DocumentLog existing = FindDocLog(node.log, documentId);
                    if (existing != null)
                    {
                        if (!existing.Positions.Contains(position))
                            existing.Positions.Add(position);
                    }
                    else
                    {
                        node.log = new DocumentLog(documentId, position, node.log);
                    }
                }
            }
            return node;
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

        // exact match search
        public bool Search(string searchStr)
        {
            string word = FilterWord(searchStr);
            TrieNode node = Get(root, word, 0);
            if (node != null && node.isEndOfWord)
            {
                Console.WriteLine("Found " + searchStr + " in:");
                DocumentLog log = node.log;
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

        // recursive search to return the node corresponding to the key
        private TrieNode Get(TrieNode node, string word, int d)
        {
            if (node == null)
                return null;
            char c = word[d];
            if (c < node.c)
                return Get(node.left, word, d);
            else if (c > node.c)
                return Get(node.right, word, d);
            else
            {
                if (d == word.Length - 1)
                    return node;
                else
                    return Get(node.mid, word, d + 1);
            }
        }

        // auto comp lists all words starting with the given prefix and their documents
        public void PrefixSearch(string prefix)
        {
            prefix = FilterWord(prefix);
            if (prefix.Length == 0)
            {
                Console.WriteLine("No valid prefix provided.");
                return;
            }
            TrieNode node = Get(root, prefix, 0);
            if (node == null)
            {
                Console.WriteLine("No matches for " + prefix + " found");
                return;
            }
            List<(string word, TrieNode node)> completions = new List<(string, TrieNode)>();
            // if the prefix itself is a word then add it
            if (node.isEndOfWord)
                completions.Add((prefix, node));
            // collects all completions from the mid subtree
            Collect(node.mid, prefix, completions);
            if (completions.Count == 0)
            {
                Console.WriteLine("No complete words found with prefix " + prefix);
            }
            else
            {
                foreach (var (wordComp, trieNode) in completions)
                {
                    Console.WriteLine("Found " + wordComp + " in:");
                    DocumentLog log = trieNode.log;
                    while (log != null)
                    {
                        Console.WriteLine(documentTitles[log.DocumentId]);
                        log = log.Next;
                    }
                }
            }
        }

        // prefix search that returns unique documents where any word starting with the prefix appears
        public void PrefixSearchDocuments(string prefix)
        {
            prefix = FilterWord(prefix);
            if (prefix.Length == 0)
            {
                Console.WriteLine("No valid prefix provided.");
                return;
            }
            TrieNode node = Get(root, prefix, 0);
            if (node == null)
            {
                Console.WriteLine("No matches for " + prefix + " found");
                return;
            }
            HashSet<int> documentIds = new HashSet<int>();
            List<(string word, TrieNode node)> completions = new List<(string, TrieNode)>();
            if (node.isEndOfWord)
                completions.Add((prefix, node));
            Collect(node.mid, prefix, completions);
            foreach (var (wordComp, trieNode) in completions)
            {
                DocumentLog log = trieNode.log;
                while (log != null)
                {
                    documentIds.Add(log.DocumentId);
                    log = log.Next;
                }
            }
            if (documentIds.Count == 0)
            {
                Console.WriteLine($"No documents found for prefix: '{prefix}'");
            }
            else
            {
                Console.WriteLine($"Documents containing a word starting with '{prefix}':");
                foreach (var docId in documentIds)
                {
                    Console.WriteLine(documentTitles[docId]);
                }
            }
        }
        public bool PhraseSearch(string phrase)
        {
            string[] words = phrase.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0)
            {
                Console.WriteLine("invalid search provided.");
                return false;
            }
            if (words.Length == 1)
            {
                return Search(words[0]);
            }
            for (int i = 0; i < words.Length; i++)
            {
                words[i] = FilterWord(words[i]);
            }
            TrieNode firstNode = Get(root, words[0], 0);
            if (firstNode == null || !firstNode.isEndOfWord)
            {
                Console.WriteLine($"Phrase '{phrase}' not found - first word doesn't exist");
                return false;
            }
            Dictionary<int, List<int>> candidateDocs = new Dictionary<int, List<int>>();
            DocumentLog log = firstNode.log;
            while (log != null)
            {
                candidateDocs[log.DocumentId] = new List<int>(log.Positions);
                log = log.Next;
            }
            for (int wordIndex = 1; wordIndex < words.Length; wordIndex++)
            {
                TrieNode node = Get(root, words[wordIndex], 0);
                if (node == null || !node.isEndOfWord)
                {
                    Console.WriteLine($"Phrase '{phrase}' not found - word '{words[wordIndex]}' doesn't exist");
                    return false;
                }
                Dictionary<int, List<int>> newCandidates = new Dictionary<int, List<int>>();
                foreach (var docEntry in candidateDocs)
                {
                    int docId = docEntry.Key;
                    List<int> prevPositions = docEntry.Value;
                    DocumentLog currLog = FindDocLog(node.log, docId);
                    if (currLog != null)
                    {
                        List<int> validPositions = new List<int>();
                        HashSet<int> currPositions = new HashSet<int>(currLog.Positions);
                        foreach (int prevPos in prevPositions)
                        {
                            if (currPositions.Contains(prevPos + 1))
                            {
                                validPositions.Add(prevPos + 1);
                            }
                        }
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
        public bool SearchRanked(string searchStr)
        {
            string word = FilterWord(searchStr);
            TrieNode node = Get(root, word, 0);
            if (node == null || !node.isEndOfWord)
            {
                Console.WriteLine("No matches for " + searchStr + " found");
                return false;
            }
            //collect occurrence counts for each document
            Dictionary<int, int> docCounts = new Dictionary<int, int>();
            DocumentLog log = node.log;
            while (log != null)
            {
                if (!docCounts.ContainsKey(log.DocumentId))
                    docCounts[log.DocumentId] = 0;
                docCounts[log.DocumentId] += log.Positions.Count;
                log = log.Next;
            }

            //sort descending order of occurrences
            var sortedDocs = new List<KeyValuePair<int, int>>(docCounts);
            sortedDocs.Sort((a, b) => b.Value.CompareTo(a.Value));

            Console.WriteLine("Found " + searchStr + " in (ranked by occurrences):");
            foreach (var docPair in sortedDocs)
            {
                Console.WriteLine(documentTitles[docPair.Key] + " (occurrences: " + docPair.Value + ")");
            }
            return true;
        }

        // recursively collects complete words from the ternary trie
        // traverses the subtree from starting at 'node' then builds completions
        private void Collect(TrieNode node, string prefix, List<(string, TrieNode)> completions)
        {
            if (node == null)
                return;

            // traverse left subtree
            Collect(node.left, prefix, completions);

            // process the current node
            string word = prefix + node.c;
            if (node.isEndOfWord)
                completions.Add((word, node));

            // traverse the mid subtree which extends the prefix.
            Collect(node.mid, word, completions);

            // traverse right subtree (prefix doesnt unchanged)
            Collect(node.right, prefix, completions);
        }
    }
}