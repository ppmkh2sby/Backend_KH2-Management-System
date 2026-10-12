using KH2.ManagementSystem.Domain.FaceRecognition;
using KH2.ManagementSystem.Domain.Santris;
using KH2.ManagementSystem.Domain.Users;
using KH2.ManagementSystem.Infrastructure.FaceRecognition;
using KH2.ManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace KH2.ManagementSystem.UnitTests;

[Collection(PostgreSqlIntegrationFixtureDefinition.Name)]
public sealed class PostgreSqlEnrollmentStoreIntegrationTests(PostgreSqlIntegrationFixture fixture)
{
    [Fact]
    public async Task SaveAsyncAtomicallySupersedesTheOldGenerationAndActivatesTheReplacement()
    {
        await using var context = await fixture.CreateContextAsync();
        var profile = await SeedActiveEnrollmentAsync(context);
        var store = new FaceEnrollmentStore(context);
        var replacement = NewEnrollment(profile.Id);

        var saved = await store.SaveAsync(profile, false, replacement, NewEmbeddings(replacement.Id), DateTimeOffset.UtcNow, CancellationToken.None);

        Assert.True(saved);
        context.ChangeTracker.Clear();
        var enrollments = await context.FaceEnrollments.Where(item => item.FaceProfileId == profile.Id).ToListAsync();
        Assert.Equal(2, enrollments.Count);
        Assert.Single(enrollments, item => item.Status == FaceEnrollmentStatus.Active && item.Id == replacement.Id);
        Assert.Single(enrollments, item => item.Status == FaceEnrollmentStatus.Superseded);
        Assert.Equal(5, await context.FaceEmbeddings.CountAsync(item => item.FaceEnrollmentId == replacement.Id));
        Assert.Equal(FaceProfileStatus.Active, await context.FaceProfiles.Where(item => item.Id == profile.Id).Select(item => item.Status).SingleAsync());
    }

    [Fact]
    public async Task SaveAsyncRollsBackAllReenrollmentWritesWhenFinalActivationFails()
    {
        Guid profileId;
        Guid oldEnrollmentId;
        await using (var setup = await fixture.CreateContextAsync())
        {
            var profile = await SeedActiveEnrollmentAsync(setup);
            profileId = profile.Id;
            oldEnrollmentId = await setup.FaceEnrollments.Where(item => item.FaceProfileId == profile.Id).Select(item => item.Id).SingleAsync();
        }

        var interceptor = new ThrowOnThirdSaveChangesInterceptor();
        await using (var context = await fixture.CreateContextAsync(false, interceptor))
        {
            var profile = await context.FaceProfiles.SingleAsync(item => item.Id == profileId);
            var store = new FaceEnrollmentStore(context);
            var replacement = NewEnrollment(profileId);

            await Assert.ThrowsAsync<InvalidOperationException>(() => store.SaveAsync(
                profile, false, replacement, NewEmbeddings(replacement.Id), DateTimeOffset.UtcNow, CancellationToken.None));
        }

        await using var verification = await fixture.CreateContextAsync(false);
        var enrollments = await verification.FaceEnrollments.Where(item => item.FaceProfileId == profileId).ToListAsync();
        Assert.Single(enrollments);
        Assert.Equal(oldEnrollmentId, enrollments[0].Id);
        Assert.Equal(FaceEnrollmentStatus.Active, enrollments[0].Status);
        Assert.Equal(5, await verification.FaceEmbeddings.CountAsync(item => item.FaceEnrollmentId == oldEnrollmentId));
        Assert.Equal(FaceProfileStatus.Active, await verification.FaceProfiles.Where(item => item.Id == profileId).Select(item => item.Status).SingleAsync());
    }

    private static async Task<FaceProfile> SeedActiveEnrollmentAsync(AppDbContext context)
    {
        var userId = Guid.NewGuid();
        var user = new User(userId, $"enrollment-{userId:N}", "Enrollment", null, UserRole.Santri, "hash");
        var santri = new Santri(Guid.NewGuid(), user.Id, "Enrollment", userId.ToString("N")[..12], "A", "B", "M", "A", "A");
        var profile = new FaceProfile(Guid.NewGuid(), santri.Id, DateTimeOffset.UtcNow);
        profile.ActivateProfile(DateTimeOffset.UtcNow);
        var enrollment = NewEnrollment(profile.Id);
        enrollment.Activate(DateTimeOffset.UtcNow);
        context.AddRange(user, santri, profile, enrollment);
        context.FaceEmbeddings.AddRange(NewEmbeddings(enrollment.Id));
        await context.SaveChangesAsync();
        return profile;
    }

    private static FaceEnrollment NewEnrollment(Guid profileId) =>
        new(Guid.NewGuid(), profileId, "arcface", "1", DateTimeOffset.UtcNow);

    private static FaceEmbedding[] NewEmbeddings(Guid enrollmentId) =>
        Enumerable.Range(1, 5).Select(index => new FaceEmbedding(
            Guid.NewGuid(), enrollmentId, Enumerable.Repeat(.1f, FaceEmbedding.RequiredDimensions).ToArray(), index, .9f)).ToArray();

    private sealed class ThrowOnThirdSaveChangesInterceptor : SaveChangesInterceptor
    {
        private int saveCount;

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref saveCount) == 3)
            {
                throw new InvalidOperationException("Test-only failure before final enrollment activation.");
            }

            return ValueTask.FromResult(result);
        }
    }
}
