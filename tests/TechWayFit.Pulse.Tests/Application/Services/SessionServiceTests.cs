using FluentAssertions;
using Moq;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Application.Services;
using TechWayFit.Pulse.Domain.Entities;
using TechWayFit.Pulse.Domain.Enums;
using TechWayFit.Pulse.Domain.ValueObjects;
using Xunit;

namespace TechWayFit.Pulse.Tests.Application.Services;

public class SessionServiceTests
{
    private readonly Mock<ISessionRepository> _sessionRepositoryMock;
    private readonly SessionService _sessionService;

    public SessionServiceTests()
    {
        _sessionRepositoryMock = new Mock<ISessionRepository>();
      _sessionService = new SessionService(_sessionRepositoryMock.Object);
    }

    [Fact]
    public async Task CreateSessionAsync_Should_Create_Session_Successfully()
    {
        // Arrange
        var code = "TEST-2024";
        var title = "Test Session";
        var goal = "Test Goal";
var context = "Test Context";
var settings = new SessionSettings(5, null, true, true, 360);
        var joinFormSchema = new JoinFormSchema(5, new List<JoinFormField>());
      var now = DateTimeOffset.UtcNow;
        var facilitatorUserId = Guid.NewGuid();

        _sessionRepositoryMock
            .Setup(x => x.GetByCodeAsync(code, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Session?)null);

      _sessionRepositoryMock
 .Setup(x => x.AddAsync(It.IsAny<Session>(), It.IsAny<CancellationToken>()))
    .Returns(Task.CompletedTask);

     // Act
        var result = await _sessionService.CreateSessionAsync(
            code, title, goal, context, settings, joinFormSchema, now, facilitatorUserId);

     // Assert
        result.Should().NotBeNull();
        result.Code.Should().Be(code);
  result.Title.Should().Be(title);
        result.Goal.Should().Be(goal);
        result.Context.Should().Be(context);
        result.Settings.Should().Be(settings);
        result.JoinFormSchema.Should().Be(joinFormSchema);
        result.Status.Should().Be(SessionStatus.Draft);
        result.CreatedAt.Should().Be(now);
        result.FacilitatorUserId.Should().Be(facilitatorUserId);
    result.ExpiresAt.Should().Be(now.AddMinutes(360));

  _sessionRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Session>(), It.IsAny<CancellationToken>()),
        Times.Once);
    }

