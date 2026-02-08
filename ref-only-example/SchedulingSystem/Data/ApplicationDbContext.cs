using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Models;

namespace SchedulingSystem.Data;

/// <summary>
/// Main database context for the scheduling system
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // DbSets for all entities
    public DbSet<SchoolYear> SchoolYears { get; set; }
    public DbSet<Term> Terms { get; set; }
    public DbSet<Department> Departments { get; set; }
    public DbSet<Subject> Subjects { get; set; }
    public DbSet<Teacher> Teachers { get; set; }
    public DbSet<Class> Classes { get; set; }
    public DbSet<Room> Rooms { get; set; }
    public DbSet<Period> Periods { get; set; }
    public DbSet<Lesson> Lessons { get; set; }

    // Lesson many-to-many junction tables
    public DbSet<LessonSubject> LessonSubjects { get; set; }
    public DbSet<LessonClass> LessonClasses { get; set; }
    public DbSet<LessonTeacher> LessonTeachers { get; set; }

    public DbSet<ScheduledLesson> ScheduledLessons { get; set; }
    public DbSet<ScheduledLessonRoom> ScheduledLessonRooms { get; set; }
    public DbSet<Timetable> Timetables { get; set; }
    public DbSet<Student> Students { get; set; }
    public DbSet<TeacherAvailability> TeacherAvailabilities { get; set; }
    public DbSet<RoomAvailability> RoomAvailabilities { get; set; }
    public DbSet<ClassAvailability> ClassAvailabilities { get; set; }
    public DbSet<SubjectAvailability> SubjectAvailabilities { get; set; }
    public DbSet<TeacherSubject> TeacherSubjects { get; set; }

    // Substitution Management DbSets
    public DbSet<Absence> Absences { get; set; }
    public DbSet<Substitution> Substitutions { get; set; }
    public DbSet<Notification> Notifications { get; set; }

    // Authentication DbSets
    public new DbSet<User> Users { get; set; } // 'new' keyword to hide inherited Users from IdentityDbContext
    public DbSet<MagicLink> MagicLinks { get; set; }

    // Backup DbSets
    public DbSet<DatabaseBackup> DatabaseBackups { get; set; }

    // Break Supervision DbSets
    public DbSet<BreakSupervisionDuty> BreakSupervisionDuties { get; set; }
    public DbSet<BreakSupervisionSubstitution> BreakSupervisionSubstitutions { get; set; }

    // Lesson Assignment DbSets (for teacher-subject-class combinations and room assignments)
    public DbSet<LessonAssignment> LessonAssignments { get; set; }
    public DbSet<ScheduledLessonRoomAssignment> ScheduledLessonRoomAssignments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure SchoolYear
        modelBuilder.Entity<SchoolYear>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name);
            entity.HasMany(e => e.Terms)
                .WithOne(e => e.SchoolYear)
                .HasForeignKey(e => e.SchoolYearId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Term
        modelBuilder.Entity<Term>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SchoolYearId);
        });

        // Configure Department
        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasMany(e => e.Teachers)
                .WithOne(e => e.Department)
                .HasForeignKey(e => e.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(e => e.Subjects)
                .WithOne(e => e.Department)
                .HasForeignKey(e => e.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Subject
        modelBuilder.Entity<Subject>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => e.Name);
            entity.HasOne(e => e.PreferredRoom)
                .WithMany()
                .HasForeignKey(e => e.PreferredRoomId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Teacher
        modelBuilder.Entity<Teacher>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.FirstName); // Index on FirstName for lookups
            entity.HasIndex(e => e.Email);
            // Removed old: entity.HasMany(e => e.Lessons) - now using LessonTeachers junction
            entity.HasMany(e => e.LessonTeachers)
                .WithOne(lt => lt.Teacher)
                .HasForeignKey(lt => lt.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(e => e.Availabilities)
                .WithOne(e => e.Teacher)
                .HasForeignKey(e => e.TeacherId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Class (self-referencing for parent/subclass)
        modelBuilder.Entity<Class>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name);
            entity.HasOne(e => e.ParentClass)
                .WithMany(e => e.SubClasses)
                .HasForeignKey(e => e.ParentClassId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.ClassTeacher)
                .WithMany()
                .HasForeignKey(e => e.ClassTeacherId)
                .OnDelete(DeleteBehavior.Restrict);
            // Removed old: entity.HasMany(e => e.Lessons) - now using LessonClasses junction
            entity.HasMany(e => e.LessonClasses)
                .WithOne(lc => lc.Class)
                .HasForeignKey(lc => lc.ClassId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(e => e.Students)
                .WithOne(e => e.Class)
                .HasForeignKey(e => e.ClassId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Room (self-referencing for alternative room)
        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.RoomNumber).IsUnique();
            entity.HasOne(e => e.AlternativeRoom)
                .WithMany()
                .HasForeignKey(e => e.AlternativeRoomId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(e => e.Availabilities)
                .WithOne(e => e.Room)
                .HasForeignKey(e => e.RoomId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.ScheduledLessons)
                .WithOne(e => e.Room)
                .HasForeignKey(e => e.RoomId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Period
        modelBuilder.Entity<Period>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PeriodNumber);
            entity.Ignore(e => e.DurationMinutes); // Computed property
        });

        // Configure Lesson
        modelBuilder.Entity<Lesson>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Many-to-many relationships via junction tables
            entity.HasMany(e => e.LessonSubjects)
                .WithOne(ls => ls.Lesson)
                .HasForeignKey(ls => ls.LessonId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.LessonClasses)
                .WithOne(lc => lc.Lesson)
                .HasForeignKey(lc => lc.LessonId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.LessonTeachers)
                .WithOne(lt => lt.Lesson)
                .HasForeignKey(lt => lt.LessonId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.ScheduledLessons)
                .WithOne(e => e.Lesson)
                .HasForeignKey(e => e.LessonId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure LessonSubject (Lesson-Subject many-to-many)
        modelBuilder.Entity<LessonSubject>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.LessonId, e.SubjectId });

            entity.HasOne(e => e.Lesson)
                .WithMany(l => l.LessonSubjects)
                .HasForeignKey(e => e.LessonId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Subject)
                .WithMany(s => s.LessonSubjects)
                .HasForeignKey(e => e.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure LessonClass (Lesson-Class many-to-many)
        modelBuilder.Entity<LessonClass>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.LessonId, e.ClassId });

            entity.HasOne(e => e.Lesson)
                .WithMany(l => l.LessonClasses)
                .HasForeignKey(e => e.LessonId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Class)
                .WithMany(c => c.LessonClasses)
                .HasForeignKey(e => e.ClassId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure LessonTeacher (Lesson-Teacher many-to-many)
        modelBuilder.Entity<LessonTeacher>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.LessonId, e.TeacherId });

            entity.HasOne(e => e.Lesson)
                .WithMany(l => l.LessonTeachers)
                .HasForeignKey(e => e.LessonId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Teacher)
                .WithMany(t => t.LessonTeachers)
                .HasForeignKey(e => e.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.AssignedRoom)
                .WithMany()
                .HasForeignKey(e => e.RoomAssignment)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure ScheduledLesson
        modelBuilder.Entity<ScheduledLesson>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.DayOfWeek, e.PeriodId, e.TimetableId });
            entity.HasOne(e => e.Period)
                .WithMany(e => e.ScheduledLessons)
                .HasForeignKey(e => e.PeriodId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Timetable)
                .WithMany(e => e.ScheduledLessons)
                .HasForeignKey(e => e.TimetableId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.ScheduledLessonRooms)
                .WithOne(e => e.ScheduledLesson)
                .HasForeignKey(e => e.ScheduledLessonId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure ScheduledLessonRoom (many-to-many junction table)
        modelBuilder.Entity<ScheduledLessonRoom>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ScheduledLessonId, e.RoomId });
            entity.HasOne(e => e.ScheduledLesson)
                .WithMany(e => e.ScheduledLessonRooms)
                .HasForeignKey(e => e.ScheduledLessonId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Room)
                .WithMany(e => e.ScheduledLessonRooms)
                .HasForeignKey(e => e.RoomId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.PrimaryTeacherForRoom)
                .WithMany()
                .HasForeignKey(e => e.PrimaryTeacherIdForRoom)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Timetable
        modelBuilder.Entity<Timetable>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SchoolYearId);
            entity.HasOne(e => e.SchoolYear)
                .WithMany()
                .HasForeignKey(e => e.SchoolYearId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Term)
                .WithMany()
                .HasForeignKey(e => e.TermId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Student
        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.StudentNumber).IsUnique();
            entity.Ignore(e => e.FullName); // Computed property
        });

        // Configure TeacherAvailability
        modelBuilder.Entity<TeacherAvailability>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TeacherId, e.DayOfWeek, e.PeriodId });
            entity.HasOne(e => e.Period)
                .WithMany()
                .HasForeignKey(e => e.PeriodId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure RoomAvailability
        modelBuilder.Entity<RoomAvailability>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.RoomId, e.DayOfWeek, e.PeriodId });
            entity.HasOne(e => e.Period)
                .WithMany()
                .HasForeignKey(e => e.PeriodId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure ClassAvailability
        modelBuilder.Entity<ClassAvailability>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ClassId, e.DayOfWeek, e.PeriodId });
            entity.HasOne(e => e.Class)
                .WithMany()
                .HasForeignKey(e => e.ClassId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Period)
                .WithMany()
                .HasForeignKey(e => e.PeriodId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure SubjectAvailability
        modelBuilder.Entity<SubjectAvailability>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.SubjectId, e.DayOfWeek, e.PeriodId });
            entity.HasOne(e => e.Subject)
                .WithMany()
                .HasForeignKey(e => e.SubjectId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Period)
                .WithMany()
                .HasForeignKey(e => e.PeriodId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure TeacherSubject (many-to-many with additional properties)
        modelBuilder.Entity<TeacherSubject>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TeacherId, e.SubjectId }).IsUnique();
            entity.HasOne(e => e.Teacher)
                .WithMany(t => t.TeacherSubjects)
                .HasForeignKey(e => e.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Subject)
                .WithMany(s => s.TeacherSubjects)
                .HasForeignKey(e => e.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Absence
        modelBuilder.Entity<Absence>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TeacherId, e.Date });
            entity.HasIndex(e => e.Date);
            entity.HasIndex(e => e.Status);
            entity.HasOne(e => e.Teacher)
                .WithMany()
                .HasForeignKey(e => e.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.ReportedByUser)
                .WithMany()
                .HasForeignKey(e => e.ReportedByUserId)
                .IsRequired(false)  // Make this relationship optional
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(e => e.Substitutions)
                .WithOne(e => e.Absence)
                .HasForeignKey(e => e.AbsenceId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Substitution
        modelBuilder.Entity<Substitution>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.AbsenceId, e.ScheduledLessonId });
            entity.HasIndex(e => e.SubstituteTeacherId);
            entity.HasOne(e => e.Absence)
                .WithMany(a => a.Substitutions)
                .HasForeignKey(e => e.AbsenceId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.ScheduledLesson)
                .WithMany()
                .HasForeignKey(e => e.ScheduledLessonId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.SubstituteTeacher)
                .WithMany()
                .HasForeignKey(e => e.SubstituteTeacherId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.AssignedByUser)
                .WithMany()
                .HasForeignKey(e => e.AssignedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Notification
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.IsRead });
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.Type);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.Role);
            entity.HasIndex(e => e.IsActive);
        });

        // Configure MagicLink
        modelBuilder.Entity<MagicLink>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => new { e.UserId, e.CreatedAt });
            entity.HasIndex(e => e.ExpiresAt);
            entity.HasIndex(e => e.IsUsed);
            entity.HasOne(e => e.User)
                .WithMany(u => u.MagicLinks)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure DatabaseBackup
        modelBuilder.Entity<DatabaseBackup>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.IsAutomaticDailyBackup);
        });

        // Configure BreakSupervisionDuty
        // Links to Room table for supervision locations (Hof1, Hof2, oben, unten, etc.)
        // Also links to Timetable - supervision duties belong to specific timetables
        modelBuilder.Entity<BreakSupervisionDuty>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.RoomId, e.DayOfWeek, e.PeriodNumber });
            entity.HasIndex(e => new { e.TeacherId, e.DayOfWeek, e.PeriodNumber });
            entity.HasIndex(e => e.TimetableId);
            entity.HasIndex(e => new { e.TimetableId, e.RoomId, e.DayOfWeek, e.PeriodNumber });
            entity.HasOne(e => e.Room)
                .WithMany()
                .HasForeignKey(e => e.RoomId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Teacher)
                .WithMany(t => t.BreakSupervisionDuties)
                .HasForeignKey(e => e.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Timetable)
                .WithMany(t => t.BreakSupervisionDuties)
                .HasForeignKey(e => e.TimetableId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure BreakSupervisionSubstitution
        // Temporary supervision coverage during teacher absence
        modelBuilder.Entity<BreakSupervisionSubstitution>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.AbsenceId, e.BreakSupervisionDutyId });
            entity.HasIndex(e => new { e.Date, e.BreakSupervisionDutyId });
            entity.HasIndex(e => e.SubstituteTeacherId);
            entity.HasOne(e => e.Absence)
                .WithMany(a => a.SupervisionSubstitutions)
                .HasForeignKey(e => e.AbsenceId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.BreakSupervisionDuty)
                .WithMany()
                .HasForeignKey(e => e.BreakSupervisionDutyId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.SubstituteTeacher)
                .WithMany()
                .HasForeignKey(e => e.SubstituteTeacherId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.AssignedByUser)
                .WithMany()
                .HasForeignKey(e => e.AssignedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure LessonAssignment
        // Specifies which teacher teaches which subject to which class within a lesson
        modelBuilder.Entity<LessonAssignment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.LessonId);
            entity.HasIndex(e => e.TeacherId);
            entity.HasIndex(e => e.SubjectId);
            entity.HasIndex(e => e.ClassId);
            entity.HasIndex(e => new { e.LessonId, e.TeacherId, e.SubjectId, e.ClassId });
            entity.HasOne(e => e.Lesson)
                .WithMany(l => l.LessonAssignments)
                .HasForeignKey(e => e.LessonId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Teacher)
                .WithMany()
                .HasForeignKey(e => e.TeacherId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Subject)
                .WithMany()
                .HasForeignKey(e => e.SubjectId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Class)
                .WithMany()
                .HasForeignKey(e => e.ClassId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure ScheduledLessonRoomAssignment
        // Links LessonAssignments to specific rooms in scheduled lessons
        modelBuilder.Entity<ScheduledLessonRoomAssignment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ScheduledLessonRoomId);
            entity.HasIndex(e => e.LessonAssignmentId);
            entity.HasOne(e => e.ScheduledLessonRoom)
                .WithMany(slr => slr.RoomAssignments)
                .HasForeignKey(e => e.ScheduledLessonRoomId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.LessonAssignment)
                .WithMany(la => la.RoomAssignments)
                .HasForeignKey(e => e.LessonAssignmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
