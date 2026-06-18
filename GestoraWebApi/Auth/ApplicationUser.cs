using GestoraWebApi.Models;
using Microsoft.AspNetCore.Identity;

namespace GestoraWebApi.Auth
{
    public class ApplicationUser : IdentityUser
    {
        public virtual ICollection<Prenotazione> Prenotazioni { get; set; } = new List<Prenotazione>();
    }
}