[Fact]
    public async Task CreateSessionAsync_Should_Throw_When_Code_Already_Exists()
    {
        // Arrange
        var code = "EXISTING-CODE";
        var title = "Test Session";
    var settings = new SessionSettings(5, null, true, true, 360);
        var joinFormSchema = new JoinFormSchema(5, new List<JoinFormField>());
        var existingSession = CreateTestSession(code);

        _sessionRepositoryMock
            .Setup(x => x.GetByCodeAsync(code, It.IsAny<CancellationToken>()))
.ReturnsAsync(existingSession);

        // Act & Assert
        var act = async () => await _sessionService.CreateSessionAsync(
    code, title, null, null, settings, joinFormSchema, DateTimeOffset.UtcNow);

        await act.Should().ThrowAsync<InvalidOperationException>()
      .WithMessage("Session code already exists.");
    }

    [Theory]
    [InlineData("", "Valid Title")]
    [InlineData("   ", "Valid Title")]
    [InlineData(null, "Valid Title")]
    [InlineData("VALID-CODE", "")]
    [InlineData("VALID-CODE", "   ")]
    [InlineData("VALID-CODE", null)]
    public async Task CreateSessionAsync_Should_Throw_When_Required_Fields_Invalid(
     string code, string title)
    {
        // Arrange
        var settings = new SessionSettings(5, null, true, true, 360);
      var joinFormSchema = new JoinFormSchema(5, new List<JoinFormField>());

   // Act & Assert
        var act = async () => await _sessionService.CreateSessionAsync(
 code, title, null, null, settings, joinFormSchema, DateTimeOffset.UtcNow);

 await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateSessionAsync_Should_Throw_When_Code_Too_Long()
    {
        // Arrange
        var longCode = new string('A', 33); // 33 characters, max is 32
var title = "Valid Title";
     var settings = new SessionSettings(5, null, true, true, 360);
        var joinFormSchema = new JoinFormSchema(5, new List<JoinFormField>());

        // Act & Assert
   var act = async () => await _sessionService.CreateSessionAsync(
            longCode, title, null, null, settings, joinFormSchema, DateTimeOffset.UtcNow);

     await act.Should().ThrowAsync<ArgumentException>()
     .WithMessage("*Session code must be <= 32 characters*");
    }

    [Fact]
    public async Task CreateSessionAsync_Should_Throw_When_Title_Too_Long()
    {
 // Arrange
        var code = "VALID-CODE";
        var longTitle = new string('A', 201); // 201 characters, max is 200
        var settings = new SessionSettings(5, null, true, true, 360);
        var joinFormSchema = new JoinFormSchema(5, new List<JoinFormField>());

        // Act & Assert
 var act = async () => await _sessionService.CreateSessionAsync(
      code, longTitle, null, null, settings, joinFormSchema, DateTimeOffset.UtcNow);

     await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Session title must be <= 200 characters*");
    }

    [Fact]
  public async Task SetStatusAsync_Should_Update_Status_Successfully()
    {
   // Arrange
        var sessionId = Guid.NewGuid();
        var session = CreateTestSession("TEST-CODE", sessionId);
        var newStatus = SessionStatus.Live;
        var now = DateTimeOffset.UtcNow;

      _sessionRepositoryMock
            .Setup(x => x.GetByIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        _sessionRepositoryMock
   .Setup(x => x.UpdateAsync(It.IsAny<Session>(), It.IsAny<CancellationToken>()))
    .Returns(Task.CompletedTask);

        // Act
        await _sessionService.SetStatusAsync(sessionId, newStatus, now);

        // Assert
        session.Status.Should().Be(newStatus);
        session.UpdatedAt.Should().Be(now);

        _sessionRepositoryMock.Verify(
            x => x.UpdateAsync(session, It.IsAny<CancellationToken>()),
       Times.Once);
    }

    [Fact]
 public async Task SetStatusAsync_Should_Throw_When_Session_Not_Found()
 {
      // Arrange
        var sessionId = Guid.NewGuid();

        _sessionRepositoryMock
     .Setup(x => x.GetByIdAsync(sessionId, It.IsAny<CancellationToken>()))
     .ReturnsAsync((Session?)null);

        // Act & Assert
     var act = async () => await _sessionService.SetStatusAsync(
            sessionId, SessionStatus.Live, DateTimeOffset.UtcNow);

        await act.Should().ThrowAsync<InvalidOperationException>()
       .WithMessage("Session not found.");
  }

    [Fact]
    public async Task UpdateJoinFormSchemaAsync_Should_Update_Schema_Successfully()
    {
    // Arrange
        var sessionId = Guid.NewGuid();
        var session = CreateTestSession("TEST-CODE", sessionId);
        var newSchema = new JoinFormSchema(5, new List<JoinFormField>
        {
     new JoinFormField("department", "Department", FieldType.Text, true, null, true)
        });
        var now = DateTimeOffset.UtcNow;

        _sessionRepositoryMock
       .Setup(x => x.GetByIdAsync(sessionId, It.IsAny<CancellationToken>()))
       .ReturnsAsync(session);

        _sessionRepositoryMock
          .Setup(x => x.UpdateAsync(It.IsAny<Session>(), It.IsAny<CancellationToken>()))
     .Returns(Task.CompletedTask);

        // Act
        var result = await _sessionService.UpdateJoinFormSchemaAsync(sessionId, newSchema, now);

        // Assert
    result.Should().Be(session);
   session.JoinFormSchema.Should().Be(newSchema);
   session.UpdatedAt.Should().Be(now);

   _sessionRepositoryMock.Verify(
     x => x.UpdateAsync(session, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static Session CreateTestSession(string code = "TEST-2024", Guid? id = null)
    {
        return new Session(
          id ?? Guid.NewGuid(),
            code,
        "Test Session",
    "Test Goal",
            "Test Context",
     new SessionSettings(5, null, true, true, 360),
  new JoinFormSchema(5, new List<JoinFormField>()),
   SessionStatus.Draft,
        null,
         DateTimeOffset.UtcNow,
     DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddMinutes(360));
    }
}
