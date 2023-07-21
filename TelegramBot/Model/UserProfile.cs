using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramBot.Model
{
    public class UserProfile
    {
        public string Telegram { get; set; }
        public string City { get; set; }
        public string Email { get; set; }
        public string Fio { get; set; }
        public string Phone { get; set; }
        public List<string> Skills { get; set; } = new List<string>();
        public string Experience { get; set; }
        public string Education { get; set; }
        public string Purpose { get; set; }
    }
}
