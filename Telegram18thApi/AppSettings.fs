namespace Telegram18thApi

[<CLIMutable>]
type AppSettings = {
    Minio: MinioSettings
    Elastic: ElasticSettings
    Spotify: SpotifySettings
}

and [<CLIMutable>] MinioSettings = {
    Endpoint: string
    AccessKey: string
    SecretKey: string
}

and [<CLIMutable>] ElasticSettings = {
    Prefix: string
    Node: string
}

and [<CLIMutable>] SpotifySettings = {
    ClientId: string
    ClientSecret: string
    RedirectUri: string
}
