using FluentAssertions;
using Moq;
using TechWayFit.Pulse.Application.Abstractions.Repositories;
using TechWayFit.Pulse.Application.Services;
using TechWayFit.Pulse.Domain.Entities;
using Xunit;

namespace TechWayFit.Pulse.Tests.Application.Services;

public class SessionGroupServiceTests
{
    private readonly Mock<ISessionGroupRepository> _groups;
    private readonly SessionGroupService _service;

    public SessionGroupServiceTests()
    {
        _groups = new Mock<ISessionGroupRepository>();
        _service = new SessionGroupService(_groups.Object);
    }

    [Fact]
    public async Task CreateGroupAsync_Should_Create_Level1_Group()
    {
        var now = DateTimeOffset.UtcNow;
        var facilitatorId = Guid.NewGuid();
        SessionGroup? createdGroup = null;

        _groups
            .Setup(x => x.CreateAsync(It.IsAny<SessionGroup>(), It.IsAny<CancellationToken>()))
            .Callback<SessionGroup, CancellationToken>((group, _) => createdGroup = group)
            .ReturnsAsync((SessionGroup group, CancellationToken _) => group);

        var result = await _service.CreateGroupAsync(
            "  Team A  ",
            "  Description  ",
            1,
            null,
            now,
            facilitatorId);

        result.Should().BeSameAs(createdGroup);
        result.Name.Should().Be("Team A");
        result.Description.Should().Be("Description");
        result.Level.Should().Be(1);
        result.ParentGroupId.Should().BeNull();
        result.FacilitatorUserId.Should().Be(facilitatorId);
        result.CreatedAt.Should().Be(now);
        result.UpdatedAt.Should().Be(now);

        _groups.Verify(
            x => x.CreateAsync(It.IsAny<SessionGroup>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateGroupAsync_Should_Throw_When_Parent_Not_Found()
    {
        var parentId = Guid.NewGuid();

        _groups
            .Setup(x => x.GetByIdAsync(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SessionGroup?)null);

        var act = async () => await _service.CreateGroupAsync(
            "Team A",
            null,
            2,
            parentId,
            DateTimeOffset.UtcNow,
            Guid.NewGuid());

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Parent group not found.*");
    }

    [Fact]
    public async Task CreateGroupAsync_Should_Throw_When_Facilitator_Mismatch()
    {
        var parentId = Guid.NewGuid();
        var parentFacilitatorId = Guid.NewGuid();
        var facilitatorId = Guid.NewGuid();

        _groups
            .Setup(x => x.GetByIdAsync(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateGroup(parentId, "Parent", 1, null, parentFacilitatorId));

        var act = async () => await _service.CreateGroupAsync(
            "Team A",
            null,
            2,
            parentId,
            DateTimeOffset.UtcNow,
            facilitatorId);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Cannot create group under another facilitator's group.");
    }

    [Fact]
    public async Task CreateGroupAsync_Should_Throw_When_Level_Is_Invalid_For_Parent()
    {
        var parentId = Guid.NewGuid();
        var facilitatorId = Guid.NewGuid();

        _groups
            .Setup(x => x.GetByIdAsync(parentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateGroup(parentId, "Parent", 1, null, facilitatorId));

        var act = async () => await _service.CreateGroupAsync(
            "Team A",
            null,
            3,
            parentId,
            DateTimeOffset.UtcNow,
            facilitatorId);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Invalid level. Expected level 2 for this parent group.*");
    }

    [Fact]
    public async Task DeleteGroupAsync_Should_Throw_When_Has_Child_Groups()
    {
        var groupId = Guid.NewGuid();

        _groups
            .Setup(x => x.GetByIdAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateGroup(groupId, "Group", 1, null, Guid.NewGuid()));
        _groups
            .Setup(x => x.HasChildGroupsAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var act = async () => await _service.DeleteGroupAsync(groupId);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot delete group that has child groups. Delete child groups first.");

        _groups.Verify(
            x => x.HasSessionsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteGroupAsync_Should_Delete_When_No_Dependencies()
    {
        var groupId = Guid.NewGuid();

        _groups
            .Setup(x => x.GetByIdAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateGroup(groupId, "Group", 1, null, Guid.NewGuid()));
        _groups
            .Setup(x => x.HasChildGroupsAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _groups
            .Setup(x => x.HasSessionsAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _groups
            .Setup(x => x.DeleteAsync(groupId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _service.DeleteGroupAsync(groupId);

        _groups.Verify(
            x => x.DeleteAsync(groupId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static SessionGroup CreateGroup(
        Guid id,
        string name,
        int level,
        Guid? parentGroupId,
        Guid facilitatorId)
    {
        return new SessionGroup(
            id,
            name,
            null,
            level,
            parentGroupId,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            facilitatorId);
    }
}
