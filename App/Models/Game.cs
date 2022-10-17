using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Reservator.Models;

public class Game
{
    public int GameId { get; set; }
    public ulong ReservationMessageId { get; set; }
    public ulong ReactionsAlliesMessageId { get; set; }
    public ulong ReactionsAxisMessageId { get; set; }
    public ulong ReactionsOtherMessageId { get; set; }
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    public ulong ChannelId { get; set; }
    public ulong GuildId { get; set; }
}