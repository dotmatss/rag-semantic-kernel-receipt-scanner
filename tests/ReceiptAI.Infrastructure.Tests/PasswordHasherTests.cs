namespace ReceiptAI.Infrastructure.Tests;

public class PasswordHasherTests
{
    private readonly BcryptPasswordHasher _hasher = new();

    [Fact]
    public void Verify_accepts_only_the_original_password()
    {
        var hash = _hasher.HashPassword("correct-horse");

        Assert.True(_hasher.VerifyPassword("correct-horse", hash));
        Assert.False(_hasher.VerifyPassword("wrong-password", hash));
        Assert.False(_hasher.VerifyPassword("correct-horse", "garbage"));
    }

    [Fact]
    public void Hash_uses_a_random_salt()
    {
        Assert.NotEqual(_hasher.HashPassword("correct-horse"), _hasher.HashPassword("correct-horse"));
    }
}
