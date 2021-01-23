using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobsPlugin
{
    class JobChainSaveData
    {
        public JobDefinitionDataBase[] jobChainData;
        public string[] TrainCarGuids { get; set; }
        public bool JobTaken { get; set; }
        public TaskSaveData[] currentJobTaskData;
        public string FirstJobId { get; set; }
    }
}
