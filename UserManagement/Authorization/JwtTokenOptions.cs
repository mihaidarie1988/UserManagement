namespace DocumentManagement.Authorization;

public record JwtTokenOptions(string Issuer, string Audience, string SigningKey);
