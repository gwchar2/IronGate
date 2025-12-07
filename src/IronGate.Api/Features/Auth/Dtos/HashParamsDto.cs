namespace IronGate.Api.Features.Auth.Dtos;


/*
 * HashParamsDto encapsulates the parameters used for different password hashing algorithms.
 * It includes settings for SHA-256, Bcrypt, and Argon2 hashing methods.
 */
public sealed class HashParamsDto {
    /* SHA-256 parameters */
    public int? Iterations { get; set; }        

    /* Bcrypt parameters */
    public int? WorkFactor { get; set; }       

    /* Argon2 parameters */
    public int? MemoryKb { get; set; }
    public int? Parallelism { get; set; }
}
