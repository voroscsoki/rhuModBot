using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RedditSharp;
using RedditSharp.Things;

namespace rhuModBot
{
    public class RedditService
    {
        public static async Task Initialise()
        {
            CancellationTokenSource canceltokensource = new CancellationTokenSource(); CancellationToken canceltoken = canceltokensource.Token;
            var webAgent = new BotWebAgent(Global.Config.RedditUsername, Global.Config.RedditPW, Global.Config.RedditAppId, Global.Config.RedditAppSecret, "https://www.example.com");
            var reddit = new Reddit(webAgent, true);
            var subreddit = await reddit.GetSubredditAsync("/r/hungary");
            List<Post> subreddit_posts = await subreddit.GetPosts(Subreddit.Sort.New, max: 250).ToListAsync();
            subreddit_posts = subreddit_posts.Where(p => p.CreatedUTC >= DateTime.UtcNow.AddDays(-1)).OrderBy(p => p.CreatedUTC).ToList();
            ListingStream<RedditSharp.Things.Post> postStream = subreddit.GetPosts(Subreddit.Sort.New).Stream();
            foreach (var x in subreddit_posts)
            {
                HandleNewPost(x, ref subreddit_posts);
            }
            postStream.Subscribe(post => HandleNewPost(post, ref subreddit_posts));
            await postStream.Enumerate(canceltoken);
        }
        public static void HandleNewPost(Post post, ref List<Post> subreddit_posts)
        {
            if (subreddit_posts.Where(p => p.Id == post.Id).Count() == 0)
            {
                subreddit_posts.Add(post);
            }
            subreddit_posts = subreddit_posts.Where(p => p.CreatedUTC >= DateTime.UtcNow.AddDays(-1)).ToList();
            if (post.CreatedUTC > Global.Config.LatestChecked)
            {
                List<Post> userfilter = new List<Post>();
                userfilter = subreddit_posts.Where(p => p.AuthorName == post.AuthorName && p.CreatedUTC >= post.CreatedUTC.Date && p.CreatedUTC <= post.CreatedUTC && p.CreatedUTC >= Global.Config.RedditTimeOverride).ToList();
                int limit = 5;
                if (userfilter.Count() > limit)
                {
                    List<Comment> comments = post.GetCommentsAsync().Result;
                    if (Global.Config.RedditTesting == 0 && comments.Where(p => p.AuthorName == Global.Config.RedditUsername).Count() == 0)
                    {
                        post.CommentAsync($"Posztodat töröltük, mivel átlépted a napi korlátot ({limit} poszt). Próbáld újra később!\n\n^(Én csak egy bot vagyok, az intézkedés automatikusan lett végrehajtva.)");
                        post.RemoveAsync();
                    }
                    Console.WriteLine($"{post.AuthorName} {post.Id} azonosítójú posztja törölve lett. |{post.CreatedUTC} - {DateTime.UtcNow}|");
                }
                Global.Config.LatestChecked = post.CreatedUTC;
                File.WriteAllText(Global.ConfigFile, JsonConvert.SerializeObject(Global.Config, Formatting.Indented));
            }
            return;
        }
    }
}