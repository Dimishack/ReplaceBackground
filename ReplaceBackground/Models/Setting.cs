using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplaceBackground.Models
{
    internal class Setting
    {
        public DateTime DateReplaced { get; set; } = DateTime.Today;
        public string Interval { get; set; } = "Месяц";
    }
}
