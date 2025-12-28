using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using System.Text.Json;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FxNyaa;

public class NyaaController(
    ILogger<NyaaController> logger,
    IOptions<FxNyaaConfig> fxNyaaConfig,
    HttpClient httpClient) : Controller
{
    [HttpGet("/view/{torrentId:int}/com-{commentId:int}")]
    [HttpGet("/view/{torrentId:int}/{commentId:int}")]
    public async Task<ActionResult<string>> NyaaCommentRequest(ulong torrentId, ulong commentId)
    {
        var nyaaInstanceUrl = fxNyaaConfig.Value.GetNyaaInstanceUrl(Request.Host.ToUriComponent());
        var address = $"{nyaaInstanceUrl}/view/{torrentId}";

        if (!HttpContext.Request.Headers.UserAgent
                .All(x => x != null && x.Contains("Discordbot")))
        {
            return Redirect($"{address}#com-{commentId}");
        }

        var response = await httpClient.GetAsync(address);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return View("NyaaCommentError", new CommentErrorModel
            {
                Error = "Unknown torrent :("
            });
        }

        // if it's not 200 or 404 something bad probably happened that's our fault 
        response.EnsureSuccessStatusCode();

        var htmlContent = await response.Content.ReadAsStringAsync();

        var document = await ParseHtmlDocumentAsync(htmlContent);
        var commentDataRes = GetCommentData(document, torrentId, commentId);

        if (!commentDataRes.IsSuccess)
        {
            // not sure what best practice is wrt returning errors
            return View("NyaaCommentError", new CommentErrorModel
            {
                Error = "Comment couldn't be found :("
            });
        }

        var commentData = commentDataRes.CommentData;
        var activityId = $"{torrentId:0000000000}{commentId:0000000000}";

        return View(nameof(NyaaCommentRequest), new CommentEmbedModel
        {
            ActivityId = activityId,
            Address = commentData.CommentUri.AbsoluteUri,
            Author = commentData.Author,
            CommentContent = commentData.Content,
            CommentId = commentId,
            TorrentId = torrentId,
            IconUrl = fxNyaaConfig.Value.IconUrl
        });
    }

    // fake mastodon instance
    [HttpGet("/api/v1/statuses/{id:length(20)}")]
    public async Task<ActionResult<string>> StatusRequest(string id)
    {
        if (!ulong.TryParse(id[..10], out var torrentId) || !ulong.TryParse(id[10..], out var commentId))
        {
            return BadRequest("Invalid ID.");
        }

        var nyaaInstanceUrl = fxNyaaConfig.Value.GetNyaaInstanceUrl(Request.Host.ToUriComponent());
        var address = $"{nyaaInstanceUrl}/view/{torrentId}";

        var response = await httpClient.GetAsync(address);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return BadRequest("Unknown torrent.");
        }

        // if it's not 200 or 404 something bad probably happened that's our fault 
        response.EnsureSuccessStatusCode();

        var htmlContent = await response.Content.ReadAsStringAsync();

        var document = await ParseHtmlDocumentAsync(htmlContent);
        var torrentData = GetTorrentData(document, torrentId);
        var commentDataRes = GetCommentData(document, torrentId, commentId);

        if (!commentDataRes.IsSuccess)
        {
            return BadRequest($"Comment couldn't be found or is invalid.");
        }

        var commentData = commentDataRes.CommentData;

        var transformedContent = new StringBuilder();

        // have to use discord's own conversion from <a> links to markdown here, as discord will not parse normal markdown
        // this breaks however when the text being hyperlinked has square brackets in
        // \[ and \] doesn't fix this, so figured it'd just be easier to replace the brackets with something similar
        transformedContent.Append($"\u21a9 <strong><a href=\"{address}\">{torrentData.Title
            .Replace("[", "［")
            .Replace("]", "］")}</a></strong>\n");

        var content = DiscordFlavouredMarkdown.ParseMarkdown(commentData.Content, out var imageUrls);

        transformedContent.Append(content);

        // var accountUrl = $"https://{fxNyaaConfig.Value.NyaaInstanceUrl}/user/{author}";
        var model = new ActivityModel
        {
            Id = torrentId,
            Url = commentData.CommentUri.AbsoluteUri,
            Uri = commentData.CommentUri.AbsoluteUri,
            CreatedAt = commentData.PostedAt,
            Content = CompiledRegex.NewLineBlocksRegex().Replace(transformedContent.ToString(), "<br/>"),
            Account = new ActivityModel.ActivityAccount
            {
                Id = 0,
                DisplayName = commentData.Author,
                Username = commentData.Author,
                Acct = commentData.Author,
                Url = commentData.CommentUri.AbsoluteUri,
                Uri = commentData.CommentUri.AbsoluteUri,
                CreatedAt = DateTimeOffset.Now,
                Avatar = commentData.AvatarUri.AbsoluteUri,
                AvatarStatic = commentData.AvatarUri.AbsoluteUri,
                Header = commentData.AvatarUri.AbsoluteUri,
                HeaderStatic = commentData.AvatarUri.AbsoluteUri
            },
            MediaAttachments = imageUrls.Select(x => new ActivityModel.ActivityMedia
            {
                Id = 0,
                Url = x.Url,
                Description = x.AltText
            }).ToList()
        };
        var json = JsonSerializer.Serialize(model);

        return Content(json, "application/json");
    }

    [HttpGet("/view/{torrentId:int}")]
    public ActionResult<string> NyaaTorrentRequest(ulong torrentId)
    {
        return View(nameof(NyaaTorrentRequest));
    }

    private static TorrentData GetTorrentData(IDocument document, ulong torrentId)
    {
        // there's three h3.panel-title elements, luckily the first will be the torrent title
        // alternative is parsing the <title> in the head
        var torrentTitleElement = document.QuerySelector($"h3.panel-title");
        if (torrentTitleElement is null)
        {
            throw new InvalidOperationException("Torrent title element couldn't be found?");
        }

        var torrentTitle = torrentTitleElement.TextContent.Trim();

        return new TorrentData
        {
            Title = torrentTitle
        };
    }

    private CommentDataResult GetCommentData(IDocument document, ulong torrentId, ulong commentId)
    {
        var nyaaInstanceUrl = fxNyaaConfig.Value.GetNyaaInstanceUrl(Request.Host.ToUriComponent());
        var commentUri = new Uri($"{nyaaInstanceUrl}/view/{torrentId}#com-{commentId}");

        var commentElement = document.QuerySelector($"#com-{commentId}");
        if (commentElement is null)
        {
            logger.LogInformation("Comment element with id 'com-{commentId}' not found.", commentId);
            return CommentDataResult.FromError();
        }

        var contentElement = commentElement.QuerySelector("div.comment-content");
        if (contentElement is null || string.IsNullOrWhiteSpace(contentElement.TextContent))
        {
            logger.LogWarning("The comment content couldn't be found or is empty.");
            return CommentDataResult.FromError();
        }

        var content = contentElement.TextContent.Trim();

        var authorElement = commentElement.QuerySelector("a[title]");
        if (authorElement is null || string.IsNullOrWhiteSpace(authorElement.TextContent))
        {
            logger.LogWarning("The author information couldn't be found or is empty.");
            return CommentDataResult.FromError();
        }

        var author = authorElement.TextContent.Trim();

        var avatarElement = commentElement.QuerySelector("img.avatar");
        if (avatarElement is null || !avatarElement.HasAttribute("src"))
        {
            logger.LogWarning("The avatar image couldn't be found.");
            return CommentDataResult.FromError();
        }

        var avatarSrc = avatarElement.GetAttribute("src")?.Trim();
        if (string.IsNullOrWhiteSpace(avatarSrc))
        {
            logger.LogWarning("The avatar's src is empty.");
            return CommentDataResult.FromError();
        }

        var avatarUri = new Uri(new Uri(nyaaInstanceUrl), avatarSrc);

        var timestampElement = commentElement.QuerySelector(".comment-details small");
        if (timestampElement is null || !timestampElement.HasAttribute("data-timestamp"))
        {
            logger.LogWarning("The comment timestamp couldn't be retrieved.");
            return CommentDataResult.FromError();
        }

        var timestampText = timestampElement.GetAttribute("data-timestamp")?.Trim();
        if (!long.TryParse(timestampText, out var postedAtNumber))
        {
            logger.LogWarning("The comment timestamp isn't in the expected format.");
            return CommentDataResult.FromError();
        }

        var postedAt = DateTimeOffset.FromUnixTimeSeconds(postedAtNumber);

        return CommentDataResult.FromSuccess(new CommentData
        {
            CommentUri = commentUri,
            Content = content,
            Author = author,
            AvatarUri = avatarUri,
            PostedAt = postedAt
        });
    }

    private async Task<IDocument> ParseHtmlDocumentAsync(string htmlContent)
    {
        var parser = new HtmlParser();
        var document = await parser.ParseDocumentAsync(htmlContent);

        return document;
    }
}

public record TorrentData
{
    public required string Title;
}

public record CommentData
{
    public required Uri CommentUri;
    public required string Content;
    public required string Author;
    public required Uri AvatarUri;
    public required DateTimeOffset PostedAt;
}

public readonly record struct CommentDataResult
{
    [MemberNotNullWhen(true, nameof(CommentData))]
    public bool IsSuccess { get; init; }

    public CommentData? CommentData { get; init; }

    public static CommentDataResult FromSuccess(CommentData data)
    {
        return new CommentDataResult()
        {
            IsSuccess = true,
            CommentData = data
        };
    }

    public static CommentDataResult FromError()
    {
        return new CommentDataResult()
        {
            IsSuccess = false
        };
    }
}