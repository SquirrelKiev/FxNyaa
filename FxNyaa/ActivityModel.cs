using System.Text.Json.Serialization;

namespace FxNyaa;

// this entire thing really needs a better model
public record ActivityModel
{
    public record ActivityAccount
    {
        [JsonPropertyName("id")]
        public required ulong Id { get; init; }
        
        [JsonPropertyName("display_name")]
        public required string DisplayName { get; init; }
        
        [JsonPropertyName("username")]
        public required string Username { get; init; }
        
        [JsonPropertyName("acct")]
        public required string Acct { get; init; }
        
        [JsonPropertyName("url")]
        public required string Url { get; init; }
        
        [JsonPropertyName("uri")]
        public required string Uri { get; init; }
        
        [JsonPropertyName("created_at")]
        public required DateTimeOffset CreatedAt { get; init; }
        
        [JsonPropertyName("locked")]
        public bool Locked { get; init; }
        
        [JsonPropertyName("bot")]
        public bool Bot { get; init; }
        
        [JsonPropertyName("discoverable")]
        public bool Discoverable { get; init; } = true;
        
        [JsonPropertyName("indexable")]
        public bool Indexable { get; init; }
        
        [JsonPropertyName("group")]
        public bool Group { get; init; }
        
        [JsonPropertyName("avatar")]
        public required string Avatar { get; init; }
        
        [JsonPropertyName("avatar_static")]
        public required string AvatarStatic { get; init; }
        
        [JsonPropertyName("header")]
        public required string Header { get; init; }
        
        [JsonPropertyName("header_static")]
        public required string HeaderStatic { get; init; }
        
        [JsonPropertyName("followers_count")]
        public int FollowersCount { get; init; }
        
        [JsonPropertyName("following_count")]
        public int FollowingCount { get; init; }
        
        [JsonPropertyName("statuses_count")]
        public int StatusesCount { get; init; }
        
        [JsonPropertyName("hide_collections")]
        public bool HideCollections { get; init; }
        
        [JsonPropertyName("noindex")]
        public bool NoIndex { get; init; }
        
        [JsonPropertyName("emojis")]
        public object[] Emojis { get; init; } = [];
        
        [JsonPropertyName("roles")]
        public object[] Roles { get; init; } = [];
        
        [JsonPropertyName("fields")]
        public object[] Fields { get; init; } = [];
    }

    public record ActivityApplication
    {
        [JsonPropertyName("name")]
        public string Name { get; init; } = "Nyaa";
        
        [JsonPropertyName("website")]
        public string? Website { get; init; } = null;
    }

    public record ActivityMedia
    {
        [JsonPropertyName("id")]
        public required ulong Id { get; init; }
        
        [JsonPropertyName("type")]
        public string Type { get; init; } = "image";
        
        [JsonPropertyName("url")]
        public required string Url { get; init; }
        
        [JsonPropertyName("preview_url")]
        public string? PreviewUrl { get; init; }
        
        [JsonPropertyName("remove_url")]
        public string? RemoveUrl { get; init; }
        
        [JsonPropertyName("preview_remote_url")]
        public string? PreviewRemoteUrl { get; init; }
        
        [JsonPropertyName("text_url")]
        public string? TextUrl { get; init; }
        
        [JsonPropertyName("description")]
        public string? Description { get; init; }
        
        [JsonPropertyName("meta")]
        public Dictionary<string, ActivityMediaMeta> Meta { get; init; } = [];

        public record ActivityMediaMeta
        {
            [JsonPropertyName("width")]
            public int Width { get; init; } = 1280;
            
            [JsonPropertyName("height")]
            public int Height { get; init; } = 720;
            
            [JsonPropertyName("size")]
            public string Size => $"{Width}x{Height}";
            
            [JsonPropertyName("aspect")]
            public float Aspect => (float)Width / Height;
        }
    }

    [JsonPropertyName("id")]
    public required ulong Id { get; init; }
    
    [JsonPropertyName("url")]
    public required string Url { get; init; }
    
    [JsonPropertyName("uri")]
    public required string Uri { get; init; }
    
    [JsonPropertyName("created_at")]
    public required DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("edited_at")]
    public DateTimeOffset? EditedAt { get; init; } = null;
    
    [JsonPropertyName("reblog")]
    public object? Reblog { get; init; } = null;
    
    [JsonPropertyName("in_reply_to_account_id")]
    public ulong? InReplyToAccountId { get; init; } = null;
    
    [JsonPropertyName("language")]
    public string Language { get; init; } = "en";
    
    [JsonPropertyName("content")]
    public required string Content { get; init; }
    
    [JsonPropertyName("spoiler_text")]
    public string SpoilerText { get; init; } = "";
    
    [JsonPropertyName("visibility")]
    public string Visibility { get; init; } = "public";
    
    [JsonPropertyName("application")]
    public ActivityApplication Application { get; init; } = new();
    
    [JsonPropertyName("media_attachments")]
    public List<ActivityMedia> MediaAttachments { get; init; } = [];
    
    [JsonPropertyName("account")]
    public required ActivityAccount Account { get; init; }
    
    [JsonPropertyName("mentions")]
    public object[] Mentions { get; init; } = [];
    
    [JsonPropertyName("tags")]
    public object[] Tags { get; init; } = [];
    
    [JsonPropertyName("emojis")]
    public object[] Emojis { get; init; } = [];
    
    [JsonPropertyName("card")]
    public object? Card { get; init; } = null;
    
    [JsonPropertyName("poll")]
    public object? Poll { get; init; } = null;
}