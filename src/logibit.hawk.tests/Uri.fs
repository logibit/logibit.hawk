﻿module logibit.hawk.Tests.Uri

open System
open Fuchu
open NodaTime

open logibit.hawk
open logibit.hawk.Encoding
open logibit.hawk.Types
open logibit.hawk.Bewit

open logibit.hawk.Tests.Shared


let timestamp = Instant.FromSecondsSinceUnixEpoch 123456789L

let clock =
  { new IClock with
      member x.Now = timestamp }

let creds_inner =
  { id        = "123456"
    key       = "2983d45yun89q"
    algorithm = SHA256 }

let settings =
  { BewitSettings.clock = clock
    logger              = Logging.NoopLogger
    allowed_clock_skew  = Duration.FromMilliseconds 8000L
    local_clock_offset  = Duration.Zero
    creds_repo          = fun id -> Choice1Of2 (creds_inner, "steve") }

[<Tests>]
let temp =
  let seconds i = Duration.FromSeconds i

  testList "Bewit.generate" [
    testCase "returns a valid bewit value (explicit port)" <| fun _ ->
      let b = Bewit.generate "https://example.com:8080/somewhere/over/the/rainbow"
                        { BewitOptions.credentials = creds_inner
                          ttl = Duration.FromSeconds 3L
                          localtime_offset = ts 1356420407232L - clock.Now 
                          ext = Some "xandyandz" }
      Assert.Equal("bewit should generate correctly",
                   "MTIzNDU2XDEzNTY0MjA3MDdcaFpiSjNQMmNLRW80a3kwQzhqa1pBa1J5Q1p1ZWc0V1NOYnhWN3ZxM3hIVT1ceGFuZHlhbmR6",
                   b)
  ]

[<Tests>]
let encoding_tests =
  testCase "it should encode and decode a uri to match the original" <| fun _ ->
    let test_uri = "http://example.com:80/resource/4?a=1&b=2"
    Assert.Equal("return value", test_uri, (ModifiedBase64Url.encode >> ModifiedBase64Url.decode) test_uri)


[<Tests>]
let getBewit =

  let credentials =
    { id        = "123456"
      key       = "2983d45yun89q"
      algorithm = SHA256 }

  testList "bewit tests" [
    testCase "it returns a valid bewit value" <| fun _ ->
      Assert.Equal("test", "0", "1") 
  ]

[<Tests>]
let uri =
  let uri = "http://example.com:80/resource/4?a=1&b=2"
  let bewit_request =
    { ``method`` = GET
      uri        = Uri (uri + "&bewit=" + (bewit uri))
      header     = None
      host       = None
      port       = None }

  testList "authentication" [
    testCase "it should generate a bewit then succesfully authenticate it" <| fun _ ->
      authenticate settings {bewit_request with header = Option.Some "ext=\"some-app-data\"" }
      |> ensure_value
      |> fun (attrs, _, user) ->
        match attrs.ext with
        | Some ext ->
          Assert.Equal("return value", "some-app-data", ext)
        | None ->
          Tests.failtest "Expected ext=\"some-app-data\" got \"None\""
        Assert.Equal("return value", "steve", user)

    testCase "it should generate a bewit then succesfully authenticate it (no ext)" <| fun _ ->
      authenticate settings bewit_request
      |> ensure_value
      |> fun (attrs, _, user) ->
        Assert.Equal("return value", "steve", user)

    testCase "should successfully authenticate a request (last param)" <| fun _ ->
      { ``method`` = GET
        uri        = Uri "/resource/4?a=1&b=2&bewit=MTIzNDU2XDQ1MTE0ODQ2MjFcMzFjMmNkbUJFd1NJRVZDOVkva1NFb2c3d3YrdEVNWjZ3RXNmOGNHU2FXQT1cc29tZS1hcHAtZGF0YQ"
        header     = None
        host       = Some "example.com"
        port       = Some 8080us }
      |> authenticate settings
      |> ensure_value
      |> fun (attrs, _, user) ->
        match attrs.ext with
        | Some ext ->
          Assert.Equal("return value", "some-app-data", ext)
        | None ->
          Tests.failtest "Expected ext=\"some-app-data\" got \"None\""
        Assert.Equal("return value", "steve", user)

  ]
