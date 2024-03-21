namespace Telegram18thApi

[<CLIMutable>]
type AppSettings = {
    Minio: MinioSettings
    Elastic: ElasticSettings
    Spotify: SpotifySettings
    Telegram: TelegramSettings
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

and [<CLIMutable>] TelegramSettings = {
    TokenSecret: string
    Token: string
    Webhook: string
}
