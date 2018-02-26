using System;
using System.Collections.Generic;
using System.Linq;

namespace IO.Ably
{
    using kvp = KeyValuePair<string, string>;

    public sealed class HttpUtility
    {
        public static HttpValueCollection ParseQueryString(string query)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            if ((query.Length > 0) && (query[0] == '?'))
            {
                query = query.Substring(1);
            }

            return new HttpValueCollection(query, true);
        }
    }

    public class HttpValueCollection
    {
        public IEnumerable<string> AllKeys
        {
            get { return m_data.Select(i => i.Key); }
        }

        private readonly List<kvp> m_data = new List<kvp>();

        public string this[string name]
        {
            get
            {
                string[] items = GetValues(name);
                if (items == null)
                {
                    return null;
                }

                return String.Join(",", items);
            }

            set
            {
                // If the specified key already exists in the collection, setting this property overwrites the existing list of values with the specified value.
                m_data.RemoveAll(i => i.Key == name);
                m_data.Add(new kvp(name, value));
            }
        }

        public string[] GetValues(string name)
        {
            string[] res = m_data.Where(i => i.Key==name).Select(i => i.Value).ToArray();
            if (res.Length <= 0)
            {
                return null;
            }

            return res;
        }

        public void Add(string name, string value)
        {
            m_data.Add(new kvp(name, value));
        }

        public HttpValueCollection() { }

        public HttpValueCollection(string query)
            : this(query, true) { }

        public HttpValueCollection(string query, bool urlencoded)
        {
            if (!string.IsNullOrEmpty(query))
            {
                this.FillFromString(query, urlencoded);
            }
        }

        private void FillFromString(string query, bool urlencoded)
        {
            // http://stackoverflow.com/a/20284635/126995
            int num = (query != null) ? query.Length : 0;
            for (int i = 0; i < num; i++)
            {
                int startIndex = i;
                int num4 = -1;
                while (i < num)
                {
                    char ch = query[i];
                    if (ch == '=')
                    {
                        if (num4 < 0)
                        {
                            num4 = i;
                        }
                    }
                    else if (ch == '&')
                    {
                        break;
                    }

                    i++;
                }

                string str = null;
                string str2 = null;
                if (num4 >= 0)
                {
                    str = query.Substring(startIndex, num4 - startIndex);
                    str2 = query.Substring(num4 + 1, (i - num4) - 1);
                }
                else
                {
                    str2 = query.Substring(startIndex, i - startIndex);
                }

                if (urlencoded)
                {
                    this.Add(Uri.UnescapeDataString(str), Uri.UnescapeDataString(str2));
                }
                else
                {
                    this.Add(str, str2);
                }

                if ((i == (num - 1)) && (query[i] == '&'))
                {
                    this.Add(null, string.Empty);
                }
            }
        }
    }
}