namespace KayCareLIS.Core.DTOs.Appointments;

public class CalendarRequest
{
    public Guid?     DoctorUserId { get; set; }
    public DateTime? From        { get; set; }
    public DateTime? To          { get; set; }
    public string?   Status      { get; set; }
}
