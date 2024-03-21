namespace Telegram18thApi.Controllers

open System
open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Options
open SpotifyAPI.Web
open Telegram18thApi

[<Sealed>]
[<ApiController>]
type SpotifyController(config: IOptions<SpotifySettings>,
                       logger: ILogger<SpotifyController>,
                       userStore: UserStore) as this =
    inherit ControllerBase()

    [<HttpGet("/spotify/{userToken}/authorize")>]
    member _.Authorize(userToken: string) = task {
        match! userStore.GetBySingleAuthToken(userToken) with
        | None ->
            return this.BadRequest("Invalid token. Try reauthorize in telegram bot.") :> IActionResult
        | Some _user ->
            let loginRequest = LoginRequest(Uri(config.Value.RedirectUri), config.Value.ClientId, LoginRequest.ResponseType.Code,
                State = userToken,
                Scope = [|
                    Scopes.UserReadPlaybackState
                |])

            return this.Redirect(loginRequest.ToUri().ToString())
    }

    [<HttpGet("/spotify/callback")>]
    member _.Callback([<FromQuery>] code: string, [<FromQuery>] state: string) = task {
        match! userStore.GetBySingleAuthToken(state) with
        | None ->
            return this.BadRequest("Invalid token. Try reauthorize in telegram bot.") :> IActionResult
        | Some user ->
            let! response = OAuthClient().RequestToken(AuthorizationCodeTokenRequest(
                config.Value.ClientId, config.Value.ClientSecret, code, Uri(config.Value.RedirectUri)))
            let spotifyClient = SpotifyClient(response.AccessToken)
            try
                let! spotifyUser = spotifyClient.UserProfile.Current()
                do! userStore.Update({ user with
                                            SingleAuthToken = None
                                            SpotifyToken = Some response.AccessToken
                                            SpotifyRefreshToken = Some response.RefreshToken }) :> Task

                return this.Ok($"Authorized as {spotifyUser.DisplayName}. Close window.") :> IActionResult
            with e ->
                logger.LogError(e, "Callback fail")
                return this.BadRequest("Invalid token.")
    }
