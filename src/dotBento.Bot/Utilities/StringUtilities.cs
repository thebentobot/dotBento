using System.Security.Cryptography;
using System.Text;

namespace dotBento.Bot.Utilities;

public static class StringUtilities
{
    public static string GenerateRandomCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ123456789";
        const int size = 8;

        var data = new byte[4 * size];
        using (var crypto = RandomNumberGenerator.Create())
        {
            crypto.GetBytes(data);
        }
        var result = new StringBuilder(size);
        for (var i = 0; i < size; i++)
        {
            var rnd = Math.Abs(BitConverter.ToInt32(data, i * 4));
            var idx = rnd % chars.Length;

            result.Append(chars[idx]);
        }

        return result.ToString();
    }
}