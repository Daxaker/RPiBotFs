namespace RPiBotFs
open Microsoft.AspNetCore.Mvc.RazorPages;
open BotModule
type StartModel () = 
    inherit PageModel()

    member this.OnGet() =
        do Start()