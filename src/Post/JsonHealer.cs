using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

namespace AIInfluenceAntigravity.Post
{
    public static class JsonHealer
    {
        public static string Heal(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;

            string text = Clean(raw);
            int start = text.IndexOf('{');
            if (start < 0) return null;

            text = text.Substring(start);

            int? end = FindBalancedEnd(text);
            if (end != null)
            {
                text = StripInnerTrailingCommas(text.Substring(0, end.Value + 1));
            }
            else
            {
                text = Complete(text);
                if (text == null) return null;
            }

            return text.Length > 0 ? text : null;
        }

        public static bool TryParse(string raw, out JObject json)
        {
            json = null;
            string healed = Heal(raw);
            if (healed == null) return false;

            try
            {
                json = JObject.Parse(healed);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string Clean(string raw)
        {
            StringBuilder sb = new StringBuilder(raw.Length);
            foreach (char c in raw)
            {
                if (c == '﻿' || c < ' ') continue;
                sb.Append(c);
            }
            return sb.ToString();
        }

        private static int? FindBalancedEnd(string text)
        {
            bool inString = false;
            bool escaped = false;
            int depth = 0;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (inString)
                {
                    if (escaped) escaped = false;
                    else if (c == '\\') escaped = true;
                    else if (c == '"') inString = false;
                    continue;
                }

                if (c == '"') inString = true;
                else if (c == '{' || c == '[') depth++;
                else if (c == '}' || c == ']')
                {
                    depth--;
                    if (depth == 0) return i;
                    if (depth < 0) return null;
                }
            }

            return null;
        }

        private static string StripInnerTrailingCommas(string text)
        {
            StringBuilder sb = new StringBuilder(text.Length);
            bool inString = false;
            bool escaped = false;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (inString)
                {
                    sb.Append(c);
                    if (escaped) escaped = false;
                    else if (c == '\\') escaped = true;
                    else if (c == '"') inString = false;
                    continue;
                }

                if (c == '"')
                {
                    inString = true;
                    sb.Append(c);
                    continue;
                }

                if (c == ',')
                {
                    int j = i + 1;
                    while (j < text.Length && char.IsWhiteSpace(text[j])) j++;

                    if (j < text.Length && (text[j] == '}' || text[j] == ']')) continue;
                }

                sb.Append(c);
            }

            return sb.ToString();
        }

        private static string Complete(string text)
        {
            StringBuilder sb = new StringBuilder(text.Length + 8);
            Stack<char> open = new Stack<char>();
            bool inString = false;
            bool escaped = false;
            int stringStart = -1;
            bool stringIsKey = false;
            int danglingKeyStart = -1;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                sb.Append(c);

                if (inString)
                {
                    if (escaped) escaped = false;
                    else if (c == '\\') escaped = true;
                    else if (c == '"')
                    {
                        inString = false;
                        if (!stringIsKey) danglingKeyStart = -1;
                    }
                    continue;
                }

                if (c == '"')
                {
                    inString = true;
                    stringStart = sb.Length - 1;
                    stringIsKey = IsKeyPosition(sb);
                    if (stringIsKey) danglingKeyStart = stringStart;
                    continue;
                }

                if (c == '{' || c == '[')
                {
                    open.Push(c);
                    danglingKeyStart = -1;
                }
                else if (c == '}' || c == ']')
                {
                    if (open.Count == 0 || !Matches(open.Peek(), c)) return null;
                    open.Pop();
                    danglingKeyStart = -1;
                }
                else if (c == ',')
                {
                    danglingKeyStart = -1;
                }
                else if (!char.IsWhiteSpace(c) && c != ':')
                {
                    danglingKeyStart = -1;
                }
            }

            if (inString)
            {
                if (stringIsKey)
                {
                    sb.Length = stringStart;
                    TrimSeparators(sb);
                }
                else
                {
                    sb.Append('"');
                }
            }
            else if (danglingKeyStart >= 0)
            {
                sb.Length = danglingKeyStart;
                TrimSeparators(sb);
            }

            TrimSeparators(sb);

            while (open.Count > 0)
            {
                sb.Append(open.Pop() == '{' ? '}' : ']');
            }

            return sb.ToString();
        }

        private static bool IsKeyPosition(StringBuilder sb)
        {
            for (int i = sb.Length - 2; i >= 0; i--)
            {
                char c = sb[i];
                if (c == ' ' || c == '\t') continue;
                return c == '{' || c == ',' || c == '[';
            }
            return true;
        }

        private static void TrimSeparators(StringBuilder sb)
        {
            while (sb.Length > 0)
            {
                char c = sb[sb.Length - 1];
                if (c == ',' || c == ':' || c == ' ' || c == '\t') sb.Length--;
                else break;
            }
        }

        private static bool Matches(char open, char close)
        {
            return (open == '{' && close == '}') || (open == '[' && close == ']');
        }
    }
}
