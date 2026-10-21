namespace PrivilegeDesk.Api.Models;

// Stored in SQL Server as integers (0, 1, 2, ...).
// Using enums in C# makes the rest of the code easier to read than magic numbers.
public enum UserRole
{
    Employee = 0,
    Approver = 1,
    Admin = 2
}

public enum RequestStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Expired = 3
}
