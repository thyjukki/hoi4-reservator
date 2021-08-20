using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Reservator.Models
{
    public class GuildRoles
    {
        [Key, Column(Order = 0)]
        public ulong RoleId { get; set; }
        [Key, Column(Order = 1)]
        public ulong GuildId { get; set; }
        
        [Key, Column(Order = 2)]
        public string Type { get; set; }
    }
    
}