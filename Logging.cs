using Microsoft.EntityFrameworkCore;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rhuModBot
{
    public class PostContext : DbContext
    {
        public DbSet<DbPost> Posts { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite("Data Source=posts.db");
    }
    public class DbPost
    {
        public DbPost(string id, DateTime time, string linkedArticle, string postTitle, string articleTitle, double similarity, bool userIsIgnored, bool domainIsIgnored, bool isReported)
        {
            this.id = id;
            this.time = time;
            this.linkedArticle = linkedArticle;
            this.postTitle = postTitle;
            this.articleTitle = articleTitle;
            this.similarity = similarity;
            this.userIsIgnored = userIsIgnored;
            this.domainIsIgnored = domainIsIgnored;
            this.isReported = isReported;
        }
        [Key]
        public int key { get; set; }
        public string id { get; set; }
        public DateTime time { get; set; }
        public string? linkedArticle { get; set; }
        public string postTitle { get; set; }
        public string? articleTitle { get; set; }
        public double similarity { get; set; }
        public bool userIsIgnored { get; set; }
        public bool domainIsIgnored { get; set; }
        public bool isReported { get; set; }
    }
    public static class DbOperations
    {
        public static bool entryExistsInDB(DbPost test)
        {
            using (var db = new PostContext())
            {
                return db.Posts.Where(p => p.id == test.id).Count() > 0;
            }
        }
        public static void addPost(DbPost toAdd)
        {
            using (var db = new PostContext())
            {
                if (!entryExistsInDB(toAdd))
                    db.Posts.Add(toAdd);
                db.SaveChanges();
            }
        }
        public static List<DbPost> findRecentURL(string url, string excludeID)
        {
            using (var db = new PostContext())
            {
                var res =  db.Posts.Where(p => p.linkedArticle == url && p.id != excludeID && p.time >= DateTime.Now.AddDays(-3)).ToList();
                foreach (var item in res)
                {
                    if (RedditService.reddit.Post($"t3_{item.id}").Removed)
                        res.Remove(item);
                }
                return res;
            }
        }
    }
}
