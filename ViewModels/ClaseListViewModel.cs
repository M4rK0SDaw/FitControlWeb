namespace FitControlWeb.ViewModels;

public class ClaseListViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public DateOnly Fecha { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }
    public int CapacidadMaxima { get; set; }
    public string Entrenador { get; set; } = string.Empty;
    public string Especialidad { get; set; } = string.Empty;

    public int PlazasOcupadas { get; set; }

    public bool Completa { get; set; }

    public bool YaReservada { get; set; }

    public bool EsPasada { get; set; }

    public bool ClienteTieneSuscripcionActiva { get; set; }

}