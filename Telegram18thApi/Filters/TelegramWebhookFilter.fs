namespace Telegram18thApi.Filters

open System
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc
open Microsoft.AspNetCore.Mvc.Filters
open Microsoft.Extensions.Options
open Telegram18thApi

[<Sealed>]
type TelegramWebhookFilter(configOpt: IOptions<TelegramSettings>) =
    let [<Literal>] HeaderKey = "X-Telegram-Bot-Api-Secret-Token"

    interface IActionFilter with
        member this.OnActionExecuted(_context) = ()
        member this.OnActionExecuting(context) =
            let secretToken = context.HttpContext.Request.Headers.TryGetValue(HeaderKey) |> Option.ofTry
            match secretToken with
            | Some secretToken when string secretToken = configOpt.Value.TokenSecret ->
                ()
            | _ ->
                context.Result <- ObjectResult(
                    {| Error = $"\"{HeaderKey}\" is invalid" |},
                    StatusCode = StatusCodes.Status403Forbidden)

[<Sealed>]
[<AttributeUsage(AttributeTargets.Method)>]
type SanitizeTelegramRequestAttribute() =
    inherit TypeFilterAttribute<TelegramWebhookFilter>()
