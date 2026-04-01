namespace MULTITUDE.Class.Facility
{
    internal enum DropRequestType
    {
        SimpleClueReference
    }

    internal class DropRequest
    {
        public DropRequest(DropRequestType requestType, object data)
        {
            Type = requestType;
            Data = data;
        }

        public object Data { get; set; }
        public DropRequestType Type { get; set; }

        #region Static Members
        public static readonly string DropRequestDropDataFormatString = "DropRequestData";
        #endregion
    }
}
