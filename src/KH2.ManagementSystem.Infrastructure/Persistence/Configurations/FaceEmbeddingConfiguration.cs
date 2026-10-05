using KH2.ManagementSystem.Domain.FaceRecognition;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Pgvector;

namespace KH2.ManagementSystem.Infrastructure.Persistence.Configurations;

public sealed class FaceEmbeddingConfiguration : IEntityTypeConfiguration<FaceEmbedding>
{
    public void Configure(EntityTypeBuilder<FaceEmbedding> builder)
    {
        builder.ToTable("FaceEmbeddings", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "CK_FaceEmbeddings_CaptureIndex_Positive",
                "\"CaptureIndex\" > 0");
            tableBuilder.HasCheckConstraint(
                "CK_FaceEmbeddings_QualityScore_Range",
                "\"QualityScore\" IS NULL OR (\"QualityScore\" >= 0 AND \"QualityScore\" <= 1)");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.FaceEnrollmentId).IsRequired();

        var converter = new ValueConverter<float[], Vector>(
            value => new Vector(value),
            value => value.ToArray());
        var comparer = new ValueComparer<float[]>(
            (left, right) => left != null && right != null && left.SequenceEqual(right),
            value => value.Aggregate(0, (hash, item) => HashCode.Combine(hash, item)),
            value => value.ToArray());

        builder.Property<float[]>("Embedding")
            .HasConversion(converter, comparer)
            .HasColumnType($"vector({FaceEmbedding.RequiredDimensions})")
            .IsRequired();
        builder.Property(x => x.QualityScore).HasColumnType("real");
        builder.Property(x => x.CaptureIndex).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.HasIndex(x => x.FaceEnrollmentId);
        builder.HasOne<FaceEnrollment>().WithMany().HasForeignKey(x => x.FaceEnrollmentId).OnDelete(DeleteBehavior.Cascade);
    }
}
