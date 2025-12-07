namespace IronGate.Core.Security.TotpValidator;

public interface ITotpValidator {
    bool ValidateCode(string secret, string code);
}