namespace CreditCardsSystem.Domain.Shared.Models.Account
{
    public class UserDto
    {
        public string KfhId { get; set; }

        public string? Name { get; set; }

        public string? NameAr { get; set; }

        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public string? Gender { get; set; }

        public string? Email { get; set; }

        public string? PhoneExtension { get; set; }

        public string? Phone { get; set; }

        public string? Title { get; set; }

        public string? TitleAr { get; set; }

        public string? DepartmentName { get; set; }

        public string? DepartmentNameAr { get; set; }

        public string? DepartmentId { get; set; }

        public string? GroupName { get; set; }

        public string? GroupNameAr { get; set; }

        public string? Location { get; set; }

        public int? DefaultBranchId { get; set; } = null!;
        public string? DefaultBranchName { get; set; } = null!;
    }
}
