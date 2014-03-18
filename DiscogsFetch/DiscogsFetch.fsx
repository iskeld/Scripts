#r "packages/FSharp.Data.2.0.3/lib/net40/FSharp.Data.dll"
#r "packages/Newtonsoft.Json.6.0.1/lib/net45/Newtonsoft.Json.dll"
#r "packages/taglib.2.1.0.0/lib/policy.2.0.taglib-sharp.dll"
#r "packages/taglib.2.1.0.0/lib/taglib-sharp.dll"
#r "packages/UriQueryBuilder.1.0.1/lib/UriQueryBuilder.dll"

open System;
open System.Collections.Generic;
open System.IO;
open FSharp.Data;
open FSharp.Data.JsonExtensions;
open Newtonsoft.Json;

type DiscogsDbReleaseSearch = JsonProvider<"http://api.discogs.com/database/search?type=release&artist=Metallica&title=Master%20Of%20Puppets">
type DiscogsDbReleaseInfo = JsonProvider<"http://api.discogs.com/releases/368276">

type SingleArtistReleaseInfo = {
    Artist: string
    Release: string
    Tracks: int
}

type Track = {
    Title: string
    Duration: TimeSpan
}

type Release = {
    Artist: string
    Title: string
    Tracks: Track list
}

type SupportedFileFormat =
    | Mp3
    | Flac

let inline (|?) (a: 'a option) b = if a.IsSome then a.Value else b

let getSupportedFileFormat (file:FileInfo) =
    let extension = file.Extension.TrimStart('.').ToLower()
    match extension with
    | "mp3" -> Some Mp3
    | "flac" -> Some Flac
    | _ -> None

let getMusicFileTypes dir =
    let directory = new DirectoryInfo(dir)
    match directory.Exists with
    | true -> directory.GetFiles() |> Array.tryPick getSupportedFileFormat
    | false -> failwith "Directory does not exist"

let fillTagSetsForMp3 (artists:HashSet<string>) (releases:HashSet<string>) (file:FileInfo) =
    use mp3File = TagLib.File.Create(file.FullName)
    mp3File.Tag.AlbumArtists 
        |> Seq.append mp3File.Tag.Performers
        |> Seq.iter (fun str -> if artists.Add(str) then printfn "Found artist %A" str)
    releases.Add(mp3File.Tag.Album) |> ignore

let getFilesOfFormat path fileType =
    let extension = 
        match fileType with
        | Mp3 -> "*.mp3"
        | Flac -> "*.flac"

    let directory = new DirectoryInfo(path)
    directory.GetFiles(extension)

let getReleaseInfo path fileType =
    let artistsHash = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    let releaseHash = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    let musicFiles = getFilesOfFormat path fileType
    let musicFilesCount = musicFiles.Length
    printfn "Foud %A files of type %A" musicFilesCount fileType 
    musicFiles |> Array.iter (fillTagSetsForMp3 artistsHash releaseHash)

    // TODO: multiple artists / albums

    let release = releaseHash |> Seq.head 
    {   
        Artist = artistsHash |> Seq.head
        Release = release.Replace("(EP)", String.Empty)
        Tracks = musicFilesCount
    }

let fetchRelease (url:string) = 
    let trackMapper (track:DiscogsDbReleaseInfo.Tracklist) = 
        let duration = if String.IsNullOrEmpty((track.JsonValue?duration).AsString()) then DateTime.MinValue else track.Duration
        {
            Title = track.Title
            Duration = new TimeSpan(duration.Hour, duration.Minute, duration.Second)
        }
    let jsonRelease = DiscogsDbReleaseInfo.Load(url)
    {
        Artist = jsonRelease.Artists.[0].Name
        Title = jsonRelease.Title
        Tracks = jsonRelease.Tracklist |> Seq.filter (fun t -> t.Type = "track") |> Seq.map trackMapper |> Seq.toList
    }

let queryDiscogs (release:SingleArtistReleaseInfo) =
    let rec harvester (url:string) =
        printfn "Downloading results from %A" url
        let results = DiscogsDbReleaseSearch.Load(url)
        let nextUrl = if String.IsNullOrWhiteSpace(results.Pagination.Urls.Next) then None else Some results.Pagination.Urls.Next
        results.Results 
            |> List.ofArray 
            |> List.map (fun rel -> rel.ResourceUrl)
            |> (fun current -> if nextUrl.IsNone then current else current @ harvester(nextUrl.Value + "pipka"))

    let builder = new UriQueryBuilder("http", "api.discogs.com");
    builder.Path <- "database/search"
    builder.QueryString.["type"] <- "release"
    builder.QueryString.["artist"] <- release.Artist
    builder.QueryString.["title"] <- release.Release
    let uri = builder.ToString()
    harvester(uri)

let writeObject path obj =  
    let json = JsonConvert.SerializeObject(obj, Formatting.Indented)
    File.WriteAllText(path, json)


let overrideInfoProvider album artist path fileType =
    getReleaseInfo path fileType 
        |> fun input -> { input with Artist = artist |? input.Artist; Release = album |? input.Release }

let fetchForDirectory path releaseInfoProvider =
    let filesType = getMusicFileTypes path
    if filesType.IsNone then failwith "Unsupported files format or no files found"
    let releaseInfo = releaseInfoProvider path filesType.Value
    let releaseUrls = queryDiscogs(releaseInfo)
    let releases = 
        releaseUrls 
        |> Seq.map fetchRelease
        |> Seq.filter (fun rel -> rel.Tracks.Length = releaseInfo.Tracks)
        |> Seq.distinct 
        |> Seq.toList

    let outputPath file = Path.Combine(path, file)
    let writeOutput obj = writeObject (outputPath "Output.json") obj

    match releases.Length with
    | 0 -> printfn "No releases found"
    | 1 -> 
        printfn "Found 1 release. Serializing" 
        writeOutput releases.Head
        File.WriteAllLines((outputPath "Tracks.txt"), releases.Head.Tracks |> Seq.map (fun t -> t.Title))
    | _ ->
        printfn "Found %A releases. Serializing" releases.Length
        File.WriteAllLines((outputPath "Tracks.txt"), releases |> Seq.map (fun r -> String.Join("|", r.Tracks |> Seq.map (fun t -> t.Title))))
        writeOutput releases

match fsi.CommandLineArgs with
    | [|_;path|] -> fetchForDirectory path getReleaseInfo; printfn "Processing completed."
    | [|_;path;album|] -> fetchForDirectory path <| overrideInfoProvider (Some album) None; printfn "Processing completed."
    | [|_;path;album;artist|] -> fetchForDirectory path <| overrideInfoProvider (Some album) (Some artist); printfn "Processing completed."
    | _ -> eprintfn "USAGE: [input directory]"