using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using BiblePathsCore.Models.DB;

namespace BiblePathsCore.Models
{
    public partial class BiblePathsCoreDbContext : DbContext
    {
        public BiblePathsCoreDbContext()
        {
        }

        public BiblePathsCoreDbContext(DbContextOptions<BiblePathsCoreDbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<BibleBooks> BibleBooks { get; set; }
        public virtual DbSet<BibleChapters> BibleChapters { get; set; }
        public virtual DbSet<BibleNoiseWords> BibleNoiseWords { get; set; }
        public virtual DbSet<BibleVerses> BibleVerses { get; set; }
        public virtual DbSet<Bibles> Bibles { get; set; }
        public virtual DbSet<CommentaryBooks> CommentaryBooks { get; set; }
        public virtual DbSet<PathNodes> PathNodes { get; set; }
        public virtual DbSet<PathStats> PathStats { get; set; }
        public virtual DbSet<Paths> Paths { get; set; }
        public virtual DbSet<PredefinedQuizQuestions> PredefinedQuizQuestions { get; set; }
        public virtual DbSet<PredefinedQuizzes> PredefinedQuizzes { get; set; }
        public virtual DbSet<QuizAnswers> QuizAnswers { get; set; }
        public virtual DbSet<QuizBookListBookMap> QuizBookListBookMap { get; set; }
        public virtual DbSet<QuizBookLists> QuizBookLists { get; set; }
        public virtual DbSet<QuizGroupStats> QuizGroupStats { get; set; }
        public virtual DbSet<QuizQuestionStats> QuizQuestionStats { get; set; }
        public virtual DbSet<QuizQuestions> QuizQuestions { get; set; }
        public virtual DbSet<QuizUsers> QuizUsers { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Name=AppConnection");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BibleBooks>(entity =>
            {
                entity.HasKey(e => new { e.BibleId, e.BookNumber })
                    .HasName("pk_BookID");

                entity.Property(e => e.BibleId)
                    .HasColumnName("BibleID")
                    .HasMaxLength(64);

                entity.Property(e => e.Name).HasMaxLength(32);

                entity.Property(e => e.Testament).HasMaxLength(32);

                entity.HasOne(d => d.Bible)
                    .WithMany(p => p.BibleBooks)
                    .HasForeignKey(d => d.BibleId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__BibleBook__Bible__1273C1CD");
            });

            modelBuilder.Entity<BibleChapters>(entity =>
            {
                entity.HasKey(e => new { e.BibleId, e.BookNumber, e.ChapterNumber })
                    .HasName("pk_BookChapterID");

                entity.Property(e => e.BibleId)
                    .HasColumnName("BibleID")
                    .HasMaxLength(64);

                entity.Property(e => e.Name).HasMaxLength(32);

                entity.HasOne(d => d.B)
                    .WithMany(p => p.BibleChapters)
                    .HasForeignKey(d => new { d.BibleId, d.BookNumber })
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_BibleID_BookNumber");
            });

            modelBuilder.Entity<BibleNoiseWords>(entity =>
            {
                entity.HasKey(e => new { e.BibleId, e.NoiseWord })
                    .HasName("pk_NoiseWordID");

                entity.Property(e => e.BibleId)
                    .HasColumnName("BibleID")
                    .HasMaxLength(64);

                entity.Property(e => e.NoiseWord).HasMaxLength(32);

                entity.HasOne(d => d.Bible)
                    .WithMany(p => p.BibleNoiseWords)
                    .HasForeignKey(d => d.BibleId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__BibleNois__Bible__15502E78");
            });

            modelBuilder.Entity<BibleVerses>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.BibleId)
                    .HasColumnName("BibleID")
                    .HasMaxLength(64);

                entity.Property(e => e.BookName).HasMaxLength(32);

                entity.Property(e => e.Testament).HasMaxLength(32);

                entity.Property(e => e.Text).HasMaxLength(2048);

                entity.HasOne(d => d.Bible)
                    .WithMany(p => p.BibleVerses)
                    .HasForeignKey(d => d.BibleId)
                    .HasConstraintName("FK__BibleVers__Bible__1B0907CE");
            });

