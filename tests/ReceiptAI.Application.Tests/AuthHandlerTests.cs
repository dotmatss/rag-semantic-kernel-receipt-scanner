using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using ReceiptAI.Application.Common.Exceptions;
using ReceiptAI.Application.Common.Interfaces;
using ReceiptAI.Application.Common.Interfaces.Auth;
using ReceiptAI.Application.Common.Interfaces.Persistence;
using ReceiptAI.Application.Features.Auth.Commands.Login;
using ReceiptAI.Application.Features.Auth.Commands.Register;
using ReceiptAI.Application.Features.Auth.Queries.GetCurrentUser;
using ReceiptAI.Domain.Entities;

namespace ReceiptAI.Application.Tests;

public class AuthHandlerTests
{
    private const string Email = "Jane@Example.com";
    private const string Password = "correct-horse";

    private readonly FakeUserRepository _users = new();
    private readonly FakePasswordHasher _hasher = new();
    private readonly FakeJwtService _tokens = new();

    [Fact]
    public async Task Register_stores_normalized_user_and_returns_tokens()
    {
        var handler = new RegisterCommandHandler(_users, _hasher, _tokens);

        var response = await handler.Handle(new RegisterCommand(Email, Password), default);

        var user = _users.Users.Single();
        Assert.Equal("jane@example.com", user.Email);
        Assert.Equal($"access-{user.Id}", response.AccessToken);
    }

    [Fact]
    public async Task Register_with_existing_email_throws_conflict()
    {
        var handler = new RegisterCommandHandler(_users, _hasher, _tokens);
        await handler.Handle(new RegisterCommand(Email, Password), default);

        await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new RegisterCommand("jane@example.com", "another-password"), default));
    }

    [Fact]
    public async Task Login_with_valid_credentials_returns_tokens()
    {
        _users.Users.Add(new User(Email, _hasher.HashPassword(Password)));
        var handler = new LoginCommandHandler(_users, _hasher, _tokens);

        var response = await handler.Handle(new LoginCommand("  jane@example.COM ", Password), default);

        Assert.Equal($"access-{_users.Users.Single().Id}", response.AccessToken);
    }

    [Theory]
    [InlineData(Email, "wrong-password")]
    [InlineData("nobody@example.com", Password)]
    public async Task Login_with_invalid_credentials_throws_unauthorized(string email, string password)
    {
        _users.Users.Add(new User(Email, _hasher.HashPassword(Password)));
        var handler = new LoginCommandHandler(_users, _hasher, _tokens);

        await Assert.ThrowsAsync<UnauthorizedException>(() => handler.Handle(new LoginCommand(email, password), default));
    }

    [Fact]
    public async Task GetCurrentUser_returns_user_from_database()
    {
        var user = new User(Email, "hash");
        _users.Users.Add(user);
        var handler = new GetCurrentUserQueryHandler(new FakeCurrentUser { UserId = user.Id }, _users);

        var response = await handler.Handle(new GetCurrentUserQuery(), default);

        Assert.Equal(new CurrentUserResponse(user.Id, "jane@example.com", "User"), response);
    }

    [Fact]
    public async Task GetCurrentUser_without_user_throws_unauthorized()
    {
        var handler = new GetCurrentUserQueryHandler(new FakeCurrentUser(), _users);

        await Assert.ThrowsAsync<UnauthorizedException>(() => handler.Handle(new GetCurrentUserQuery(), default));
    }

    [Theory]
    [InlineData("not-an-email", Password, "Email")]
    [InlineData(Email, "short", "Password")]
    [InlineData("", "", "Email")]
    public void Register_validator_rejects_bad_input(string email, string password, string failingProperty)
    {
        var result = new RegisterCommandValidator().Validate(new RegisterCommand(email, password));

        Assert.Contains(result.Errors, e => e.PropertyName == failingProperty);
    }

    [Theory]
    [InlineData("", Password, "Email")]
    [InlineData(Email, "", "Password")]
    public void Login_validator_rejects_missing_fields(string email, string password, string failingProperty)
    {
        var result = new LoginCommandValidator().Validate(new LoginCommand(email, password));

        Assert.Contains(result.Errors, e => e.PropertyName == failingProperty);
    }

    [Fact]
    public void Login_validator_does_not_apply_registration_rules()
    {
        // Existing users must still log in if the password policy changes later.
        var result = new LoginCommandValidator().Validate(new LoginCommand("legacy-user", "short"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Pipeline_runs_validators_before_the_handler()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddSingleton<IUserRepository>(_users);
        services.AddSingleton<IPasswordHasher>(_hasher);
        services.AddSingleton<IJwtService>(_tokens);
        await using var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<ISender>();

        await Assert.ThrowsAsync<ValidationException>(() => sender.Send(new RegisterCommand("not-an-email", "short")));
        Assert.Empty(_users.Users);

        var response = await sender.Send(new RegisterCommand(Email, Password));
        Assert.NotNull(response.AccessToken);
    }
}
