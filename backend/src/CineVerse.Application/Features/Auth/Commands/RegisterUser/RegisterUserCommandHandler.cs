using CineVerse.Application.Common.Exceptions;
using CineVerse.Application.Common.Interfaces;
using CineVerse.Domain.Entities;
using CineVerse.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CineVerse.Application.Features.Auth.Commands.RegisterUser;

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailService _emailService;

    public RegisterUserCommandHandler(IApplicationDbContext context, IPasswordHasher passwordHasher, IEmailService emailService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _emailService = emailService;
    }

    public async Task<Guid> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var emailTaken = await _context.Users.AnyAsync(u => u.Email == request.Email, cancellationToken);
        if (emailTaken)
        {
            throw new ConflictException($"Email \"{request.Email}\" вже використовується.");
        }

        var phoneTaken = await _context.Users.AnyAsync(u => u.Phone == request.Phone, cancellationToken);
        if (phoneTaken)
        {
            throw new ConflictException($"Телефон \"{request.Phone}\" вже використовується.");
        }

        var user = new User
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = UserRole.CUSTOMER,
            IsEmailConfirmed = false,
            EmailConfirmationToken = Guid.NewGuid().ToString("N")
        };

        _context.Users.Add(user);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            // Страховка понад перевірку AnyAsync вище: якщо два запити з однаковим email/телефоном
            // прийшли practически одночасно, обидва можуть пройти AnyAsync ДО того, як перший встигне
            // зробити SaveChanges — тоді другий впаде саме тут, на UNIQUE-обмеженні БД. Без цього catch
            // користувач отримав би сирий необроблений 500 замість зрозумілого "email вже зайнято".
            throw new ConflictException(
                $"Email \"{request.Email}\" або телефон \"{request.Phone}\" вже використовується (можливо, щойно зареєстровано паралельним запитом).");
        }

        await _emailService.SendEmailConfirmationAsync(user.Email, user.EmailConfirmationToken, cancellationToken);

        return user.Id;
    }

    /// <summary>PostgreSQL повертає SQLSTATE 23505 (unique_violation) при порушенні UNIQUE-індексу.
    /// Перевіряємо саме код помилки, а не текст повідомлення, — стабільно між версіями/локалями.</summary>
    private static bool IsUniqueConstraintViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: "23505" };
}
