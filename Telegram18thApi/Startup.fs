namespace Telegram18thApi

open System.Net.Http
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open NLog.Extensions.Logging
open System.Text.Json.Serialization
open YoutubeSearchApi.Net.Services

[<Sealed>]
type Startup(configuration: IConfiguration) =
    member _.ConfigureServices(services: IServiceCollection) =
        %services
            .Configure<AppSettings>(configuration)
            .Configure<ElasticSettings>(configuration.GetSection("elastic"))
            .Configure<SpotifySettings>(configuration.GetSection("spotify"))
            .AddLogging(
                fun loggingBuilder ->
                    %loggingBuilder
                         .ClearProviders()
                         .AddNLog()
                )

        %services
            .AddControllers()
            .AddJsonOptions(fun options ->
                    options.JsonSerializerOptions.WriteIndented <- true

                    JsonFSharpOptions()
                        .WithUnionUntagged()
                        .AddToJsonSerializerOptions(options.JsonSerializerOptions)
                    )
        %services
             .AddSingleton<StjElasticsearchSerializer>()
             .AddSingleton<UserStore>()
             .AddSingleton<CustomYouTube>()
             .AddSingleton<YoutubeSearchClient>(YoutubeSearchClient(new HttpClient()))
             .AddSingleton<TrackSearchService>()

    member _.Configure(app: IApplicationBuilder, env: IWebHostEnvironment) =
        if env.IsDevelopment() then
            %app.UseDeveloperExceptionPage()
        %app
             .UseRouting()
             .UseAuthorization()
             .UseEndpoints(fun endpoints ->
                 %endpoints.MapControllers()
             )
