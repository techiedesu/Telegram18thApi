namespace Telegram18thApi

open SpotifyAPI.Web
open VideoLibrary
open YoutubeSearchApi.Net.Services

type TrackSearchService(userStore: UserStore, youTube: CustomYouTube, ytSearch: YoutubeSearchClient) =
    member _.Search(telegramId: TelegramId, query: string option) = task {
        match query with
        | None ->
            let! user = userStore.GetByTelegramId(telegramId)
            let token = user |> Option.bind (_.SpotifyToken)

            let! currentPlayingTrack = task {
                match token with
                | None ->
                    return None
                | Some token ->
                    let client = SpotifyClient(token)
                    let! track = client.Player.GetCurrentPlayback()
                    let trackData =
                                track.Item :?> FullTrack
                                |> Option.ofObj
                                |> Option.map (fun fullTrack -> fullTrack.Artists[0].Name + " - " + fullTrack.Name)

                    match trackData with
                    | None ->
                        return None
                    | Some trackData ->
                        let track = task {
                            let! tracks = ytSearch.SearchAsync(trackData)
                            let result = tracks.Results
                                         |> Seq.tryHead
                                         |> Option.map (fun youtubeVideo -> youTube.GetAllVideos(youtubeVideo.Url))

                            match result with
                            | None ->
                                return None
                            | Some tracks ->
                                let track = tracks |> Seq.find (fun youTubeVideo -> youTubeVideo.AudioFormat = AudioFormat.Opus)
                                let! bytes = youTube.CreateDownloadAsync(track.Uri)
                                return bytes
                        }

                        return! track
            }

            return currentPlayingTrack
        | Some _ ->
            return None
    }
