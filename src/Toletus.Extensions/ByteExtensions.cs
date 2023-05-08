using System.Linq;

namespace Toletus.Extensions
{
    public static class ByteExtensions
    {
        public static byte[] SupressEndWithZeroBytes(this byte[] input)
        {
            int i;
            for (i = input.Length - 1; i >= 0; i--)
                if (input[i] != 0)
                    break;

            return input.Take(i + 1).ToArray();
        }

        public static string ConvertToAsciiString(this byte[] input)
        {
            var result = string.Join("", input
                .Select(b => b >= 32 && b <= 254 ?
                    ((char)b).ToString() : $@"\{b:x}"));

            return result;
        }
    }
}