namespace Telegram18thApi

open System
open System.Threading.Tasks
open Elasticsearch.Net
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Options
open Nest

type TelegramId = int64

[<CLIMutable>]
type User = {
    TelegramId: TelegramId
    SpotifyToken: string option

    SingleAuthToken: string option

    CreatedAt: int64
    UpdatedAt: int64
}

[<Sealed>]
type UserStore(configOpt: IOptionsMonitor<ElasticSettings>, stjSerializer: StjElasticsearchSerializer, logger: ILogger<UserStore>) as this =
    let mutable prefix = configOpt.CurrentValue.Prefix
    let mutable client =
        let pool = new SingleNodeConnectionPool(Uri(configOpt.CurrentValue.Node))
        let cs = new ConnectionSettings(pool, sourceSerializer = ConnectionSettings.SourceSerializerFactory(fun x y -> stjSerializer))
        ElasticClient(cs)

    do
        %configOpt.OnChange(fun newConfig ->
            logger.LogInformation("Called update {newConfig}", newConfig)
            client <-
                let pool = new SingleNodeConnectionPool(Uri(newConfig.Node))
                let cs = new ConnectionSettings(pool, sourceSerializer = ConnectionSettings.SourceSerializerFactory(fun x y -> stjSerializer))
                ElasticClient(cs)
            prefix <- newConfig.Prefix
        )

    member _.Index = $"{prefix}_user"

    member _.Create(user) = task {
        let! existingUser = this.GetByTelegramId(user.TelegramId)

        match existingUser with
        | None ->
            do!
                client
                    .IndexAsync<User>(user,
                        selector = fun descriptor ->
                            descriptor
                                .Index(this.Index)
                                .Id(user.TelegramId)
                    ) :> Task
        | _ -> ()
    }

    member _.Update(user) = task {
        let! existingUser = this.GetByTelegramId(user.TelegramId)

        match existingUser with
        | Some existingUser ->

            let updatedUser = { user with
                                    CreatedAt = existingUser.CreatedAt
                                    UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }

            let! indexResponse =
                    client
                        .IndexAsync<User>(updatedUser,
                            selector = fun descriptor ->
                                descriptor
                                    .Index(this.Index)
                                    .Id(user.TelegramId)
                        )
            return Some indexResponse.Result
        | _ ->
            return None
    }

    member _.GetByTelegramId(telegramId: TelegramId) = task {
        let! searchResponse =
            client.SearchAsync<User>(fun s ->
                let sr = Nest.SearchRequest(this.Index, Size = (Nullable 1))
                let query = Nest.QueryContainerDescriptor<User>()

                let q = sprintf """
{
  "bool": {
    "minimum_should_match": "1",
    "should": [
      {
        "term": {
          "telegramId": {
            "value": "%i"
          }
        }
      }
    ]
  }
}
"""

                %query.Raw(q telegramId)
                sr.Query <- query
                sr :> Nest.ISearchRequest)

        return searchResponse.Hits
               |> Seq.tryHead
               |> Option.map (_.Source)
    }

    member _.GetBySingleAuthToken(singleAuthToken: string) = task {
        let! searchResponse =
            client.SearchAsync<User>(fun s ->
                let sr = Nest.SearchRequest(this.Index, Size = (Nullable 1000))
                let query = Nest.QueryContainerDescriptor<User>()

                let q = sprintf """
{
  "bool": {
    "minimum_should_match": "1",
    "should": [
      {
        "term": {
          "singleAuthToken": {
            "value": "%s"
          }
        }
      }
    ]
  }
}
"""

                %query.Raw(q singleAuthToken)
                sr.Query <- query
                sr :> Nest.ISearchRequest)

        return searchResponse.Hits
               |> Seq.tryHead
               |> Option.map (_.Source)
    }

    member _.AddSingleAuthToken(telegramUserId: TelegramId) = task {
        match! this.GetByTelegramId(telegramUserId) with
        | None ->
            let token = Guid.NewGuid().ToString()
            let user = {
                TelegramId = telegramUserId
                SpotifyToken = None
                SingleAuthToken = Some token
                CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                UpdatedAt = 0L
            }
            do! this.Create(user) :> Task
            return token

        | Some user ->
            let token = Guid.NewGuid().ToString()
            let user = {
               user with
                    SingleAuthToken = Some token
                    UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            }
            do! this.Update(user) :> Task
            return token
    }
