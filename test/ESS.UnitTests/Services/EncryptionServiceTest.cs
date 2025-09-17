using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ESS.Api.Services.Common;
using ESS.Api.Setup;
using Microsoft.Extensions.Options;

namespace ESS.UnitTests.Services;
public sealed class EncryptionServiceTest
{
    private readonly EncryptionService _encryprtionService;
    public EncryptionServiceTest()
    {
        IOptions<EncryptionOptions> options = Options.Create(new EncryptionOptions
        {
            Key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
        });

        _encryprtionService = new EncryptionService(options);
    }

    [Fact]
    public void Decrypt_ShouldReturnPlainText_WhenDecryptingCorrectCiphertext()
    {
        //Arrange
        const string plainText = "sensitive data";
        string ciphertext = _encryprtionService.Encrypt(plainText);

        //Act
        string decryptedCiphertext = _encryprtionService.Decrypt(ciphertext);

        //Assert
        Assert.Equal(plainText, decryptedCiphertext);
    }
}
