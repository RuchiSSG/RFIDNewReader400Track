using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PoliceRecruitmentAPI.Core.ModelDtos
{
    public class Groundtestdata1
    {
        public string? ResulType { get; set; }
        public string? ApplicationNo   { get; set; }
        public long? CandidateID { get; set; }
        public string? FullNameDevnagari   { get; set; }
        public string? FullNameEnglish  { get; set; }
        public string? DOB  { get; set; }
        public string? Cast  { get; set; }
        public string? Category  { get; set; }
        public string? gender  { get; set; }
        public string? chestno  { get; set; }
        public long? HundredMeterRunning   { get; set; }
        public long? SixteenHundredMeterRunning { get; set; }
        public long? eightHundredMeterRunning   { get; set; }
        public long? ShotPut  { get; set; }
        public long? TotalScore { get; set; }
        public string? SequenceID { get; set; }

    }

    public class GroundTestCategory
    {
        //public string Category { get; set; }
        public List<Groundtestdata1> Items { get; set; }
    }

}
