namespace IronGate.Api.Features.Auth.TotpValidator;

public interface ITotpValidator {
    bool ValidateCode(string secret, string code);
}