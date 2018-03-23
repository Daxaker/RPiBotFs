module Tests

open System
open Xunit
open RPiBotFs

[<Fact>]
let ``(?) should work`` () =
    let dd:Nancy.DynamicDictionary = 
        Nancy.DynamicDictionary()
    dd.["test"] <- 1
    let res = dd?test
    Assert.Equal(res, Nancy.DynamicDictionaryValue(1))
    
