using System.Security.Cryptography;
using System.Text;
namespace MamiMotovlog.Services;
public static class AdminSessionStamp
{
    public static string FromHash(string passwordHash) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(passwordHash)));
}
