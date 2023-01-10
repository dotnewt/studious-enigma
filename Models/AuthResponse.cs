using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace studious_enigma.Models
{
    public class AuthResponse
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
    }
}