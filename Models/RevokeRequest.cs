using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace studious_enigma.Models
{
    public class RevokeRequest
    {
        [Required]
        public string RefreshToken { get; set; }
    }
}