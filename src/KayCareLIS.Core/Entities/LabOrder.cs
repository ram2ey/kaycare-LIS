namespace KayCareLIS.Core.Entities;

public class LabOrder : TenantEntity
{
    public Guid  LabOrderId           { get; set; }
    public Guid  PatientId            { get; set; }
    public Guid? BillId               { get; set; }
    public Guid  OrderingDoctorUserId { get; set; }

    public string  Organisation { get; set; } = "DIRECT";
    public string  Status       { get; set; } = "Pending";
    public string? Notes        { get; set; }

    public Patient Patient        { get; set; } = null!;
    public Bill?   Bill           { get; set; }
    public User    OrderingDoctor { get; set; } = null!;

    public ICollection<LabOrderItem> Items { get; set; } = [];
}
