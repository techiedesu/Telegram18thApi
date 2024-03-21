namespace Telegram18thApi.Controllers

open System
open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Options
open Telegram.Bot
open Telegram.Bot.Types
open Telegram18thApi
open Telegram18thApi.Filters

[<Sealed>]
[<ApiController>]
type TelegramController(logger: ILogger<TelegramController>,
                        configOpt: IOptionsMonitor<AppSettings>,
                        trackSearchService: TrackSearchService,
                        tgClient: ITelegramBotClient,
                        userStore: UserStore) as this =
    inherit ControllerBase()
    let mutable config = configOpt.CurrentValue

    do
        %configOpt.OnChange(fun newConfig ->
            logger.LogDebug("Called update {newConfig}", newConfig)
            config <- newConfig)

    [<HttpGet("/ping")>]
    member _.Ping() = task {
        logger.LogInformation("Called ping")
        let! res = userStore.GetByTelegramId(6337379028L)
        return this.Ok(res)
    }

    [<HttpGet("/pong")>]
    member _.Pong() = task {
        logger.LogInformation("Called pong")
        let! res = userStore.Create({
            TelegramId = 6337379028L
            SpotifyToken = None
            CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            UpdatedAt = 6L
            SingleAuthToken = Some "notas"
            SpotifyRefreshToken = None
        })
        return this.Ok(res)
    }

    [<HttpGet("/track")>]
    member _.Track() = task {
        let! res = trackSearchService.Search(6337379028L, None)
        match res with
        | None ->
            return this.Ok("не удалось") :> IActionResult
        | Some res ->
            return this.File(res, "audio/aac")
    }

    [<SanitizeTelegramRequest>]
    [<HttpPost("/webhook")>]
    member _.Webhook(ctx: Update) = task {
        let! message =  tgClient.SendDiceAsync(ctx.Message.Chat.Id) :> Task
        return this.Ok(message)
    }
