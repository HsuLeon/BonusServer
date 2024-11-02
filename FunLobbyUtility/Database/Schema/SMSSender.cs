using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FunLobbyUtils.Database.Schema
{
    [BsonIgnoreExtraElements]
    public class SMSSender
    {
        public ObjectId _id { get; set; }
        public string IP { get; set; }
        public int SnedCnt { get; set; }
        DateTime mCreateDate;
        public DateTime CreateDate
        {
            get { return mCreateDate.ToLocalTime(); }
            set { mCreateDate = value.ToUniversalTime(); }
        }
        DateTime mLastUpdateDate;
        public DateTime LastUpdateDate
        {
            get { return mLastUpdateDate.ToLocalTime(); }
            set { mLastUpdateDate = value.ToUniversalTime(); }
        }

        public SMSSender()
        {
            this.IP = "";
            this.SnedCnt = 1;
            this.mCreateDate = DateTime.Now;
            this.mLastUpdateDate = DateTime.Now;
        }
    }
}
