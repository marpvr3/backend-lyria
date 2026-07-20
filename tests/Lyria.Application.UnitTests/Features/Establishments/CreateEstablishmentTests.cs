using Lyria.Application.Features.Establishments;
using Lyria.Application.Features.Establishments.Create;
using Lyria.Application.Features.EstablishmentCategories;
using Lyria.Application.Common.Errors;
using Lyria.Application.Common.Results;
using Lyria.Application.UnitTests.Fakes;
using Lyria.Domain.Establishments;
using Xunit;

namespace Lyria.Application.UnitTests.Features.Establishments;

public sealed class CreateEstablishmentTests
{
    private static readonly Guid ActiveCategoryId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private readonly FakeEstablishmentRepository _repository = new();
    private readonly FakeEstablishmentCategoryReadService _categoryReadService = new();
    private readonly CreateEstablishmentCommandHandler _handler;

    public CreateEstablishmentTests()
    {
        _categoryReadService.Seed(new EstablishmentCategoryResponse(
            ActiveCategoryId, "Restaurante", null, null, 1, true));
        _handler = new CreateEstablishmentCommandHandler(_repository, _categoryReadService);
    }

    [Fact]
    public async Task Handle_ValidInput_CreatesEstablishment()
    {
        var command = new CreateEstablishmentCommand(
            ActiveCategoryId, "Let It V", "let-it-v", "Desc", "https://x.com", "@x", null, null, null);

        Result<EstablishmentId> result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(_repository.Added);
        Assert.Equal(1, _repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_CategoryNotAvailable_ReturnsValidation()
    {
        var command = new CreateEstablishmentCommand(
            Guid.NewGuid(), "Test", "test-slug", null, null, null, null, null, null);

        Result<EstablishmentId> result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Contains("CategoryNotAvailable", result.Error.Code);
    }

    [Fact]
    public async Task Handle_SlugDuplicate_ReturnsConflict()
    {
        var first = new CreateEstablishmentCommand(
            ActiveCategoryId, "First", "same-slug", null, null, null, null, null, null);
        await _handler.Handle(first, CancellationToken.None);

        var second = new CreateEstablishmentCommand(
            ActiveCategoryId, "Second", "same-slug", null, null, null, null, null, null);
        Result<EstablishmentId> result = await _handler.Handle(second, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }

    [Fact]
    public async Task Handle_Error_DoesNotAdd()
    {
        var command = new CreateEstablishmentCommand(
            Guid.NewGuid(), "Test", "test-slug", null, null, null, null, null, null);

        await _handler.Handle(command, CancellationToken.None);

        Assert.Empty(_repository.Added);
    }

    [Fact]
    public async Task Handle_Error_DoesNotSave()
    {
        var command = new CreateEstablishmentCommand(
            Guid.NewGuid(), "Test", "test-slug", null, null, null, null, null, null);

        await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(0, _repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_ReturnsId()
    {
        var command = new CreateEstablishmentCommand(
            ActiveCategoryId, "Test", "test-slug", null, null, null, null, null, null);

        Result<EstablishmentId> result = await _handler.Handle(command, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.Value.Value);
    }

    [Fact]
    public async Task Handle_StartsActiveAndNotVerified()
    {
        var command = new CreateEstablishmentCommand(
            ActiveCategoryId, "Test", "test-slug", null, null, null, null, null, null);

        await _handler.Handle(command, CancellationToken.None);

        var created = _repository.Added[0];
        Assert.True(created.IsActive);
        Assert.False(created.IsVerified);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        var command = new CreateEstablishmentCommand(
            ActiveCategoryId, "Test", "ct-test", null, null, null, null, null, null);

        Result<EstablishmentId> result = await _handler.Handle(command, cts.Token);

        Assert.True(result.IsSuccess);
    }
}
