using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace RLMovie.Editor
{
    internal sealed class VendoredYamlParseException : Exception
    {
        public VendoredYamlParseException(string message)
            : base(message)
        {
        }
    }

    /// <summary>
    /// Lightweight repo-local YAML reader for the subset used by scenario manifests and blueprints.
    /// Supports mappings, sequences, quoted strings, booleans, integers, floats, and comments.
    /// </summary>
    internal static class VendoredYamlParser
    {
        public static object Parse(string contents)
        {
            if (string.IsNullOrWhiteSpace(contents))
            {
                throw new VendoredYamlParseException("Document is empty.");
            }

            string normalized = contents.Replace("\r\n", "\n");
            if (normalized.TrimStart().StartsWith("{", StringComparison.Ordinal))
            {
                throw new VendoredYamlParseException("JSON-compatible YAML should be handled by the legacy JSON fallback.");
            }

            List<YamlLine> lines = Tokenize(normalized);
            if (lines.Count == 0)
            {
                throw new VendoredYamlParseException("Document does not contain any YAML nodes.");
            }

            int index = 0;
            object value = ParseBlock(lines, ref index, lines[0].Indent);
            if (index < lines.Count)
            {
                throw new VendoredYamlParseException($"Unexpected trailing content near line {lines[index].LineNumber}.");
            }

            return value;
        }

        private static object ParseBlock(IReadOnlyList<YamlLine> lines, ref int index, int indent)
        {
            if (index >= lines.Count)
            {
                throw new VendoredYamlParseException("Unexpected end of document.");
            }

            if (lines[index].Indent < indent)
            {
                throw new VendoredYamlParseException($"Unexpected indentation near line {lines[index].LineNumber}.");
            }

            return lines[index].Content.StartsWith("-", StringComparison.Ordinal)
                ? ParseSequence(lines, ref index, indent)
                : ParseMapping(lines, ref index, indent);
        }

        private static Dictionary<string, object> ParseMapping(IReadOnlyList<YamlLine> lines, ref int index, int indent)
        {
            var map = new Dictionary<string, object>(StringComparer.Ordinal);

            while (index < lines.Count)
            {
                YamlLine line = lines[index];
                if (line.Indent < indent)
                {
                    break;
                }

                if (line.Indent > indent)
                {
                    throw new VendoredYamlParseException($"Unexpected indentation near line {line.LineNumber}.");
                }

                if (line.Content.StartsWith("-", StringComparison.Ordinal))
                {
                    break;
                }

                ParseKeyValue(line.Content, line.LineNumber, out string key, out string valueText, out bool hasInlineValue);
                index++;

                object value;
                if (hasInlineValue)
                {
                    value = ParseScalar(valueText);
                }
                else if (index < lines.Count && lines[index].Indent > indent)
                {
                    value = ParseBlock(lines, ref index, lines[index].Indent);
                }
                else
                {
                    value = new Dictionary<string, object>(StringComparer.Ordinal);
                }

                map[key] = value;
            }

            return map;
        }

        private static List<object> ParseSequence(IReadOnlyList<YamlLine> lines, ref int index, int indent)
        {
            var list = new List<object>();

            while (index < lines.Count)
            {
                YamlLine line = lines[index];
                if (line.Indent < indent)
                {
                    break;
                }

                if (line.Indent > indent)
                {
                    throw new VendoredYamlParseException($"Unexpected indentation near line {line.LineNumber}.");
                }

                if (!line.Content.StartsWith("-", StringComparison.Ordinal))
                {
                    break;
                }

                string itemText = line.Content.Length == 1 ? string.Empty : line.Content.Substring(1).TrimStart();
                index++;

                if (string.IsNullOrWhiteSpace(itemText))
                {
                    if (index >= lines.Count || lines[index].Indent <= indent)
                    {
                        list.Add(string.Empty);
                        continue;
                    }

                    list.Add(ParseBlock(lines, ref index, lines[index].Indent));
                    continue;
                }

                if (LooksLikeInlineMapping(itemText))
                {
                    ParseKeyValue(itemText, line.LineNumber, out string key, out string valueText, out bool hasInlineValue);
                    var itemMap = new Dictionary<string, object>(StringComparer.Ordinal);
                    itemMap[key] = hasInlineValue ? ParseScalar(valueText) : new Dictionary<string, object>(StringComparer.Ordinal);

                    if (index < lines.Count && lines[index].Indent > indent)
                    {
                        Dictionary<string, object> nestedMap = ParseMapping(lines, ref index, lines[index].Indent);
                        foreach (KeyValuePair<string, object> entry in nestedMap)
                        {
                            itemMap[entry.Key] = entry.Value;
                        }
                    }

                    list.Add(itemMap);
                    continue;
                }

                list.Add(ParseScalar(itemText));
            }

            return list;
        }

        private static void ParseKeyValue(string content, int lineNumber, out string key, out string valueText, out bool hasInlineValue)
        {
            int separatorIndex = FindMappingSeparator(content);
            if (separatorIndex <= 0)
            {
                throw new VendoredYamlParseException($"Expected a key/value pair near line {lineNumber}.");
            }

            key = content.Substring(0, separatorIndex).Trim();
            valueText = content.Substring(separatorIndex + 1).Trim();
            hasInlineValue = valueText.Length > 0;

            if (string.IsNullOrWhiteSpace(key))
            {
                throw new VendoredYamlParseException($"Encountered an empty mapping key near line {lineNumber}.");
            }
        }

        private static int FindMappingSeparator(string content)
        {
            bool inSingleQuote = false;
            bool inDoubleQuote = false;

            for (int i = 0; i < content.Length; i++)
            {
                char current = content[i];
                switch (current)
                {
                    case '\'':
                        if (!inDoubleQuote)
                        {
                            inSingleQuote = !inSingleQuote;
                        }

                        break;

                    case '"':
                        if (!inSingleQuote)
                        {
                            inDoubleQuote = !inDoubleQuote;
                        }

                        break;

                    case ':':
                        if (!inSingleQuote && !inDoubleQuote)
                        {
                            return i;
                        }

                        break;
                }
            }

            return -1;
        }

        private static bool LooksLikeInlineMapping(string content)
        {
            return FindMappingSeparator(content) > 0;
        }

        private static object ParseScalar(string valueText)
        {
            string trimmed = valueText.Trim();
            if (trimmed.Length == 0)
            {
                return string.Empty;
            }

            if ((trimmed.StartsWith("\"", StringComparison.Ordinal) && trimmed.EndsWith("\"", StringComparison.Ordinal))
                || (trimmed.StartsWith("'", StringComparison.Ordinal) && trimmed.EndsWith("'", StringComparison.Ordinal)))
            {
                return Unquote(trimmed);
            }

            if (string.Equals(trimmed, "true", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(trimmed, "false", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (string.Equals(trimmed, "null", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intValue))
            {
                return intValue;
            }

            if (double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out double doubleValue))
            {
                return doubleValue;
            }

            return trimmed;
        }

        private static string Unquote(string value)
        {
            if (value.Length < 2)
            {
                return value;
            }

            char quote = value[0];
            string inner = value.Substring(1, value.Length - 2);
            if (quote == '\'')
            {
                return inner.Replace("''", "'");
            }

            var builder = new StringBuilder(inner.Length);
            bool escaping = false;
            for (int i = 0; i < inner.Length; i++)
            {
                char current = inner[i];
                if (!escaping && current == '\\')
                {
                    escaping = true;
                    continue;
                }

                if (escaping)
                {
                    builder.Append(current switch
                    {
                        'n' => '\n',
                        'r' => '\r',
                        't' => '\t',
                        '"' => '"',
                        '\\' => '\\',
                        _ => current
                    });
                    escaping = false;
                    continue;
                }

                builder.Append(current);
            }

            if (escaping)
            {
                builder.Append('\\');
            }

            return builder.ToString();
        }

        private static List<YamlLine> Tokenize(string contents)
        {
            string[] rawLines = contents.Split('\n');
            var lines = new List<YamlLine>();

            for (int i = 0; i < rawLines.Length; i++)
            {
                string rawLine = rawLines[i].TrimEnd();
                if (i == 0)
                {
                    rawLine = rawLine.TrimStart('\uFEFF');
                }

                string stripped = StripComments(rawLine);
                if (string.IsNullOrWhiteSpace(stripped))
                {
                    continue;
                }

                int indent = rawLine.TakeWhile(character => character == ' ').Count();
                if (indent % 2 != 0)
                {
                    throw new VendoredYamlParseException($"Only 2-space indentation is supported. Problem near line {i + 1}.");
                }

                lines.Add(new YamlLine(i + 1, indent, stripped.Trim()));
            }

            return lines;
        }

        private static string StripComments(string line)
        {
            bool inSingleQuote = false;
            bool inDoubleQuote = false;

            for (int i = 0; i < line.Length; i++)
            {
                char current = line[i];
                switch (current)
                {
                    case '\'':
                        if (!inDoubleQuote)
                        {
                            inSingleQuote = !inSingleQuote;
                        }

                        break;

                    case '"':
                        if (!inSingleQuote)
                        {
                            inDoubleQuote = !inDoubleQuote;
                        }

                        break;

                    case '#':
                        if (!inSingleQuote && !inDoubleQuote)
                        {
                            return line.Substring(0, i);
                        }

                        break;
                }
            }

            return line;
        }

        private readonly struct YamlLine
        {
            public YamlLine(int lineNumber, int indent, string content)
            {
                LineNumber = lineNumber;
                Indent = indent;
                Content = content;
            }

            public int LineNumber { get; }

            public int Indent { get; }

            public string Content { get; }
        }
    }
}
