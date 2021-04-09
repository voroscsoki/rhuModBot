using System;
using System.Threading.Tasks;
using System.Linq;
using RedditSharp;
using RedditSharp.Things;
using System.Threading;
using System.Collections.Generic;

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
            var subreddit_posts = await subreddit.GetPosts(Subreddit.Sort.New, max: 250).ToListAsync();
            subreddit_posts = subreddit_posts.OrderBy(p => p.CreatedUTC).ToList();
            ListingStream<RedditSharp.Things.Post> postStream = subreddit.GetPosts(Subreddit.Sort.New).Stream();
            foreach (var x in subreddit_posts)
            {
                HandleNewPost(x, ref subreddit_posts);
            }
            postStream.Subscribe(post => HandleNewPost(post, ref subreddit_posts));
            await postStream.Enumerate(canceltoken);
        }
        public static void HandleNewPost(RedditSharp.Things.Post post, ref List<RedditSharp.Things.Post> subreddit_posts)
        {
            if (!subreddit_posts.Contains(post))
            {
                subreddit_posts.Add(post);
            }
            var userfilter = subreddit_posts.Where(p => p.AuthorName == post.AuthorName && p.CreatedUTC >= post.CreatedUTC.Date && p.CreatedUTC <= post.CreatedUTC && p.CreatedUTC >= Global.Config.RedditTimeOverride);
            if (userfilter.Count() > 5)
            {
                if (Global.Config.RedditTesting == 0)
                {
                    post.CommentAsync("Posztodat töröltük, mivel átlépted a 24 órás korlátod. Próbáld újra később!\n\n^(Én csak egy bot vagyok, az intézkedés automatikusan lett végrehajtva.)");
                    post.DelAsync();
                }
                subreddit_posts.Remove(post);
                Console.WriteLine($"{post.AuthorName} {post.Id} azonosítójú posztja törölve lett. |{post.CreatedUTC} - {DateTime.UtcNow}|");
            }
        }
    }
}