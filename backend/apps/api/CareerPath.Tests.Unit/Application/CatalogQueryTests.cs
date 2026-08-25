using CareerPath.Application.Catalog;
using CareerPath.Application.Abstractions.Repositories;
using CareerPath.Contracts.V1.Catalog;
using CareerPath.Contracts.V1.Common;
using NSubstitute;
using Xunit;

namespace CareerPath.Tests.Unit.Application;

public sealed class GetExamsHandlerTests
{
    private readonly ICatalogRepository _repo = Substitute.For<ICatalogRepository>();

    [Fact]
    public async Task Returns_Paged_Exams()
    {
        var exams = new List<ExamDto>
        {
            new(1, "jee-main", "JEE Main", "JEE Main Full", "NTA", "National", "Bi-Annual", null, null),
            new(2, "neet-ug",  "NEET UG",  "NEET UG Full",  "NTA", "National", "Annual",    null, null),
        };
        _repo.GetExamsAsync("en", null, null, 1, 20, default).Returns((exams, 2));

        var handler = new GetExamsHandler(_repo);
        var result  = await handler.Handle(new GetExamsQuery(null, null, 1, 20, "en"), default);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.False(result.HasNextPage);
    }

    [Fact]
    public async Task Filter_By_Level_Passed_Through()
    {
        _repo.GetExamsAsync("en", "National", null, 1, 20, default).Returns((new List<ExamDto>(), 0));

        var handler = new GetExamsHandler(_repo);
        await handler.Handle(new GetExamsQuery("National", null, 1, 20, "en"), default);

        await _repo.Received(1).GetExamsAsync("en", "National", null, 1, 20, default);
    }
}

public sealed class GetCoursesHandlerTests
{
    private readonly ICatalogRepository _repo = Substitute.For<ICatalogRepository>();

    [Fact]
    public async Task Returns_Paged_Courses()
    {
        var courses = new List<CourseDto>
        {
            new(1, "btech-cs", "B.Tech CS", "B.Tech CS", "Undergraduate", 4.0m, "engineering", null),
        };
        _repo.GetCoursesAsync("en", null, null, null, 1, 20, default).Returns((courses, 1));

        var handler = new GetCoursesHandler(_repo);
        var result  = await handler.Handle(new GetCoursesQuery(null, null, null, 1, 20, "en"), default);

        Assert.Single(result.Items);
        Assert.Equal(1, result.TotalCount);
    }
}

public sealed class GetScholarshipsHandlerTests
{
    private readonly ICatalogRepository _repo = Substitute.For<ICatalogRepository>();

    [Fact]
    public async Task Returns_Paged_Scholarships()
    {
        var scholarships = new List<ScholarshipDto>
        {
            new(1, "nsp-central", "NSP", "Ministry of Education", "All", "₹50,000/year", null, null, null),
        };
        _repo.GetScholarshipsAsync("en", null, null, 1, 20, default).Returns((scholarships, 1));

        var handler = new GetScholarshipsHandler(_repo);
        var result  = await handler.Handle(new GetScholarshipsQuery(null, null, 1, 20, "en"), default);

        Assert.Single(result.Items);
        Assert.False(result.HasNextPage);
    }
}
