namespace Telegram18thApi

open System.Text.Json
open Elasticsearch.Net
open Microsoft.AspNetCore.Http.Json
open Microsoft.Extensions.Options

[<Sealed>]
type StjElasticsearchSerializer(configOpt: IOptionsMonitor<JsonOptions>) =

    let getSerializer indented (config: JsonOptions) =
        let jsonOptions = config.SerializerOptions
        jsonOptions.WriteIndented <- indented
        jsonOptions

    let mutable serializerOptions = configOpt.CurrentValue |> getSerializer false

    let mutable serializerOptionsIndented =  configOpt.CurrentValue |> getSerializer true

    do
        %configOpt.OnChange(
            fun newConfig ->
                serializerOptions <- newConfig |> getSerializer false
                serializerOptionsIndented <- newConfig |> getSerializer true)

    interface IElasticsearchSerializer with
        member this.Deserialize(``type``, stream) =
            JsonSerializer.Deserialize(stream, returnType = ``type``, options = serializerOptionsIndented)

        member this.Deserialize(stream) =
            JsonSerializer.Deserialize(stream, serializerOptionsIndented)

        member this.DeserializeAsync(``type``, stream, cancellationToken) =
            JsonSerializer.DeserializeAsync(stream, options = serializerOptionsIndented, cancellationToken = cancellationToken, returnType = ``type``).AsTask()

        member this.DeserializeAsync(stream, cancellationToken) =
            JsonSerializer.DeserializeAsync(stream, serializerOptionsIndented, cancellationToken = cancellationToken).AsTask()

        member this.Serialize(data, stream, formatting) =
            let writer = new Utf8JsonWriter(stream)
            JsonSerializer.Serialize(writer,
                                     data,
                                     if formatting = SerializationFormatting.Indented then
                                         serializerOptionsIndented
                                     else
                                         serializerOptions)

        member this.SerializeAsync(data, stream, formatting, cancellationToken) =
            JsonSerializer.SerializeAsync(stream,
                                          data,
                                          (if formatting = SerializationFormatting.Indented then
                                              serializerOptionsIndented
                                          else
                                              serializerOptions),
                                          cancellationToken = cancellationToken)
