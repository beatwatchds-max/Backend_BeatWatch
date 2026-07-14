using System.ComponentModel.DataAnnotations;

namespace BeatWatch_BackEnd.Configuration
{
    public class MongoDbSettings
    {
        [Required]
        public string ConnectionString { get; set; } = null!;

        [Required]
        public string DatabaseName { get; set; } = null!;
    }
}
