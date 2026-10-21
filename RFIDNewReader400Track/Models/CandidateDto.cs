using RFIDReaderPortal.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace PoliceRecruitmentAPI.Core.ModelDtos
{
	public class CandidateDto
	{
        public BaseModel? BaseModel { get; set; }
        public string? UserId { get; set; }
        public string? RecruitId { get; set; }
        public string? EventId { get; set; }
        public string? Category { get; set; }
        public List<Groundtestdata1>? Groundtestdata1 { get; set; }

     }
    

}
