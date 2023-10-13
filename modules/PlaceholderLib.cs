using BBRAPIModules;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace BattleBitAPI.Features
{
    [Module("A library for placeholders using {placeholder}. Supports color hexes color endings. ({#hex}, {/})", "1.1.0")]
    public class PlaceholderLib : BattleBitModule
    {

        private readonly Regex re = new Regex(@"\{([^\}]+)\}", RegexOptions.Compiled);

        public string text { get; set; }
        public Dictionary<string, object> parameters;

        public PlaceholderLib()
        {
            text = "";
            parameters = new();
        }

        public PlaceholderLib(string text)
        {
            this.text = text;
            parameters = new Dictionary<string, object>();
        }

        public PlaceholderLib(string text, params object[] values)
        {
            this.text = text;
            parameters = new();

            if (values.Length > 1)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    if ((i + 1) % 2 != 0)
                        continue;

                    string key = (string)values[i - 1];
                    object obj = values[i];

                    parameters.Add(key, obj);
                }
            }
        }

        public PlaceholderLib AddParam(string key, object value)
        {
            if (key == null || value == null)
            {
                return this;
            }

            parameters.Add(key, value);
            return this;
        }

        public string ReplaceColorCodes()
        {
            return re.Replace(text, delegate (Match match) {
                if (match.Groups[1].Value.StartsWith("#"))
                    return "<color=" + match.Groups[1].Value + ">";
                else if (match.Groups[1].Value.Equals("/"))
                    return "</color>";
                return $"{{{match.Groups[1].Value}}}";
            });
        }

        public string Run()
        {
            text = re.Replace(ReplaceColorCodes(), delegate (Match match) {
                if (parameters.ContainsKey(match.Groups[1].Value))
                    return parameters[match.Groups[1].Value].ToString();
                return text;
            });

            return ReplaceColorCodes();
        }
    }
}
