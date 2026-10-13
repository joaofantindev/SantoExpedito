namespace SantoExpedito.Tests;

public class CpfEncryptionServiceTests
{
    [Fact]
    public void EncryptDecrypt_RoundTrip()
    {
        var service = TestHelpers.CreateCpfService();
        const string cpf = "123.456.789-00";

        var encrypted = service.Encrypt(cpf);
        var decrypted = service.Decrypt(encrypted);

        Assert.NotEqual(cpf, encrypted);
        Assert.Equal(cpf, decrypted);
    }

    [Fact]
    public void Encrypt_Vazio_RetornaMesmoValor()
    {
        var service = TestHelpers.CreateCpfService();
        Assert.Equal("", service.Encrypt(""));
    }

    [Fact]
    public void Decrypt_ValorNaoCifrado_RetornaOriginal()
    {
        var service = TestHelpers.CreateCpfService();
        Assert.Equal("abc", service.Decrypt("abc"));
    }
}
