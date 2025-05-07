using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Text;

namespace SearchEngine.Services
{
    public class FileContentService
    {
        private readonly string _filePath;
        private readonly Dictionary<string, string> _documentContents;

        public FileContentService(string filePath)
        {
            _filePath = filePath;
            _documentContents = new Dictionary<string, string>();
        }

        public void ClearDocumentPositions()
        {
            _documentContents.Clear();
        }
        
        public void RegisterDocument(string title, string content)
        {
            _documentContents[title] = content;
        }

        public Task<string> GetDocumentContentAsync(string title)
        {
            if (_documentContents.TryGetValue(title, out var content))
                return Task.FromResult(content);
                
            return Task.FromResult(string.Empty);
        }
        
        public async Task<string> GetSnippetAsync(string title, string query, string operation)
        {
            var content = await GetDocumentContentAsync(title);
            if (string.IsNullOrEmpty(content))
                return $"No content available for {title}";
                
            // split content into lines
            var lines = content.Split('\n').ToArray();
            
            // find the appropriate line based on the search operation
            string lineWithTerm = null;
            var cleanQuery = query.Replace("*", "").Replace("#", "");
            
            switch (operation?.ToLower())
            {
                case "prefixdocs":
                    // for prefix search, find lines with words starting with the term
                    lineWithTerm = lines.FirstOrDefault(l => 
                        System.Text.RegularExpressions.Regex.IsMatch(l, $@"\b{System.Text.RegularExpressions.Regex.Escape(cleanQuery)}[a-zA-Z0-9]*\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase));
                    break;
                    
                case "fulltext" when query.Contains("&&") || query.Contains("||"):
                    // for boolean search, find lines containing any of the terms
                    var terms = cleanQuery.Split(new[] { "&&", "||" }, StringSplitOptions.RemoveEmptyEntries)
                                         .Select(t => t.Trim())
                                         .Where(t => !string.IsNullOrWhiteSpace(t))
                                         .ToArray();
                    
                    lineWithTerm = lines.FirstOrDefault(l => 
                        terms.Any(term => System.Text.RegularExpressions.Regex.IsMatch(l, $@"\b{System.Text.RegularExpressions.Regex.Escape(term)}\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase)));
                    break;
                    
                case "fulltext" when query.Contains(" "):
                    // for phrase search, find lines with the exact phrase
                    lineWithTerm = lines.FirstOrDefault(l => 
                        l.IndexOf(cleanQuery, StringComparison.OrdinalIgnoreCase) >= 0);
                    break;
                    
                default:
                    // for exact search, find lines with exact word match
                    lineWithTerm = lines.FirstOrDefault(l => 
                        System.Text.RegularExpressions.Regex.IsMatch(l, $@"\b{System.Text.RegularExpressions.Regex.Escape(cleanQuery)}\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase));
                    break;
            }
            
            if (lineWithTerm == null && lines.Length > 0)
                return TruncateAndHighlight(lines[0], cleanQuery, operation, 150);
                
            return lineWithTerm != null 
                ? TruncateAndHighlight(lineWithTerm, cleanQuery, operation, 150) 
                : $"Result for {title}";
        }
        
        private string TruncateAndHighlight(string text, string query, string operation, int maxLength)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;
            
            // clean the query
            var cleanQuery = query.Replace("*", "").Replace("#", "");
            
            // find position for highlighting based on operation
            int position = -1;
            
            switch (operation?.ToLower())
            {
                case "prefixdocs":
                    // for prefix search, find position of any word starting with the query
                    var prefixMatch = System.Text.RegularExpressions.Regex.Match(
                        text, 
                        $@"\b{System.Text.RegularExpressions.Regex.Escape(cleanQuery)}[a-zA-Z0-9]*\b", 
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    position = prefixMatch.Success ? prefixMatch.Index : -1;
                    break;
                    
                case "fulltext" when query.Contains("&&") || query.Contains("||"):
                    // for boolean search, find position of the first term that matches
                    var terms = cleanQuery.Split(new[] { "&&", "||" }, StringSplitOptions.RemoveEmptyEntries)
                                         .Select(t => t.Trim())
                                         .Where(t => !string.IsNullOrWhiteSpace(t));
                    
                    foreach (var term in terms)
                    {
                        var match = System.Text.RegularExpressions.Regex.Match(
                            text, 
                            $@"\b{System.Text.RegularExpressions.Regex.Escape(term)}\b", 
                            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        
                        if (match.Success)
                        {
                            position = match.Index;
                            break;
                        }
                    }
                    break;
                    
                case "fulltext" when query.Contains(" "):
                    // for phrase search, find position of the exact phrase
                    position = text.IndexOf(cleanQuery, StringComparison.OrdinalIgnoreCase);
                    break;
                    
                default:
                    // for exact search, find position of the exact word
                    var exactMatch = System.Text.RegularExpressions.Regex.Match(
                        text, 
                        $@"\b{System.Text.RegularExpressions.Regex.Escape(cleanQuery)}\b", 
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    position = exactMatch.Success ? exactMatch.Index : -1;
                    break;
            }
            
            // if nothing matches, just truncate
            if (position == -1)
            {
                if (text.Length <= maxLength)
                    return text;
                return text.Substring(0, maxLength) + "...";
            }
            
            // determine start and end positions for the snippet
            int startPos = Math.Max(0, position - 40);
            int length = Math.Min(text.Length - startPos, maxLength);
            
            // extract the snippet
            string snippet = text.Substring(startPos, length);
            
            // add ellipsis if we're not at the beginning/end
            if (startPos > 0) snippet = "..." + snippet;
            if (startPos + length < text.Length) snippet += "...";
            
            // highlight based on search operation
            switch (operation?.ToLower())
            {
                case "prefixdocs":
                    // highlight words that start with the query
                    return System.Text.RegularExpressions.Regex.Replace(
                        snippet,
                        $@"\b{System.Text.RegularExpressions.Regex.Escape(cleanQuery)}[a-zA-Z0-9]*\b",
                        "<mark>$&</mark>",
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase
                    );
                    
                case "fulltext" when query.Contains("&&") || query.Contains("||"):
                    // highlight each term
                    var highlighted = snippet;
                    var terms = cleanQuery.Split(new[] { "&&", "||" }, StringSplitOptions.RemoveEmptyEntries)
                                         .Select(t => t.Trim())
                                         .Where(t => !string.IsNullOrWhiteSpace(t));
                    
                    foreach (var term in terms)
                    {
                        highlighted = System.Text.RegularExpressions.Regex.Replace(
                            highlighted,
                            $@"\b{System.Text.RegularExpressions.Regex.Escape(term)}\b",
                            "<mark>$&</mark>",
                            System.Text.RegularExpressions.RegexOptions.IgnoreCase
                        );
                    }
                    return highlighted;
                    
                case "fulltext" when query.Contains(" "):
                    // highlight the exact phrase
                    return System.Text.RegularExpressions.Regex.Replace(
                        snippet,
                        System.Text.RegularExpressions.Regex.Escape(cleanQuery),
                        "<mark>$&</mark>",
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase
                    );
                    
                default:
                    // highlight exact matches only
                    return System.Text.RegularExpressions.Regex.Replace(
                        snippet,
                        $@"\b{System.Text.RegularExpressions.Regex.Escape(cleanQuery)}\b",
                        match => match.Value.Equals(cleanQuery, StringComparison.OrdinalIgnoreCase) ?
                               $"<mark>{match.Value}</mark>" : match.Value,
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase
                    );
            }
        }
    }
} 