using System.Collections.Generic;

namespace WpfPdfReader
{
    public class Finding
    {
        public string Title { get; set; }
        public Dictionary<string, string> Details { get; set; } = new Dictionary<string, string>();

        public void AddDetail(string key, string value)
        {
            Details[key] = value;
        }

        public void AppendToDetail(string key, string text)
        {
            if (Details.ContainsKey(key))
            {
                Details[key] += Details[key].Length > 0 ? " " + text : text;
            }
            else
            {
                Details[key] = text;
            }
        }
    }
}
