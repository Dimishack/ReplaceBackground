using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplaceBackground.Models
{
    internal class Setting
    {
        public DateTime DateReplaced { get; set; } = DateTime.Today;
        public string Interval { get; set; } = "Месяц";
        public string Season { get; set; } = GetSeason(DateTime.Today.Month);

        public static string GetSeason(int month) => month switch
        {
            12 or 1 or 2 => "Winter",
            3 or 4 or 5 => "Spring",
            6 or 7 or 8 => "Summer",
            9 or 10 or 11 => "Autumn",
            _ => throw new InvalidDataException($"Неизвестное число месяца {month}"),
        };
    }
}
