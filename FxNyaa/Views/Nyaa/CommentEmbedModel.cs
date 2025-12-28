namespace FxNyaa;

public class CommentEmbedModel
{
    public required ulong TorrentId { get; init; }
    public required ulong CommentId { get; init; }
    public required string Author { get; init; }
    public required string CommentContent { get; init; }
    public required string Address { get; init; }
    public required string ActivityId { get; init; }
    public required string IconUrl { get; init; }
}