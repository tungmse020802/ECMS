namespace ECMS.Web.Authorization;

public static class ApplicationRoles
{
    public const string Admin = "Admin";
    public const string Staff = "Staff";
    public const string Teacher = "Teacher";
    public const string Student = "Student";

    public static readonly string[] All =
    [
        Admin,
        Staff,
        Teacher,
        Student
    ];

    public static readonly string[] AdminOrStaff =
    [
        Admin,
        Staff
    ];
}
