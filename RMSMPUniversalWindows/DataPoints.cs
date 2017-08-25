using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMSMPUniversalWindows
{
    public class DataPoints
    {
        public DataPoints()
        {
            timeStamp = DateTime.UtcNow;
            deviceId = Settings.deviceId;
        }
        public DateTime timeStamp { get; set; }
        public DateTime groupingStamp { get; set; }
        public string deviceId { get; set; }
        public double returnAirTemp { get; set; }
        public double returnAirHumidity { get; set; }
        public double dischargeAirTemp { get; set; }
        public double dischargeAirHumidity { get; set; }
        public double suctionTemp { get; set; }
        public double compressionTemp { get; set; }
        public double condensorTemp { get; set; }
        public double evaporatorTemp { get; set; }
        public double fanCurrent { get; set; }
        public double compressorCurrent { get; set; }

        public int sourceCount { get; set; }

        public DataPoints(List<DataPoints> list)
        {
            this.timeStamp = list.FirstOrDefault().timeStamp;
            this.groupingStamp = groupingStamp.AddMilliseconds(Settings.uploadInterval / 2);
            this.returnAirHumidity = list.Average(x => x.returnAirHumidity);
            this.returnAirTemp = list.Average(x => x.returnAirTemp);
            this.sourceCount = list.Count();
        }

    }
}
