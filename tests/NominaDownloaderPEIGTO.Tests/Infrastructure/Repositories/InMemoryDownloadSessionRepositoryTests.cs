using FluentAssertions;
using NominaDownloaderPEIGTO.Domain.Entities;
using NominaDownloaderPEIGTO.Domain.Enums;
using NominaDownloaderPEIGTO.Domain.ValueObjects;
using NominaDownloaderPEIGTO.Infrastructure.Repositories;

namespace NominaDownloaderPEIGTO.Tests.Infrastructure.Repositories;

public class InMemoryDownloadSessionRepositoryTests
{
    private readonly InMemoryDownloadSessionRepository _repository;

    public InMemoryDownloadSessionRepositoryTests()
    {
        _repository = new InMemoryDownloadSessionRepository();
    }

    [Fact]
    public async Task GetByIdAsync_WhenSessionExists_ShouldReturnSession()
    {
        // Arrange
        var credentials = new LoginCredentials("user", "pass");
        var config = new DownloadConfig(@"C:\Downloads");
        var session = new DownloadSession(credentials, config);

        await _repository.CreateAsync(session);

        // Act
        var result = await _repository.GetByIdAsync(session.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(session.Id);
        result.Credentials.Username.Should().Be("user");
    }

    [Fact]
    public async Task GetByIdAsync_WhenSessionDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_WithValidSession_ShouldStoreSession()
    {
        // Arrange
        var credentials = new LoginCredentials("user", "pass");
        var config = new DownloadConfig(@"C:\Downloads");
        var session = new DownloadSession(credentials, config);

        // Act
        var result = await _repository.CreateAsync(session);

        // Assert
        result.Should().Be(session);

        // Verificar que se puede recuperar
        var retrieved = await _repository.GetByIdAsync(session.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(session.Id);
    }

    [Fact]
    public async Task CreateAsync_WithNullSession_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = async () => await _repository.CreateAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("session");
    }

    [Fact]
    public async Task UpdateAsync_WithExistingSession_ShouldUpdateSession()
    {
        // Arrange
        var credentials = new LoginCredentials("user", "pass");
        var config = new DownloadConfig(@"C:\Downloads");
        var session = new DownloadSession(credentials, config);

        await _repository.CreateAsync(session);

        // Modificar la sesión
        session.Start();

        // Act
        await _repository.UpdateAsync(session);

        // Assert
        var retrieved = await _repository.GetByIdAsync(session.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Status.Should().Be(DownloadStatus.InProgress);
    }

    [Fact]
    public async Task UpdateAsync_WithNewSession_ShouldAddSession()
    {
        // Arrange
        var credentials = new LoginCredentials("user", "pass");
        var config = new DownloadConfig(@"C:\Downloads");
        var session = new DownloadSession(credentials, config);

        // Act (sin crear primero)
        await _repository.UpdateAsync(session);

        // Assert
        var retrieved = await _repository.GetByIdAsync(session.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(session.Id);
    }

    [Fact]
    public async Task UpdateAsync_WithNullSession_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = async () => await _repository.UpdateAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("session");
    }

    [Fact]
    public async Task GetActiveSessionsAsync_ShouldReturnOnlyActiveOrPendingSessions()
    {
        // Arrange
        var credentials = new LoginCredentials("user", "pass");
        var config = new DownloadConfig(@"C:\Downloads");

        var pendingSession = new DownloadSession(credentials, config);
        var inProgressSession = new DownloadSession(credentials, config);
        var completedSession = new DownloadSession(credentials, config);
        var failedSession = new DownloadSession(credentials, config);

        inProgressSession.Start();
        completedSession.Start();
        completedSession.Complete();
        failedSession.Start();
        failedSession.Fail("Error");

        await _repository.CreateAsync(pendingSession);
        await _repository.CreateAsync(inProgressSession);
        await _repository.CreateAsync(completedSession);
        await _repository.CreateAsync(failedSession);

        // Act
        var activeSessions = await _repository.GetActiveSessionsAsync();

        // Assert
        activeSessions.Should().HaveCount(2);
        activeSessions.Should().Contain(s => s.Id == pendingSession.Id);
        activeSessions.Should().Contain(s => s.Id == inProgressSession.Id);
        activeSessions.Should().NotContain(s => s.Id == completedSession.Id);
        activeSessions.Should().NotContain(s => s.Id == failedSession.Id);
    }

    [Fact]
    public async Task GetActiveSessionsAsync_WhenNoActiveSessions_ShouldReturnEmptyCollection()
    {
        // Arrange
        var credentials = new LoginCredentials("user", "pass");
        var config = new DownloadConfig(@"C:\Downloads");
        var completedSession = new DownloadSession(credentials, config);
        
        completedSession.Start();
        completedSession.Complete();
        await _repository.CreateAsync(completedSession);

        // Act
        var activeSessions = await _repository.GetActiveSessionsAsync();

        // Assert
        activeSessions.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRecentSessionsAsync_ShouldReturnSessionsOrderedByStartedAt()
    {
        // Arrange
        var credentials = new LoginCredentials("user", "pass");
        var config = new DownloadConfig(@"C:\Downloads");

        var sessions = new List<DownloadSession>();
        
        // Crear 5 sesiones con diferentes tiempos de inicio
        for (int i = 0; i < 5; i++)
        {
            var session = new DownloadSession(credentials, config);
            session.Start();
            
            // Simular diferentes tiempos de inicio manipulando la propiedad privada
            // (En un escenario real, las sesiones tendrían diferentes StartedAt)
            sessions.Add(session);
            await _repository.CreateAsync(session);
        }

        // Act
        var recentSessions = await _repository.GetRecentSessionsAsync(3);

        // Assert
        recentSessions.Should().HaveCount(3);
        
        // Verificar que están ordenadas por StartedAt (más recientes primero)
        var sessionsList = recentSessions.ToList();
        for (int i = 0; i < sessionsList.Count - 1; i++)
        {
            sessionsList[i].StartedAt.Should().BeOnOrAfter(sessionsList[i + 1].StartedAt);
        }
    }

    [Fact]
    public async Task GetRecentSessionsAsync_WithDefaultCount_ShouldReturnMaximum10Sessions()
    {
        // Arrange
        var credentials = new LoginCredentials("user", "pass");
        var config = new DownloadConfig(@"C:\Downloads");

        // Crear 15 sesiones
        for (int i = 0; i < 15; i++)
        {
            var session = new DownloadSession(credentials, config);
            await _repository.CreateAsync(session);
        }

        // Act
        var recentSessions = await _repository.GetRecentSessionsAsync();

        // Assert
        recentSessions.Should().HaveCount(10); // Default count
    }

    [Fact]
    public async Task GetRecentSessionsAsync_WithSpecificCount_ShouldReturnRequestedNumber()
    {
        // Arrange
        var credentials = new LoginCredentials("user", "pass");
        var config = new DownloadConfig(@"C:\Downloads");

        // Crear 8 sesiones
        for (int i = 0; i < 8; i++)
        {
            var session = new DownloadSession(credentials, config);
            await _repository.CreateAsync(session);
        }

        // Act
        var recentSessions = await _repository.GetRecentSessionsAsync(5);

        // Assert
        recentSessions.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetRecentSessionsAsync_WhenFewerSessionsThanRequested_ShouldReturnAllSessions()
    {
        // Arrange
        var credentials = new LoginCredentials("user", "pass");
        var config = new DownloadConfig(@"C:\Downloads");

        // Crear solo 3 sesiones
        for (int i = 0; i < 3; i++)
        {
            var session = new DownloadSession(credentials, config);
            await _repository.CreateAsync(session);
        }

        // Act
        var recentSessions = await _repository.GetRecentSessionsAsync(10);

        // Assert
        recentSessions.Should().HaveCount(3); // Solo las 3 que existen
    }

    [Fact]
    public async Task Repository_ShouldBeThreadSafe_WithConcurrentOperations()
    {
        // Arrange
        var credentials = new LoginCredentials("user", "pass");
        var config = new DownloadConfig(@"C:\Downloads");
        var sessions = new List<DownloadSession>();

        // Crear 50 sesiones
        for (int i = 0; i < 50; i++)
        {
            sessions.Add(new DownloadSession(credentials, config));
        }

        // Act - Operaciones concurrentes
        var createTasks = sessions.Select(s => _repository.CreateAsync(s));
        var updateTasks = sessions.Select(s => _repository.UpdateAsync(s));
        var readTasks = sessions.Select(s => _repository.GetByIdAsync(s.Id));

        await Task.WhenAll(createTasks);
        await Task.WhenAll(updateTasks);
        var results = await Task.WhenAll(readTasks);

        // Assert
        results.Should().HaveCount(50);
        results.Should().AllSatisfy(session => session.Should().NotBeNull());

        var activeSessions = await _repository.GetActiveSessionsAsync();
        activeSessions.Should().HaveCount(50); // Todas están en estado Pending
    }

    [Fact]
    public async Task AllMethods_WithCancellationToken_ShouldAcceptTokenWithoutError()
    {
        // Arrange
        var cancellationToken = new CancellationTokenSource().Token;
        var credentials = new LoginCredentials("user", "pass");
        var config = new DownloadConfig(@"C:\Downloads");
        var session = new DownloadSession(credentials, config);

        // Act & Assert - No debería lanzar excepciones
        var created = await _repository.CreateAsync(session, cancellationToken);
        created.Should().NotBeNull();

        var retrieved = await _repository.GetByIdAsync(session.Id, cancellationToken);
        retrieved.Should().NotBeNull();

        await _repository.UpdateAsync(session, cancellationToken);

        var activeSessions = await _repository.GetActiveSessionsAsync(cancellationToken);
        activeSessions.Should().NotBeNull();

        var recentSessions = await _repository.GetRecentSessionsAsync(10, cancellationToken);
        recentSessions.Should().NotBeNull();

        // Si llegamos aquí, todas las operaciones fueron exitosas
        true.Should().BeTrue();
    }
}
