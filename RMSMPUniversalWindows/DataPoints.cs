using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMSMPUniversalWindows
{
    class DataPoints
    {
        public DataPoints()
        {
            timeStamp = DateTime.UtcNow;
        }
        DateTime timeStamp { get; set; }
        string deviceId { get; set; }
        double returnAirTemp { get; set; }
        double returnAirHumidity { get; set; }
        double dischargeAirTemp { get; set; }
        double dischargeAirHumidity { get; set; }
        double suctionTemp { get; set; }
        double compressionTemp { get; set; }
        double condensorTemp { get; set; }
        double evaporatorTemp { get; set; }
        double fanCurrent { get; set; }
        double compressorCurrent { get; set; }

    }
}
