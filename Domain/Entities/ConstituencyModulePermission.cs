namespace Nirvachak_AI.Domain.Entities;

public class ConstituencyModulePermission
{
    public int Id { get; set; }
    public int ConstituencyId { get; set; }
    public Constituency? Constituency { get; set; }

    public string ModuleKey { get; set; } = string.Empty;
    public string SubModuleKey { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
