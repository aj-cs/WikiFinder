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
            public string strSegment;
            public TrieNode left, mid, right;
            public DocumentLog log;
            public int count;
            public bool isEndOfWord;

            public TrieNode(string segment)
            {
                strSegment = segment;
                left = mid = right = null;
                log = null;
                count = 0;
                isEndOfWord = false;
            }
        }

        // linked list that stores document titles
        private class DocumentLog
        {
            public string Title;
            public DocumentLog Next;
            public DocumentLog(string title, DocumentLog next)
            {
                Title = title;
                Next = next;
            }
        }

        public Index(string filename)
        {
            root = null;
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
        // d is the index in the word from which we still have to match
        private TrieNode Insert(TrieNode node, string word, int d, string title)
        {
            if (node == null)
            {
                // create a new node with the remainign substring
                TrieNode newNode = new TrieNode(word.Substring(d));
                newNode.isEndOfWord = true;
                newNode.count = 1;
                newNode.log = new DocumentLog(title, null);
                return newNode;
            }

            char currentChar = word[d];
            if (currentChar < node.strSegment[0])
            {
                node.left = Insert(node.left, word, d, title);
                return node;
            }
            else if (currentChar > node.strSegment[0])
            {
                node.right = Insert(node.right, word, d, title);
                return node;
            }
            else
            {
                // else case is when they share the first character, so now we compute
                // the longest common prefix between node.segment and the word starting at d
                int i = 0;
                while (i < node.strSegment.Length
                        && d + i < word.Length
                        && node.strSegment[i] == word[d + i])
                {
                    i++;
                }

                if (i < node.strSegment.Length)
                {
                    // partially matches, we split the node

                    TrieNode splitNode = new TrieNode(node.strSegment.Substring(i));
                    splitNode.mid = node.mid;
                    splitNode.isEndOfWord = node.isEndOfWord;
                    splitNode.count = node.count;
                    splitNode.log = node.log;

                    // now we adjust the current node, basically its segment becomes the common prefix
                    node.strSegment = node.strSegment.Substring(0, i);
                    node.isEndOfWord = false;
                    node.count = 0;
                    node.log = null;
                    node.mid = splitNode;

                    // we insert the remainder of the new word if there is one
                    if (d + i < word.Length)
                    {
                        node.mid = Insert(node.mid, word, d + i, title);
                    }

                    else
                    {
                        // when the new word exactly matches the common prefix
                        node.isEndOfWord = true;
                        node.count++;
                        if (!DocExists(node.log, title))
                        {
                            node.log = new DocumentLog(title, node.log);
                        }
                    }
                    return node;
                }
                else
                {
                    // node segment completely matches the corresponding part of the word
                    if (d + i == word.Length)
                    {
                        // word matches exactly
                        node.isEndOfWord = true;
                        node.count++;
                        if (!DocExists(node.log, title))
                        {
                            node.log = new DocumentLog(title, node.log);
                        }
                    }
                    else
                    {
                        // cotniue with insertion on the mid child with the remaining part
                        node.mid = Insert(node.mid, word, d + i, title);
                    }

                    return node;
                }
            }

        }

        private bool DocExists(DocumentLog log, string title)
        {
            while (log != null)
            {
                if (log.Title.Equals(title))
                    return true;
                log = log.Next;
            }
            return false;
        }

        // exact match seach
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
                    Console.WriteLine(log.Title);
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
            {
                return null;
            }
            if (d >= word.Length)
            {
                return node;
            }
            char currentChar = word[d];
            if (currentChar < node.strSegment[0])
            {
                return Get(node.left, word, d);
            }
            else if (currentChar > node.strSegment[0])
            {
                return Get(node.right, word, d);
            }
            else
            {
                int i = 0;
                while (i < node.strSegment.Length
                        && d + i < word.Length
                        && node.strSegment[i] == word[d + i])
                {
                    i++;
                }

                if (i < node.strSegment.Length)
                {
                    // mismatch in the middle of the node.segment, word is not present
                    return null;
                }

                if (d + i == word.Length)
                {
                    return node;
                }
                return Get(node.mid, word, d + i);

            }
        }

        // returns the node where the prefix search should start
        // this handles the case where the prefix is within a nodes segemnt
        private TrieNode GetPrefixNode(TrieNode node, string prefix, int d)
        {
            if (node == null)
            {
                return null;
            }

            if (d >= prefix.Length)
            {
                return node;
            }

            char currentChar = prefix[d];
            if (currentChar < node.strSegment[0])
            {
                return GetPrefixNode(node.left, prefix, d);
            }

            else if (currentChar > node.strSegment[0])
            {
                return GetPrefixNode(node.right, prefix, d);
            }

            else
            {
                int i = 0;
                while (i < node.strSegment.Length
                        && d + i < prefix.Length
                        && node.strSegment[i] == prefix[d + i])
                {
                    i++;
                }
                if (d + i == prefix.Length)
                    return node;
                if (i == node.strSegment.Length)
                    return GetPrefixNode(node.mid, prefix, d + i);
                return null;
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
                        Console.WriteLine(log.Title);
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
            HashSet<string> documents = new HashSet<string>();
            List<(string word, TrieNode node)> completions = new List<(string, TrieNode)>();
            if (node.isEndOfWord)
                completions.Add((prefix, node));
            Collect(node.mid, prefix, completions);
            foreach (var (wordComp, trieNode) in completions)
            {
                DocumentLog log = trieNode.log;
                while (log != null)
                {
                    documents.Add(log.Title);
                    log = log.Next;
                }
            }
            if (documents.Count == 0)
            {
                Console.WriteLine($"No documents found for prefix: '{prefix}'");
            }
            else
            {
                Console.WriteLine($"Documents containing a word starting with '{prefix}':");
                foreach (var doc in documents)
                {
                    Console.WriteLine(doc);
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
            string word = prefix + node.strSegment;
            if (node.isEndOfWord)
                completions.Add((word, node));

            // traverse the mid subtree which extends the prefix.
            Collect(node.mid, word, completions);

            // traverse right subtree (prefix doesnt unchanged)
            Collect(node.right, prefix, completions);
        }
    }
}

