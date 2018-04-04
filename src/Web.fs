namespace RPiBotFs 

open Nancy
open BotModule 

[<AutoOpen>]
module Web =
    
    let inline (?) (parameters:obj) param =
        (parameters :?> Nancy.DynamicDictionary).[param]

type HelloModule() as self =
    inherit NancyModule()
    do
        self.Get("/start", fun _ -> Start())
        self.Get("/stop", fun _ -> Stop())
        self.Get("/showConfig", fun _ -> self.View.["showconfig.sshtml", ShowConfig()])
        //self.Get("/showconfigv")