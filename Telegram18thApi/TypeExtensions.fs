[<AutoOpen>]
module Telegram18thApi.TypeExtensions

let inline (^) f x = f x
let inline (~%) x = ignore x

module Option =
    let inline ofTry (success, res) = if success then Some res else None
