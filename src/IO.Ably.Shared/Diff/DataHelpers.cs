using System;
using System.Text;

namespace IO.Ably.Diff
{
    internal static class DataHelpers
    {
        public static byte[] ConvertToByteArray(object data)
        {
            if (data is byte[])
            {
                return data as byte[];
            }
            else if (data is string)
            {
                string dataAsString = data as string;
                return TryConvertFromBase64String(dataAsString, out byte[] result) ? result : Encoding.UTF8.GetBytes(dataAsString);
            }
            else
            {
                throw new ArgumentException();
            }
        }

        public static bool TryConvertToDeltaByteArray(object obj, out byte[] delta)
        {
            byte[] dataAsByteArray = obj as byte[];
            string dataAsString = obj as string;
            if (dataAsByteArray != null || (dataAsString != null && TryConvertFromBase64String(dataAsString, out dataAsByteArray)))
            {
                delta = dataAsByteArray;
                return true;
            }

            delta = null;
            return false;
        }

        public static bool TryConvertFromBase64String(string str, out byte[] result)
        {
            result = null;
            try
            {
                result = Convert.FromBase64String(str);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
