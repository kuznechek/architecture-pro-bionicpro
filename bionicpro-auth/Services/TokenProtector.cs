using Microsoft.AspNetCore.DataProtection;

namespace BionicProAuth.Services;

public class TokenProtector
{
    private readonly IDataProtector _protector;

    public TokenProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("BionicPro.Auth.Tokens.v1");
    }

    public string Protect(string plainText) => _protector.Protect(plainText);
    public string Unprotect(string protectedText) => _protector.Unprotect(protectedText);
}