            modelBuilder.Entity<Bibles>(entity =>
            {
                entity.Property(e => e.Id)
                    .HasColumnName("ID")
                    .HasMaxLength(64);

                entity.Property(e => e.Language)
                    .IsRequired()
                    .HasMaxLength(64);

                entity.Property(e => e.Version)
                    .IsRequired()
                    .HasMaxLength(64);
            });

            modelBuilder.Entity<CommentaryBooks>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.BibleId)
                    .HasColumnName("BibleID")
                    .HasMaxLength(64);

                entity.Property(e => e.BookName).HasMaxLength(32);

                entity.HasOne(d => d.Bible)
                    .WithMany(p => p.CommentaryBooks)
                    .HasForeignKey(d => d.BibleId)
                    .HasConstraintName("FK__Commentar__Bible__49C3F6B7");
            });

            modelBuilder.Entity<PathNodes>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.EndVerse).HasColumnName("End_Verse");

                entity.Property(e => e.PathId).HasColumnName("PathID");

                entity.Property(e => e.StartVerse).HasColumnName("Start_Verse");

                entity.HasOne(d => d.Path)
                    .WithMany(p => p.PathNodes)
                    .HasForeignKey(d => d.PathId)
                    .HasConstraintName("FK__PathNodes__PathI__25869641");
            });

            modelBuilder.Entity<PathStats>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.EventData).HasMaxLength(2048);

                entity.Property(e => e.PathId).HasColumnName("PathID");

                entity.HasOne(d => d.Path)
                    .WithMany(p => p.PathStats)
                    .HasForeignKey(d => d.PathId)
                    .HasConstraintName("FK__PathStats__PathI__286302EC");
            });

            modelBuilder.Entity<Paths>(entity =>
            {
                entity.HasIndex(e => e.Name)
                    .HasName("AK_Name")
                    .IsUnique();

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.ComputedRating).HasColumnType("decimal(16, 8)");

                entity.Property(e => e.IsDeleted).HasColumnName("isDeleted");

                entity.Property(e => e.IsPublicEditable).HasColumnName("isPublicEditable");

                entity.Property(e => e.IsPublished).HasColumnName("isPublished");

                entity.Property(e => e.Name).HasMaxLength(256);

                entity.Property(e => e.Owner).HasMaxLength(256);

                entity.Property(e => e.OwnerBibleId)
                    .HasColumnName("OwnerBibleID")
                    .HasMaxLength(64);

                entity.Property(e => e.Topics).HasMaxLength(256);
            });

            modelBuilder.Entity<PredefinedQuizQuestions>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.PredefinedQuizId).HasColumnName("PredefinedQuizID");

                entity.HasOne(d => d.PredefinedQuiz)
                    .WithMany(p => p.PredefinedQuizQuestions)
                    .HasForeignKey(d => d.PredefinedQuizId)
                    .HasConstraintName("FK__Predefine__Prede__5070F446");
            });

            modelBuilder.Entity<PredefinedQuizzes>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.IsDeleted).HasColumnName("isDeleted");

                entity.Property(e => e.QuizName).HasMaxLength(2048);

                entity.Property(e => e.QuizUserId).HasColumnName("QuizUserID");

                entity.HasOne(d => d.QuizUser)
                    .WithMany(p => p.PredefinedQuizzes)
                    .HasForeignKey(d => d.QuizUserId)
                    .HasConstraintName("FK__Predefine__QuizU__4CA06362");
            });

            modelBuilder.Entity<QuizAnswers>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.Answer).HasMaxLength(1024);

                entity.Property(e => e.IsPrimary).HasColumnName("isPrimary");

                entity.Property(e => e.QuestionId).HasColumnName("QuestionID");

                entity.HasOne(d => d.Question)
                    .WithMany(p => p.QuizAnswers)
                    .HasForeignKey(d => d.QuestionId)
                    .HasConstraintName("FK__QuizAnswe__Quest__36B12243");
            });

            modelBuilder.Entity<QuizBookListBookMap>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.BookListId).HasColumnName("BookListID");

                entity.Property(e => e.IsDeleted).HasColumnName("isDeleted");

                entity.HasOne(d => d.BookList)
                    .WithMany(p => p.QuizBookListBookMap)
                    .HasForeignKey(d => d.BookListId)
                    .HasConstraintName("FK__QuizBookL__BookL__45F365D3");
            });

            modelBuilder.Entity<QuizBookLists>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.BookListName).HasMaxLength(2048);

                entity.Property(e => e.IsDeleted).HasColumnName("isDeleted");
            });

            modelBuilder.Entity<QuizGroupStats>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.GroupName).HasMaxLength(2048);

                entity.Property(e => e.IsDeleted).HasColumnName("isDeleted");

                entity.Property(e => e.QuizUserId).HasColumnName("QuizUserID");

                entity.HasOne(d => d.QuizUser)
                    .WithMany(p => p.QuizGroupStats)
                    .HasForeignKey(d => d.QuizUserId)
                    .HasConstraintName("FK__QuizGroup__QuizU__3E52440B");
            });

            modelBuilder.Entity<QuizQuestionStats>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.EventData).HasMaxLength(2048);

                entity.Property(e => e.QuestionId).HasColumnName("QuestionID");

                entity.Property(e => e.QuizGroupId).HasColumnName("QuizGroupID");

                entity.Property(e => e.QuizUserId).HasColumnName("QuizUserID");

                entity.HasOne(d => d.Question)
                    .WithMany(p => p.QuizQuestionStats)
                    .HasForeignKey(d => d.QuestionId)
                    .HasConstraintName("FK__QuizQuest__Quest__3A81B327");

                entity.HasOne(d => d.QuizUser)
                    .WithMany(p => p.QuizQuestionStats)
                    .HasForeignKey(d => d.QuizUserId)
                    .HasConstraintName("FK__QuizQuest__QuizU__3B75D760");
            });

            modelBuilder.Entity<QuizQuestions>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.BibleId)
                    .HasColumnName("BibleID")
                    .HasMaxLength(64);

                entity.Property(e => e.ChallengeComment).HasMaxLength(1024);

                entity.Property(e => e.ChallengedBy).HasMaxLength(256);

                entity.Property(e => e.EndVerse).HasColumnName("End_Verse");

                entity.Property(e => e.IsAnswered).HasColumnName("isAnswered");

                entity.Property(e => e.IsDeleted).HasColumnName("isDeleted");

                entity.Property(e => e.LastAsked).HasDefaultValueSql("('2001-01-01')");

                entity.Property(e => e.Owner).HasMaxLength(256);

                entity.Property(e => e.Points).HasDefaultValueSql("((1))");

                entity.Property(e => e.Question).HasMaxLength(2048);

                entity.Property(e => e.Source).HasMaxLength(256);

                entity.Property(e => e.StartVerse).HasColumnName("Start_Verse");

                entity.HasOne(d => d.Bible)
                    .WithMany(p => p.QuizQuestions)
                    .HasForeignKey(d => d.BibleId)
                    .HasConstraintName("FK__QuizQuest__Bible__5AEE82B9");
            });

            modelBuilder.Entity<QuizUsers>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.Email).HasMaxLength(256);

                entity.Property(e => e.IsModerator).HasColumnName("isModerator");

                entity.Property(e => e.IsQuestionBuilderLocked).HasColumnName("isQuestionBuilderLocked");

                entity.Property(e => e.IsQuizTakerLocked).HasColumnName("isQuizTakerLocked");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
