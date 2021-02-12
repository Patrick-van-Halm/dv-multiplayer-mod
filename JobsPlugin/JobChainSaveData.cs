namespace JobsPlugin
{
    internal class JobChainSaveData
    {
        public JobDefinitionDataBase[] jobChainData;
        public string[] TrainCarGuids { get; set; }
        public bool JobTaken { get; set; }
        public TaskSaveData[] currentJobTaskData;
        public string FirstJobId { get; set; }
    }
}
