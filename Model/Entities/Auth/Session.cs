namespace Model.Entities.Auth;

[Table("SESSIONS")]
public class Session {
    [Key]
    [Column("SESSION_ID")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Column("USER_ID")]
    public int UserId { get; set; }

    public User User { get; set; } = null!;

    [Required]
    [Column("CREATED_AT")]
    public DateTime CreatedAt { get; set; }

    [Required]
    [Column("VALID_UNTIL")]
    public DateTime ValidUntil { get; set; }

    [Required]
    [Column("TOKEN")]
    [StringLength(50)]
    public string Token { get; set; } = null!;
}