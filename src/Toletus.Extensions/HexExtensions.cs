using System;
using System.Text.RegularExpressions;

namespace Toletus.Extensions
{
    public static class HexExtensions
    {
        public static byte[] ToByteArray(this string hexString)
        {
            var noBlanks = Regex.Replace(hexString, @"\s+", "");
            var outputLength = noBlanks.Length / 2;
            var output = new byte[outputLength];
            var numeral = new char[2];
            using (var sr = new System.IO.StringReader(noBlanks))
            {
                for (var i = 0; i < outputLength; i++)
                {
                    numeral[0] = (char)sr.Read();
                    numeral[1] = (char)sr.Read();
                    output[i] = Convert.ToByte(new string(numeral), 16);
                }
            }

            return output;
        }

        public static string[] ToHexStringArray(this byte[] byteArray)
        {
            if (byteArray == null) return null;

            var hexArray = new string[byteArray.Length];

            for (var i = 0; i < byteArray.Length; i++)
                hexArray[i] = byteArray[i].ToString("x2");

            return hexArray;
        }

        public static string ToHexString(this byte[] byteArray, string separator = null)
        {
            return string.Join(separator ?? string.Empty, byteArray.ToHexStringArray());
        }
    }
}