using System.ComponentModel.DataAnnotations;

namespace DiscountsService.Persistence;

public class DiscountCode
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    [StringLength(maximumLength: 8, MinimumLength = 7)]
    public string Code { get; set; } = string.Empty;
    
    public bool Used { get; set; } = false;
    
    public DateTime? UsedAt { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }
}
