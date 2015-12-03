using System.Collections.Generic;

namespace Ably.Utils
{
    public class NameValueCollection
    {
        public NameValueCollection()
        {
            _dict = new Dictionary<string, object>();
        }

        public NameValueCollection(int capacity)
        {
            _dict = new Dictionary<string, object>(capacity);
        }

        private Dictionary<string, object> _dict;

        public IEnumerable<string> AllKeys
        {
            get
            {
                return _dict.Keys;
            }
        }

        public string this[string name]
        {
            get
            {
                return Get(name);
            }
            set
            {
                Add(name, value);
            }
        }

        public void Add(string name, string value)
        {
            object storedValue;
            if (!_dict.TryGetValue(name, out storedValue))
            {
                _dict.Add(name, value);
            }
            else
            {
                if (storedValue is string)
                {
                    _dict[name] = new List<string>() { (string)storedValue, value };
                }
                else
                {
                    (storedValue as List<string>).Add(value);
                }
            }
        }

        public string Get(string name)
        {
            return GetAsOneString(_dict[name]);
        }

        public string[] GetValues(string name)
        {
            return GetAtStringArray(name);
        }

        private static string GetAsOneString(object item)
        {
            string itemAsString = item as string;
            if (itemAsString != null)
                return itemAsString;

            return string.Join(",", item as List<string>);
        }

        private static string[] GetAtStringArray(object item)
        {
            string itemAsString = item as string;
            if (itemAsString != null)
                return new string[] { itemAsString };

            return (item as List<string>).ToArray();
        }
    }
}
