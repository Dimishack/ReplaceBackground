using MessagePack;

namespace ReplaceBackground.Models
{
    [MessagePackObject]
    public class Setting
    {
        [Key(0)]
        public DateOnly DateReplaced { get; set; } = DateOnly.FromDateTime(DateTime.Today);
        [Key(1)]
        public string Interval { get; set; } = "День";
        [Key(2)]
        public string Season { get; set; } = "Winter";

    }
}
