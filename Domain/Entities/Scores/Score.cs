using Common;
using Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Domain.Entities.Scores
{
    [Table("scores")]
    public class Score : BaseEntity
    {
        public long UserId { get; set; }
        public int Amount { get; set; } = 0;
        public string? Description { get; set; }
        public ScoreTypeEnum Type { get; set; } = ScoreTypeEnum.Deposit;
        public string? Key { get; set; }
        public long? ReferenceId { get; set; }

        public string Code { get; set; }
    }
}
