using System.ComponentModel.DataAnnotations.Schema;

namespace Reservator.Models;

public class Reservation
{
    public int ReservationId { get; set; }
    public Game Game { get; set; }
    public ulong User { get; set; }
    public string Country { get; set; }
}