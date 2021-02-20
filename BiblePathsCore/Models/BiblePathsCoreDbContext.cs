using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using BiblePathsCore.Models.DB;

#nullable disable

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

        public virtual DbSet<Bible> Bibles { get; set; }
        public virtual DbSet<BibleBook> BibleBooks { get; set; }
        public virtual DbSet<BibleChapter> BibleChapters { get; set; }
        public virtual DbSet<BibleNoiseWord> BibleNoiseWords { get; set; }
        public virtual DbSet<BibleVerse> BibleVerses { get; set; }
        public virtual DbSet<BibleWordIndex> BibleWordIndices { get; set; }
        public virtual DbSet<CommentaryBook> CommentaryBooks { get; set; }
        public virtual DbSet<GameGroup> GameGroups { get; set; }
        public virtual DbSet<GameTeam> GameTeams { get; set; }
        public virtual DbSet<Path> Paths { get; set; }
        public virtual DbSet<PathNode> PathNodes { get; set; }
        public virtual DbSet<PathStat> PathStats { get; set; }
        public virtual DbSet<PredefinedQuiz> PredefinedQuizzes { get; set; }
        public virtual DbSet<PredefinedQuizQuestion> PredefinedQuizQuestions { get; set; }
        public virtual DbSet<QuizAnswer> QuizAnswers { get; set; }
        public virtual DbSet<QuizBookList> QuizBookLists { get; set; }
        public virtual DbSet<QuizBookListBookMap> QuizBookListBookMaps { get; set; }
        public virtual DbSet<QuizGroupStat> QuizGroupStats { get; set; }
        public virtual DbSet<QuizQuestion> QuizQuestions { get; set; }
        public virtual DbSet<QuizQuestionStat> QuizQuestionStats { get; set; }
        public virtual DbSet<QuizUser> QuizUsers { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Name=AppConnection");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("Relational:Collation", "SQL_Latin1_General_CP1_CI_AS");

            modelBuilder.Entity<Bible>(entity =>
            {
                entity.Property(e => e.Id)
                    .HasMaxLength(64)
                    .HasColumnName("ID");

                entity.Property(e => e.Language)
                    .IsRequired()
                    .HasMaxLength(64);

                entity.Property(e => e.Version)
                    .IsRequired()
                    .HasMaxLength(64);
            });

            modelBuilder.Entity<BibleBook>(entity =>
            {
                entity.HasKey(e => new { e.BibleId, e.BookNumber })
                    .HasName("pk_BookID");

                entity.Property(e => e.BibleId)
                    .HasMaxLength(64)
                    .HasColumnName("BibleID");

                entity.Property(e => e.Name).HasMaxLength(32);

                entity.Property(e => e.Testament).HasMaxLength(32);

                entity.HasOne(d => d.Bible)
                    .WithMany(p => p.BibleBooks)
                    .HasForeignKey(d => d.BibleId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__BibleBook__Bible__1273C1CD");
            });

            modelBuilder.Entity<BibleChapter>(entity =>
            {
                entity.HasKey(e => new { e.BibleId, e.BookNumber, e.ChapterNumber })
                    .HasName("pk_BookChapterID");

                entity.Property(e => e.BibleId)
                    .HasMaxLength(64)
                    .HasColumnName("BibleID");

                entity.Property(e => e.Name).HasMaxLength(32);

                entity.HasOne(d => d.B)
                    .WithMany(p => p.BibleChapters)
                    .HasForeignKey(d => new { d.BibleId, d.BookNumber })
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_BibleID_BookNumber");
            });

            modelBuilder.Entity<BibleNoiseWord>(entity =>
            {
                entity.HasKey(e => new { e.BibleId, e.NoiseWord })
                    .HasName("pk_NoiseWordID");

                entity.Property(e => e.BibleId)
                    .HasMaxLength(64)
                    .HasColumnName("BibleID");

                entity.Property(e => e.NoiseWord).HasMaxLength(32);

                entity.Property(e => e.IsNoise).HasColumnName("isNoise");

                entity.HasOne(d => d.Bible)
                    .WithMany(p => p.BibleNoiseWords)
                    .HasForeignKey(d => d.BibleId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__BibleNois__Bible__531856C7");
            });

            modelBuilder.Entity<BibleVerse>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.BibleId)
                    .HasMaxLength(64)
                    .HasColumnName("BibleID");

                entity.Property(e => e.BookName).HasMaxLength(32);

                entity.Property(e => e.Testament).HasMaxLength(32);

                entity.Property(e => e.Text).HasMaxLength(2048);

                entity.HasOne(d => d.Bible)
                    .WithMany(p => p.BibleVerses)
                    .HasForeignKey(d => d.BibleId)
                    .HasConstraintName("FK__BibleVers__Bible__1B0907CE");
            });

            modelBuilder.Entity<BibleWordIndex>(entity =>
            {
                entity.ToTable("BibleWordIndex");

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.BibleId)
                    .IsRequired()
                    .HasMaxLength(64)
                    .HasColumnName("BibleID");

                entity.Property(e => e.VerseId).HasColumnName("VerseID");

                entity.Property(e => e.Word)
                    .IsRequired()
                    .HasMaxLength(32);

                entity.HasOne(d => d.Bible)
                    .WithMany(p => p.BibleWordIndices)
                    .HasForeignKey(d => d.BibleId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__BibleWord__Bible__7E02B4CC");
            });

            modelBuilder.Entity<CommentaryBook>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.BibleId)
                    .HasMaxLength(64)
                    .HasColumnName("BibleID");

                entity.Property(e => e.BookName).HasMaxLength(32);

                entity.HasOne(d => d.Bible)
                    .WithMany(p => p.CommentaryBooks)
                    .HasForeignKey(d => d.BibleId)
                    .HasConstraintName("FK__Commentar__Bible__49C3F6B7");
            });

            modelBuilder.Entity<GameGroup>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.Name).HasMaxLength(256);

                entity.Property(e => e.Owner).HasMaxLength(256);

                entity.Property(e => e.PathId).HasColumnName("PathID");

                entity.HasOne(d => d.Path)
                    .WithMany(p => p.GameGroups)
                    .HasForeignKey(d => d.PathId)
                    .HasConstraintName("FK__GameGroup__PathI__02FC7413");
            });

            modelBuilder.Entity<GameTeam>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.CurrentStepId).HasColumnName("CurrentStepID");

                entity.Property(e => e.GroupId).HasColumnName("GroupID");

                entity.Property(e => e.GuideWord).HasMaxLength(256);

                entity.Property(e => e.KeyWord).HasMaxLength(256);

                entity.Property(e => e.Name).HasMaxLength(256);

                entity.HasOne(d => d.Group)
                    .WithMany(p => p.GameTeams)
                    .HasForeignKey(d => d.GroupId)
                    .HasConstraintName("FK__GameTeams__Group__09A971A2");
            });

            modelBuilder.Entity<Path>(entity =>
            {
                entity.HasIndex(e => e.Name, "AK_Name")
                    .IsUnique();

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.ComputedRating).HasColumnType("decimal(16, 8)");

                entity.Property(e => e.IsDeleted).HasColumnName("isDeleted");

                entity.Property(e => e.IsPublicEditable).HasColumnName("isPublicEditable");

                entity.Property(e => e.IsPublished).HasColumnName("isPublished");

                entity.Property(e => e.Name).HasMaxLength(256);

                entity.Property(e => e.Owner).HasMaxLength(256);

                entity.Property(e => e.OwnerBibleId)
                    .HasMaxLength(64)
                    .HasColumnName("OwnerBibleID");

                entity.Property(e => e.Topics).HasMaxLength(256);
            });

            modelBuilder.Entity<PathNode>(entity =>
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

            modelBuilder.Entity<PathStat>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.EventData).HasMaxLength(2048);

                entity.Property(e => e.PathId).HasColumnName("PathID");

                entity.HasOne(d => d.Path)
                    .WithMany(p => p.PathStats)
                    .HasForeignKey(d => d.PathId)
                    .HasConstraintName("FK__PathStats__PathI__286302EC");
            });

            modelBuilder.Entity<PredefinedQuiz>(entity =>
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

            modelBuilder.Entity<PredefinedQuizQuestion>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.PredefinedQuizId).HasColumnName("PredefinedQuizID");

                entity.HasOne(d => d.PredefinedQuiz)
                    .WithMany(p => p.PredefinedQuizQuestions)
                    .HasForeignKey(d => d.PredefinedQuizId)
                    .HasConstraintName("FK__Predefine__Prede__5070F446");
            });

            modelBuilder.Entity<QuizAnswer>(entity =>
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

            modelBuilder.Entity<QuizBookList>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.BookListName).HasMaxLength(2048);

                entity.Property(e => e.IsDeleted).HasColumnName("isDeleted");
            });

            modelBuilder.Entity<QuizBookListBookMap>(entity =>
            {
                entity.ToTable("QuizBookListBookMap");

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.BookListId).HasColumnName("BookListID");

                entity.Property(e => e.IsDeleted).HasColumnName("isDeleted");

                entity.HasOne(d => d.BookList)
                    .WithMany(p => p.QuizBookListBookMaps)
                    .HasForeignKey(d => d.BookListId)
                    .HasConstraintName("FK__QuizBookL__BookL__45F365D3");
            });

            modelBuilder.Entity<QuizGroupStat>(entity =>
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

            modelBuilder.Entity<QuizQuestion>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.BibleId)
                    .HasMaxLength(64)
                    .HasColumnName("BibleID");

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

            modelBuilder.Entity<QuizQuestionStat>(entity =>
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

            modelBuilder.Entity<QuizUser>(entity =>
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
