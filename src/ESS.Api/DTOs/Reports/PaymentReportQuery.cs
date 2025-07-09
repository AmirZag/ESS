namespace ESS.Api.DTOs.Reports;

public sealed record PaymentReportQuery
{
    public required int Year { get; init; }
    public required int Month { get; init; }
    public required int Level { get; init; }
}
