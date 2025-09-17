using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using ESS.Api.Database.Entities.Settings;
using ESS.Api.DTOs.Settings;
using FluentValidation.Results;

namespace ESS.UnitTests.Validators;

public sealed class CreateAppSettingsValidatorTests
{
    private readonly CreateAppSettingsDtoValidator _validator = new();

    [Fact]
    public async Task Validate_ShouldSucceed_WhenDtoIsValid()
    {
        //Arrange
        var dto = new CreateAppSettingsDto
        {
            Key = AppSettingsKey.PaymentReportImageFolderPath,
            Value = "/app/report/payment",
            Type = AppSettingsType.General,
            Description = "Tests"

        };

        //Act
        ValidationResult validationResult = await _validator.ValidateAsync(dto);

        //Assert
        Assert.True(validationResult.IsValid);
        Assert.Empty(validationResult.Errors);
    }
    [Fact]
    public async Task Validate_ShouldFail_WhenDtoKeyIsInValid()
    {
        //Arrange
        var dto = new CreateAppSettingsDto
        {
            Key = "Some Key",
            Value = "/app/report/payment",
            Type = AppSettingsType.General,
            Description = "Tests"

        };

        //Act
        ValidationResult validationResult = await _validator.ValidateAsync(dto);

        //Assert
        Assert.False(validationResult.IsValid);
        ValidationFailure validationFailure = Assert.Single(validationResult.Errors);
        Assert.Equal(nameof(CreateAppSettingsDto.Key), validationFailure.PropertyName);
    }
}
