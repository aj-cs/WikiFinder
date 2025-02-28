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
        
        // Linked list that stores document IDs
        private class DocumentLog
        {
            public int DocumentId;
            public DocumentLog Next;
            public DocumentLog(int documentId, DocumentLog next)
            {
                DocumentId = documentId;
                Next = next;
            }
        }

        public Index(string filename)
        {
            root = null;
            documentIdMap = new Dictionary<string, int>();
            documentTitles = new List<string>();
            nextDocumentId = 0;
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
                                InsertWord(word, currentTitle);
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

        // InserWord filters the word and then recursively inserts it
        private void InsertWord(string word, string title)
        {
            string filtered = FilterWord(word);
            if (filtered.Length == 0)
                return;
            root = Insert(root, filtered, 0, title);
        }

        // recursive insertion into the ternary trie
        private TrieNode Insert(TrieNode node, string word, int d, string title)
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
                node.left = Insert(node.left, word, d, title);
            else if (c > node.c)
                node.right = Insert(node.right, word, d, title);
            else
            {
                if (d < word.Length - 1)
                    node.mid = Insert(node.mid, word, d + 1, title);
                else
                {
                    node.isEndOfWord = true;
                    node.count++;
                    if (node.log == null)
                        node.log = new DocumentLog(documentId, null);
                    else if (!DocExists(node.log, documentId))
                        node.log = new DocumentLog(documentId, node.log);
                }
            }
            return node;
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