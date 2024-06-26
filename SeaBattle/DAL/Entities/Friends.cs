﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameDBContext.Entities
{
    public class Friend
    {
        [Key]
        public int Id { get; set; }

        public int? User1Id { get; set; }
        [ForeignKey("User1Id")]
        public virtual User? User1 { get; set; }

        public int? User2Id { get; set; }
        [ForeignKey("User2Id")]
        public virtual User? User2 { get; set; }
    }
}
