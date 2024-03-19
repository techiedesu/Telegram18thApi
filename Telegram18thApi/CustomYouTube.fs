namespace Telegram18thApi

open System.IO
open System.Net
open System.Net.Http
open System.Net.Http.Headers
open VideoLibrary

[<Sealed>]
type CustomHandler() =
    member _.GetHandler() =
        let cookieContainer = CookieContainer()
        cookieContainer.Add(Cookie("CONSENT", "YES+cb", "/", "youtube.com"))
        new HttpClientHandler(
            UseCookies = true,
            CookieContainer = cookieContainer
        )

[<Sealed>]
type CustomYouTube() as this =
    inherit YouTube()

    let chunkSize = 10_485_760
    let hc = new HttpClient()

    override _.MakeClient(handler) =
        base.MakeClient(handler)

    override _.MakeHandler() =
         CustomHandler().GetHandler()

    member _.GetContentLengthAsync(requestUri: string) = task {
        use request = new HttpRequestMessage(HttpMethod.Head, requestUri)
        let! responseMessage = hc.SendAsync(request)
        return responseMessage.Content.Headers.ContentLength
    }

    member _.CreateDownloadAsync(uri) = task {
        let ms = new MemoryStream()
        let! fileSize = this.GetContentLengthAsync(uri)
        let fileSize = fileSize |> Option.ofNullable

        match fileSize with
        | None ->
            return None
        | Some fileSize ->
            for segment in 0 .. int32 (fileSize / int64 chunkSize) do
                let from = segment * chunkSize
                let ``to`` = (segment + 1) * chunkSize - 1
                use request = new HttpRequestMessage(HttpMethod.Get, uri)
                request.Headers.Range <- RangeHeaderValue(from, ``to``)
                let! response = hc.SendAsync(request, HttpCompletionOption.ResponseHeadersRead)
                let! stream = response.Content.ReadAsStreamAsync()
                do! stream.CopyToAsync(ms)

            return Some ^ ms.ToArray()
    }
