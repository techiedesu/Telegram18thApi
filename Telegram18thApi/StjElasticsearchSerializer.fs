namespace Telegram18thApi

open System.Text.Json
open Elasticsearch.Net
open Microsoft.AspNetCore.Http.Json
open Microsoft.Extensions.Options

[<Sealed>]
type StjElasticsearchSerializer(config: IOptions<JsonOptions>) =
    interface IElasticsearchSerializer with
        member this.Deserialize(``type``, stream) =
            JsonSerializer.Deserialize(stream, returnType = ``type``, options = config.Value.SerializerOptions)

        member this.Deserialize(stream) =
            JsonSerializer.Deserialize(stream, config.Value.SerializerOptions)

        member this.DeserializeAsync(``type``, stream, cancellationToken) =
            JsonSerializer.DeserializeAsync(stream, options = config.Value.SerializerOptions, cancellationToken = cancellationToken, returnType = ``type``).AsTask()

        member this.DeserializeAsync(stream, cancellationToken) =
            JsonSerializer.DeserializeAsync(stream, config.Value.SerializerOptions, cancellationToken = cancellationToken).AsTask()

        member this.Serialize(data, stream, formatting) =
            let writer = new Utf8JsonWriter(stream)
            JsonSerializer.Serialize(writer, data, config.Value.SerializerOptions)

        member this.SerializeAsync(data, stream, formatting, cancellationToken) =
            JsonSerializer.SerializeAsync(stream, data, config.Value.SerializerOptions, cancellationToken = cancellationToken)
