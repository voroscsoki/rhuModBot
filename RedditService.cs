﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Reddit;
using Reddit.Controllers;
using Reddit.Controllers.EventArgs;

namespace rhuModBot
{
    public class RedditService
    {
        public static List<Post> postList = new List<Post>();
        public static DateTime startupTime = DateTime.UtcNow;
        public static async Task Initialise()
        {
            var reddit = new RedditClient(appId: Global.Config.RedditAppId, appSecret: Global.Config.RedditAppSecret, refreshToken: Global.Config.RefreshToken);
            string subredditName = "hungary";
            Console.WriteLine($"{DateTime.Now} - Sikeres csatlakozás\n{reddit.Account.Me.Name} | r/{subredditName}");
            var hungary = reddit.Subreddit(subredditName);
            postList = hungary.Posts.GetNew(limit: 100).OrderBy(p => p.Created).ToList();
            for (int i = 0; i < 1; i++)
            {
                var x = hungary.Posts.GetNew(limit: 100, after: postList.OrderBy(p => p.Created).First().Fullname);
                foreach (var y in x)
                {
                    postList.Add(y);
                }
            }
            postList = postList.Where(p => p.Created >= DateTime.Now.Date.AddHours(-2)).OrderBy(p => p.Created).ToList();
            foreach (var post in postList)
            {
                HandleNewPost(post, ref postList, true);
            }
            hungary.Posts.NewUpdated += C_NewPostsUpdated;
            hungary.Posts.MonitorNew();
        }
        public static void C_NewPostsUpdated(object sender, PostsUpdateEventArgs e)
        {
            foreach (var post in e.Added)
            {
                HandleNewPost(post, ref postList, false);
            }
        }
        public static void HandleNewPost(Post post, ref List<Post> postList, bool isStartup)
        {
            if (postList.Where(p => p.Id == post.Id).Count() == 0)
            {
                postList.Add(post);
            }
            if (post.Created >= startupTime || isStartup == true)
            {
                postList = postList.Where(p => p.Created >= DateTime.Now.Date.AddHours(-2)).OrderBy(p => p.Created).ToList();
                List<Post> userfilter = new List<Post>();
                userfilter = postList.Where(p => p.Author == post.Author && p.Created <= post.Created && p.Created >= Global.Config.RedditTimeOverride).ToList();
                int limit = 5;
                if (userfilter.Count() > limit)
                {
                    List<Comment> comments = post.Comments.GetComments();
                    if (Global.Config.RedditTesting == 0 && !post.Removed && comments.Where(p => p.Author == Global.Config.RedditUsername).Count() == 0)
                    {
                        post.Comment($"Posztodat töröltük, mivel átlépted a napi korlátot ({limit} poszt). Próbáld újra később!\n\n^(Én csak egy bot vagyok, az intézkedés automatikusan lett végrehajtva.)");
                        post.RemoveAsync();
                    }
                    Console.WriteLine($"{post.Author} {post.Id} azonosítójú posztja törölve lett. |{post.Created.ToLocalTime()} - {DateTime.Now}|");
                }
            }
            return;
        }
    }
}