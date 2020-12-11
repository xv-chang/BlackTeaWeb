﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace BlackTeaWeb
{
    public class RecruitTeammateInfo
    {
        public long senderId;
        public string content;
    }

    public class RecruitInfo
    {
        public ObjectId id;
        public long senderId;

        public string desc;
        public long timestamp;

        public List<RecruitTeammateInfo> confirmedLst;

        public int requiredCount;

        public RecruitInfo()
        {
            confirmedLst = new List<RecruitTeammateInfo>();
        }

        public override string ToString()
        {
            return $"【{timestamp}】[{desc}]";
        }
        
        public string GetConnectId()
        {
            return timestamp.ToString();
        }

        public string GetTimeStr()
        {
            var dateTime = new DateTime(timestamp);
            var pastDay = DateTime.Now.Day - dateTime.Day;

            var str = "今天";
            if (pastDay == 0)
            {
                str = "今天";
            }
            else
            {
                str = $"{pastDay}天前";
            }
            return str;
        }
    }

    public static class GW2Recruit
    {
        private static string webRoot;

        public static void Init(string webRoot)
        {
            GW2Recruit.webRoot = webRoot;
        }

        public static int GetRecruitLstCount()
        {
            var lst = GetRecruitLst();
            var count = 0;
            if (lst != null)
            {
                count = lst.Count;
            }

            return count;
        }

        public static List<RecruitInfo> GetRecruitLst()
        {
            try
            {
                var db = MongoDbHelper.GetDb();
                var recruits = db.GetCollection<RecruitInfo>("recruits").AsQueryable().ToList();
                return recruits;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return null;
        }

        public static List<int> GetRandomIndex(int idxCount, int count, Random random)
        {
            List<int> lst = new List<int>();
            for (int i = 0; i < idxCount; i++)
            {
                lst.Add(i);
            }

            if (count > lst.Count)
            {
                return lst;
            }

            List<int> retLst = new List<int>();
            for (int i = 0; i < count; i++)
            {
                int idx = random.Next(lst.Count);
                retLst.Add(lst[idx]);
                lst.RemoveAt(idx);
            }

            return retLst;
        }


        public static List<T> GetRandomListItemListNoSync<T>(List<T> objectList, int count, Random random)
        {
            var idxLst = GetRandomIndex(objectList.Count, count, random);
            List<T> retLst = new List<T>();
            for (int i = 0; i < idxLst.Count; i++)
            {
                var idx = idxLst[i];
                retLst.Add(objectList[idx]);
            }

            return retLst;
        }

        public static RecruitInfo IsRecruiting(long senderId)
        {
            try
            {
                var db = MongoDbHelper.GetDb();

                return db.GetCollection<RecruitInfo>("recruits").Find(x => x.senderId == senderId).FirstOrDefault();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return null;
        }

        public static bool InsertRecruit(long senderId, int count, string rawMessage)
        {
            try
            {
                var curRecruiting = IsRecruiting(senderId);
                if (curRecruiting == null)
                {
                    var db = MongoDbHelper.GetDb();
                    db.GetCollection<RecruitInfo>("recruits")
                        .InsertOne(new RecruitInfo() { senderId = senderId, desc = rawMessage, requiredCount = count, timestamp = DateTime.Now.Ticks });
                }
                else
                {
                    var db = MongoDbHelper.GetDb();
                    var filter = Builders<RecruitInfo>.Filter;
                    var update = Builders<RecruitInfo>.Update;

                    db.GetCollection<RecruitInfo>("recruits").UpdateOne(filter.Eq("id", curRecruiting.id), update.Set("desc", rawMessage).Set("timestamp", DateTime.Now.Ticks));
                }

                return true;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return false;
        }

        public static RecruitInfo GetRecruitInfo(long id)
        {
            try
            {
                var db = MongoDbHelper.GetDb();

                //return recruitLst.Find((info) => { return info.id == id; });

                return db.GetCollection<RecruitInfo>("recruits").Find(x => x.timestamp == id).FirstOrDefault();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return null;
        }

        public static int GetTodayRecruitLstCount()
        {
            var recruits = GetRecruitLst();

            if (recruits != null)
            {
                var filterLst = new List<RecruitInfo>();
                foreach (var info in recruits)
                {
                    if (DateTime.Today.Ticks < info.timestamp)
                    {
                        filterLst.Add(info);
                    }
                }

                return filterLst.Count;
            }

            return 0;
        }

        public static bool DeleteRecruitInfo(long senderId)
        {
            var db = MongoDbHelper.GetDb();
            var collection = db.GetCollection<RecruitInfo>("recruits");
            var filter = Builders<RecruitInfo>.Filter;

            var result = collection.DeleteOne(filter.Eq("senderId", senderId));
            return result.DeletedCount > 0;
        }

        public static bool ForceDeleteRecruitInfo(long connectId)
        {
            var db = MongoDbHelper.GetDb();
            var collection = db.GetCollection<RecruitInfo>("recruits");
            var filter = Builders<RecruitInfo>.Filter;

            var result = collection.DeleteOne(filter.Eq("timestamp", connectId));
            return result.DeletedCount > 0;
        }

        public static bool TeammateJoin(long infoId, long senderId, string content)
        {
            var info = GetRecruitInfo(infoId);

            if (info == null)
                return false;

            return TeammateJoin(info, senderId, content);
        }

        public static bool TeammateJoin(RecruitInfo info, long senderId, string content)
        {
            var teammateInfo = info.confirmedLst.Find((info) => { return info.senderId == senderId; });

            var db = MongoDbHelper.GetDb();
            var filter = Builders<RecruitInfo>.Filter;
            var update = Builders<RecruitInfo>.Update;

            if (teammateInfo == null)
            {
                teammateInfo = new RecruitTeammateInfo() { senderId = senderId, content = content };
                info.confirmedLst.Add(teammateInfo);
            }
            else
            {
                teammateInfo.content = content;
            }

            db.GetCollection<RecruitInfo>("recruits").UpdateOne(filter.Eq("id", info.id), update.Set("confirmedLst", info.confirmedLst));

            return true;
        }

        public static RecruitInfo GetRecruitInfoByQQ(long senderId)
        {
            try
            {
                var db = MongoDbHelper.GetDb();

                //return recruitLst.Find((info) => { return info.id == id; });

                return db.GetCollection<RecruitInfo>("recruits").Find(x => x.senderId == senderId).FirstOrDefault();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return null;
        }

        public static bool DeleteTeammate(long senderId, long deleteId)
        {
            var recruitInfo = GetRecruitInfoByQQ(senderId);

            var teammateInfo = recruitInfo.confirmedLst.Find((info) => { return info.senderId == deleteId; });

            var db = MongoDbHelper.GetDb();
            var filter = Builders<RecruitInfo>.Filter;
            var update = Builders<RecruitInfo>.Update;

            if (teammateInfo == null)
            {
                return false;
            }

            recruitInfo.confirmedLst.Remove(teammateInfo);

            db.GetCollection<RecruitInfo>("recruits").UpdateOne(filter.Eq("id", recruitInfo.id), update.Set("confirmedLst", recruitInfo.confirmedLst));

            return true;
           
        }
    }
}
