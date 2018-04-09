namespace RPiBotFs.Pages
open System;
open Microsoft.AspNetCore.Mvc;
open Microsoft.AspNetCore.Mvc.RazorPages;
open Contracts

    type ShowConfigModel() as self =
        inherit PageModel()
        [<BindProperty>]
        member val Configs = Unchecked.defaultof<JConfig> with get, set
        member __.OnGet() = 
            self.Configs <- BotModule.ShowConfig()
        member __.OnPost() =
            let a = self.Configs
            ()