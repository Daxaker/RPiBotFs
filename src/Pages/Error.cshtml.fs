namespace RPiBotFs.Pages
open System.Diagnostics;
open Microsoft.AspNetCore.Mvc.RazorPages;

    type ErrorModel() as this = 
        inherit PageModel()
        member val RequestId = "" with get, set
        member val ShowRequestId = System.String.IsNullOrEmpty(this.RequestId) |> not
        member __.OnGet() =
            if Activity.Current |> isNull then
               this.RequestId <- Activity.Current.Id
            else
               this.RequestId <- this.HttpContext.TraceIdentifier;
