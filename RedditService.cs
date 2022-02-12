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
using System.Text.RegularExpressions;

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
            string? linkedSite = null; string? articleTitle = null; string? titleUsedByPost = null;
            if (!post.Listing.IsSelf)
                linkedSite = ((LinkPost)post).URL;
            else
            {
                SelfPost textPost = (SelfPost)post;
                string intermediary = RegexFromContent(textPost);
                if ((double)intermediary.Length / textPost.Listing.SelfText.Length > 0.95)
                {
                    string regexMatchJustURL = @"\((\S)+\)";
                    var found = Regex.Match(intermediary, regexMatchJustURL);
                    linkedSite = found.Value.Replace("(", "").Replace(")", "");
                }
            }
            double similarity = 1;
            bool isPardoned = Global.Config.PardonedUsers.Contains(post.Author);
            bool isReported = false, domainIgnored = false;
            if (linkedSite != null && !isPardoned)
            {
                try
                {
                    var document = webGet.Load(linkedSite);
                    var testForExceptions = new Uri(linkedSite, UriKind.Absolute);
                    domainIgnored = Global.Config.ArticleTitleExceptions.Contains(testForExceptions.GetLeftPart(UriPartial.Authority).Replace(testForExceptions.GetLeftPart(UriPartial.Scheme), ""));
                    if (!domainIgnored)
                    {
                        titleUsedByPost = StringDistance.RemoveNonUTF8(post.Title);
                        articleTitle = System.Net.WebUtility.HtmlDecode(GetWebsiteTitle(document, titleUsedByPost));
                        similarity = StringDistance.CalculateSimilarity(titleUsedByPost.ToLower(), articleTitle.ToLower());
                    }
                }
                catch (Exception)
                { }
                if (articleTitle == null)
                    similarity = 0;
                Console.WriteLine($"{post.Created.ToUniversalTime()} (UTC) {post.Id} - cím ellenőrzés:\nsub: {post.Subreddit}\nposztbeli oldal címe: {articleTitle}\nposzt címe: {post.Title}\nhasonlóság: {similarity * 100}");
                isReported = similarity < Global.Config.Threshold && !domainIgnored;
                if (isReported && articleTitle != null && titleUsedByPost != null)
                {
                    post.Report("", "", "", false, "", $"szerkesztett cím? ({(int)((1 - similarity) * 100)}%)", "", "", "");
                    Console.WriteLine($"{post.Id} report queue-ba küldve");
                }
                Console.WriteLine("\n");
            }
            DbOperations.addPost(new DbPost(post.Id, post.Created, linkedSite, post.Title, articleTitle, similarity, isPardoned, domainIgnored, isReported));
            return;
        }
        public static string? GetWebsiteTitle(HtmlDocument doc, string compareTo)
        {
            string? result1 = null, result2 = null;
            try
            {
                result1 = StringDistance.RemoveNonUTF8(doc.DocumentNode.SelectSingleNode("html/head/title").InnerText);
                var titleNode = doc.DocumentNode.SelectNodes("//meta").First(p => p.Attributes.Where(q => q.Value == "og:title").Count() > 0);
                result2 = StringDistance.RemoveNonUTF8(titleNode.Attributes.First(p => p.Name == "content").Value);
            }
            catch (Exception)
            { }
            if (result1 != null && result2 != null)
            {
                return StringDistance.CalculateSimilarity(compareTo.ToLower(), result1.ToLower()) >
                    StringDistance.CalculateSimilarity(compareTo.ToLower(), result2.ToLower()) ? result1 : result2;
            }
            else return result1;
        }
        public static string? RegexFromContent(SelfPost post)
        {
            if (Uri.IsWellFormedUriString(post.Listing.SelfText, UriKind.Absolute))
                return post.Listing.SelfText;
            string regexMatch = @"\[(\S)+\]\((\S)+\)";
            var found = Regex.Match(post.Listing.SelfText, regexMatch);
            if (found.Value != null)
                return found.Value;
            return null;
        }
        
    }
}