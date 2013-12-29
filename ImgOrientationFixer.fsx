(*
* Rotates JPG images in the given directory, according to their EXIF rotation (http://jpegclub.org/exif_orientation.html) and sets it to 1 (default).
* USAGE: fsi ImgOrientationFixer.fsx [Directory]
*)
open System;
open System.Drawing;
open System.Drawing.Imaging;
open System.IO;

[<Literal>]
let ExifOrientation = 0x0112

type RotationOperation =
    | Rotation of RotateFlipType
    | Unsupported

type Orientations =
    | Default = 1uy
    | UpsideDown = 3uy
    | Rotated90CCWise = 6uy
    | Rotated270CCWise = 8uy

type ImgToRotate = { img: Image; path: string; orientationProperty: PropertyItem; rotateType: RotateFlipType }

let DefaultOrientation = byte Orientations.Default

let rotateImage imgToRotatate = 
    let { path = path; rotateType = rotation; img = img; orientationProperty = pi } = imgToRotatate   
    printfn "Rotating img %s by %A" path rotation
    img.RotateFlip(rotation)
    pi.Value.[0] <- DefaultOrientation
    img.Save(path)

let (|ExifRotation|_|) (pi:PropertyItem) : Orientations option =
    if pi.Id = ExifOrientation && pi.Value.[0] <> DefaultOrientation
    then Some (LanguagePrimitives.EnumOfValue pi.Value.[0])
    else None

let getRotationOperation exif =
    match exif with 
    | Orientations.UpsideDown -> Rotation RotateFlipType.Rotate180FlipNone
    | Orientations.Rotated90CCWise -> Rotation RotateFlipType.Rotate90FlipNone
    | Orientations.Rotated270CCWise -> Rotation RotateFlipType.Rotate270FlipNone
    | _ -> Unsupported


let processImage filePath =
    let matchRotation pi = 
        match pi with
        | ExifRotation r -> 
            match getRotationOperation r with
            | Rotation flipType -> Some (pi, flipType)
            | Unsupported -> failwith "Flipping is not supported yet"
        | _ -> None

    use img = Image.FromFile(filePath)

    let rotateMe (property, rotate) = 
        rotateImage { img = img; path = filePath; orientationProperty = property; rotateType = rotate }

    img.PropertyItems
    |> Seq.tryPick matchRotation
    |> Option.iter rotateMe

let getFilesWithExtensions extensions path =
    [for ext in extensions do yield! Directory.GetFiles(path, ext)]

let getImages = getFilesWithExtensions ["*.jpg"; "*.jpeg"]

let processImages path =
    getImages path |> Seq.iter processImage

match fsi.CommandLineArgs with
    | [|_;path|] -> processImages path; printfn "Processing completed."
    | _ -> eprintfn "USAGE: [input directory]"
