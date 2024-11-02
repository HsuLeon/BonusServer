
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;

namespace FunLobbyUtils.Database.Schema
{
    [BsonIgnoreExtraElements]
    public class UserFreeScore
    {
        public ObjectId _id { get; set; }
        public string User { get; set; }
        public SCORE_TYPE ScoreType { get; set; }
        public int Scores { get; set; }
        public string Note { get; set; }
        public DateTime CreateTime { get; set; }

        public UserFreeScore()
        {
            _id = ObjectId.Empty;
            User = "";
            ScoreType = SCORE_TYPE.Formal;
            Scores = 0;
            Note = "";
            CreateTime = DateTime.UtcNow;
        }

        public static UserFreeScore FromJson(JObject obj)
        {
            UserFreeScore freeScores = null;
            if (obj != null)
            {
                try
                {
                    freeScores = new UserFreeScore();
                    freeScores._id = ObjectId.Parse(obj["_id"].Value<string>());
                    freeScores.User = obj.ContainsKey("User") ? obj["User"].Value<string>() : "";
                    freeScores.ScoreType = obj.ContainsKey("ScoreType") ? (SCORE_TYPE)obj["ScoreType"].Value<int>() : SCORE_TYPE.Formal;
                    freeScores.Scores = obj.ContainsKey("Scores") ? obj["Scores"].Value<int>() : 0;
                    freeScores.Note = obj.ContainsKey("Note") ? obj["Note"].Value<string>() : "";
                    freeScores.CreateTime = obj.ContainsKey("CreateTime") ? Convert.ToDateTime(obj["CreateTime"]) : DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    freeScores = null;
                }
            }
            return freeScores;
        }

        public string GetObjectId()
        {
            return this._id.ToString();
        }
    }
}
