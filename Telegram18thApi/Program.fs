module Telegram18thApi.Program

open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Hosting
open NLog.Web

[<EntryPoint>]
let rec main args =
    task {
        let logger = NLog.LogManager.Setup().RegisterNLogWeb().LogFactory.GetLogger("Telegram18thApi.Program")

        try
            let builder =
                Host
                    .CreateDefaultBuilder(args)
                    .ConfigureWebHostDefaults(fun wb -> %wb.UseStartup<Startup>())

            let app = builder.Build()
            do! app.RunAsync()

            logger.Info("Exiting...")
            return 0

        with e ->
            logger.Fatal(e, "Failed")
            return 1

    } |> _.ConfigureAwait(false).GetAwaiter().GetResult()
