module Telegram18thApi.Program

open Microsoft.AspNetCore
open Microsoft.AspNetCore.Hosting
open NLog.Web

[<EntryPoint>]
let rec main args =
    task {
        let logger = NLog.LogManager.Setup().RegisterNLogWeb().LogFactory.GetLogger("Telegram18thApi.Program")

        try
            let builder =
                WebHost
                    .CreateDefaultBuilder(args)
                    .SuppressStatusMessages(true)
                    .UseNLog()
                    .UseStartup<Startup>()

            let app = builder.Build()

            logger.Info("Telegram18thApi started. Press Ctrl-C to exit")
            do! app.RunAsync()

            logger.Info("Exiting...")
            return 0

        with e ->
            logger.Fatal(e, "Failed")
            return 1

    } |> _.ConfigureAwait(false).GetAwaiter().GetResult()
