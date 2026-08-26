using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZT.Cloud.MesModel.Models
{
    public class ClassConfigure:ZTBaseFrame.ClassBaseXMLCommon
    {
        public bool IsNetWorking = false;
        public bool EnableSound = false;

        public int LineID = 0;
        public string LineName = "";

        public int ProcID = 0;
        public string ProcesName = "";

        public int EqptID = 0;


        public int WorkerID = 0;
        public string WorkerName = "";
        public string WorkerCardSN = "";

    }
}
