using ECMS.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ECMS.Web.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, string>(options)
{
    public DbSet<Student> Students => Set<Student>();

    public DbSet<Teacher> Teachers => Set<Teacher>();

    public DbSet<CourseClass> Classes => Set<CourseClass>();

    public DbSet<StudentClass> StudentClasses => Set<StudentClass>();

    public DbSet<Room> Rooms => Set<Room>();

    public DbSet<Schedule> Schedules => Set<Schedule>();

    public DbSet<Attendance> Attendances => Set<Attendance>();

    public DbSet<ScoreType> ScoreTypes => Set<ScoreType>();

    public DbSet<Score> Scores => Set<Score>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        var utcDateTimeConverter = new ValueConverter<DateTime, DateTime>(
            value => value,
            value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

        builder.Entity<ApplicationUser>().ToTable("Users");
        builder.Entity<ApplicationRole>().ToTable("Roles");
        builder.Entity<IdentityUserRole<string>>().ToTable("UserRoles");
        builder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims");
        builder.Ignore<IdentityUserClaim<string>>();
        builder.Ignore<IdentityUserLogin<string>>();
        builder.Ignore<IdentityUserToken<string>>();

        builder.Entity<ApplicationUser>()
            .Property(user => user.FullName)
            .HasMaxLength(150);

        builder.Entity<Teacher>()
            .HasIndex(teacher => teacher.ApplicationUserId)
            .IsUnique()
            .HasFilter("[ApplicationUserId] IS NOT NULL");

        builder.Entity<Student>()
            .HasIndex(student => student.StudentCode)
            .IsUnique();

        builder.Entity<Student>()
            .HasIndex(student => student.ApplicationUserId)
            .IsUnique()
            .HasFilter("[ApplicationUserId] IS NOT NULL");

        builder.Entity<CourseClass>()
            .Property(courseClass => courseClass.Level)
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Entity<CourseClass>()
            .Property(courseClass => courseClass.Status)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Entity<Schedule>()
            .Property(schedule => schedule.Status)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Entity<Schedule>()
            .Property(schedule => schedule.StartAtUtc)
            .HasConversion(utcDateTimeConverter);

        builder.Entity<Schedule>()
            .Property(schedule => schedule.EndAtUtc)
            .HasConversion(utcDateTimeConverter);

        builder.Entity<Attendance>()
            .Property(attendance => attendance.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Entity<Score>()
            .Property(score => score.Value)
            .HasPrecision(4, 2);

        builder.Entity<StudentClass>()
            .HasKey(studentClass => new { studentClass.StudentId, studentClass.ClassId });

        builder.Entity<StudentClass>()
            .HasOne(studentClass => studentClass.Student)
            .WithMany(student => student.StudentClasses)
            .HasForeignKey(studentClass => studentClass.StudentId);

        builder.Entity<StudentClass>()
            .HasOne(studentClass => studentClass.Class)
            .WithMany(courseClass => courseClass.StudentClasses)
            .HasForeignKey(studentClass => studentClass.ClassId);

        builder.Entity<CourseClass>()
            .HasOne(courseClass => courseClass.Teacher)
            .WithMany(teacher => teacher.Classes)
            .HasForeignKey(courseClass => courseClass.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Schedule>()
            .HasOne(schedule => schedule.Class)
            .WithMany(courseClass => courseClass.Schedules)
            .HasForeignKey(schedule => schedule.ClassId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Schedule>()
            .HasOne(schedule => schedule.Teacher)
            .WithMany(teacher => teacher.Schedules)
            .HasForeignKey(schedule => schedule.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Schedule>()
            .HasOne(schedule => schedule.Room)
            .WithMany(room => room.Schedules)
            .HasForeignKey(schedule => schedule.RoomId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Schedule>()
            .HasIndex(schedule => new { schedule.StartAtUtc, schedule.EndAtUtc });

        builder.Entity<Schedule>()
            .HasIndex(schedule => new { schedule.TeacherId, schedule.StartAtUtc, schedule.EndAtUtc });

        builder.Entity<Attendance>()
            .HasIndex(attendance => new { attendance.ScheduleId, attendance.StudentId })
            .IsUnique();

        builder.Entity<ScoreType>()
            .HasIndex(scoreType => scoreType.Name)
            .IsUnique();

        builder.Entity<Score>()
            .HasIndex(score => new { score.StudentId, score.ClassId, score.ScoreTypeId })
            .IsUnique();

        builder.Entity<Score>()
            .HasOne(score => score.Teacher)
            .WithMany(teacher => teacher.ScoresRecorded)
            .HasForeignKey(score => score.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
