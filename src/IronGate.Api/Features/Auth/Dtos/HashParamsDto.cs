namespace IronGate.Api.Features.Auth.Dtos;

public sealed class HashParamsDto {
    /* SHA-256 parameters */
    public int? Iterations { get; set; }        

    /* Bcrypt parameters */
    public int? WorkFactor { get; set; }       

    /* Argon2 parameters */
    public int? MemoryKb { get; set; }
    public int? Parallelism { get; set; }
}
