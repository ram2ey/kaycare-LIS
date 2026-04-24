namespace KayCareLIS.Core.DTOs.Radiology;

public class CreateRadiologyOrderRequest
{
    public Guid        PatientId         { get; set; }
    public Guid?       BillId            { get; set; }
    public string      Priority          { get; set; } = "Routine";
    public string?     ClinicalIndication { get; set; }
    public string?     Notes             { get; set; }
    public List<Guid>  ProcedureIds      { get; set; } = [];
}
