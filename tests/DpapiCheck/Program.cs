using System.Text;
using Tunnelka.Storage;

var original = Encoding.UTF8.GetBytes("{\"Servers\":[{\"Link\":\"vless://секрет\"}]}");
var encrypted = Dpapi.Protect(original);
var decrypted = Dpapi.Unprotect(encrypted);

if (!OperatingSystem.IsWindows() || encrypted.SequenceEqual(original) || !decrypted.SequenceEqual(original))
{
    Console.WriteLine("::error::DPAPI round trip failed");
    return 1;
}

Console.WriteLine($"::notice title=DPAPI::round trip ok, {original.Length} bytes -> {encrypted.Length} bytes encrypted");
return 0;
