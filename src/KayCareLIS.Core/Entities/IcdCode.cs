namespace KayCareLIS.Core.Entities;

public class IcdCode
{
    public int    IcdCodeId   { get; set; }
    public string Code        { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Chapter     { get; set; } = string.Empty;
}
