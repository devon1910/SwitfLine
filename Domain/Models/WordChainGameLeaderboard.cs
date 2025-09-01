using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models
{
    public class WordChainGameLeaderboard
    {
        [Key]
        public long RefId { get; set; }
        public string UserId { get; set; }

        public int HighestScore { get; set; }
       

        public int Level { get; set; }

        [ForeignKey("UserId")]
        public SwiftLineUser SwiftLineUser { get; set; }

        [NotMapped]
        public string Username { get; set; }

        [NotMapped]
        public int Rank { get; set; }
    }
}
