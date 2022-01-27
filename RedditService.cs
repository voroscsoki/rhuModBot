using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Reddit;
using Reddit.Controllers;
using Reddit.Controllers.EventArgs;
using HtmlAgilityPack;
using Reddit.Inputs.LinksAndComments;

namespace rhuModBot
{
    public class RedditService
    {
        public static List<Post> postList = new List<Post>();
        public static DateTime startupTime = DateTime.UtcNow;
        public static int TimeDiff = 2;
        public static string subredditName = "hungary";
        public static HtmlWeb webGet = new HtmlWeb();
        public static async Task Initialise()
        {
            var reddit = new RedditClient(appId: Global.Config.RedditAppId, appSecret: Global.Config.RedditAppSecret, refreshToken: Global.Config.RefreshToken);
            Console.WriteLine($"{DateTime.UtcNow.AddHours(TimeDiff)} - Sikeres csatlakozás\n{reddit.Account.Me.Name} | r/{subredditName}");
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
            postList = postList.Where(p => p.Created >= DateTime.UtcNow.AddHours(TimeDiff).Date.AddHours(-TimeDiff)).OrderBy(p => p.Created).ToList();
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
            //post limit
            if (post.Created >= startupTime || isStartup == true)
            {
                postList = postList.Where(p => p.Created >= DateTime.UtcNow.AddHours(TimeDiff).Date.AddHours(-TimeDiff)).OrderBy(p => p.Created).ToList();
                List<Post> userfilter = new List<Post>();
                userfilter = postList.Where(p => p.Author == post.Author && p.Created <= post.Created && p.Created >= Global.Config.RedditTimeOverride).ToList();
                int limit = 5;
                if (userfilter.Count() > limit)
                {
                    List<Comment> comments = post.Comments.GetComments();
                    if (Global.Config.RedditTesting == 0 && !post.Removed && comments.Where(p => p.Author == Global.Config.RedditUsername).Count() == 0)
                    {
                        post.Reply($"Posztodat töröltük, mivel átlépted a napi korlátot ({limit} poszt). Próbáld újra később!\n\n^(Én csak egy bot vagyok, az intézkedés automatikusan lett végrehajtva.)");
                        post.RemoveAsync();
                    }
                    Console.WriteLine($"{post.Author} {post.Id} azonosítójú posztja törölve lett. |{post.Created.AddHours(TimeDiff)} - {DateTime.UtcNow.AddHours(TimeDiff)}|");
                }
            }
            //lexical distance
            string? linkedSite = null; string? title = null; string? titleUsedByPost = null;
            if (!post.Listing.IsSelf)
                linkedSite = ((LinkPost)post).URL;
            else if(Uri.IsWellFormedUriString(post.Listing.SelfText, UriKind.Absolute))
                linkedSite = ((SelfPost)post).SelfText;
            double similarity = 1;
            if (linkedSite != null && !Global.Config.PardonedUsers.Contains(post.Author))
            {
                try
                {
                    var document = webGet.Load(linkedSite);
                    var testForExceptions = new Uri(linkedSite, UriKind.Absolute);
                    if (!(Global.Config.ArticleTitleExceptions.Contains(testForExceptions.GetLeftPart(UriPartial.Authority)))){
                        title = document.DocumentNode.SelectSingleNode("html/head/title").InnerText;
                        title = StringDistance.RemoveNonUTF8(title).ToLower();
                        titleUsedByPost = StringDistance.RemoveNonUTF8(post.Title).ToLower();
                        similarity = StringDistance.CalculateSimilarity(titleUsedByPost, title);
                    }
                }
                catch (Exception)
                {
                    //these are image posts (imgur or i.reddit), it's not needed to check them anyway
                    return;
                }
                
                Console.WriteLine($"{post.Created.ToUniversalTime()} (UTC) {post.Id} - cím ellenőrzés:\nsub: {post.Subreddit}\nposztbeli oldal címe: {title}\nposzt címe: {post.Title}\nhasonlóság: {similarity*100}");
                if (similarity < Global.Config.Threshold && title != null && titleUsedByPost != null)
                {
                    post.Report("", "", "", false, "", $"szerkesztett cím? ({(int)((1-similarity) * 100)}%)", "", "", "");
                    Console.WriteLine($"{post.Id} report queue-ba küldve");
                }   
                Console.WriteLine("\n");
            }
            return;
        }
    }
}