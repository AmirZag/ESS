namespace ESS.Api.DTOs.Reports;

public sealed record PersonnelFileReportQuery
{
    public required int Year { get; init; }
}
