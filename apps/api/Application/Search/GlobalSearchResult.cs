using api.Application.Appointments;
using api.Application.Backlogs;
using api.Application.Patients;

namespace api.Application.Search;

public class GlobalSearchResult
{
    public List<PatientDto> Patients { get; set; } = [];

    public List<AppointmentDto> Appointments { get; set; } = [];

    public List<BacklogDto> Backlogs { get; set; } = [];
}
