﻿//   Copyright 2014-2017 Pierre Chalamet
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.

module Core.Publishers
open IoHelpers
open Env
open System.IO
open Graph
open Exec


type private PublishApp =
    { Name : string
      App : Application 
      Version : string }


let private publishCopy (app : PublishApp) =
    let wsDir = GetFolder Env.Folder.Workspace
    let projects = app.App.Projects
    for project in projects do
        let repoDir = wsDir |> GetSubDirectory (project.Repository.Name)
        if repoDir.Exists then
            let projFile = repoDir |> GetFile project.ProjectFile
            let args = sprintf "/nologo /t:FBPublish /p:SolutionDir=%A /p:FBApp=%A %A" wsDir.FullName app.Name projFile.FullName

            if Env.IsMono () then Exec "xbuild" args wsDir Map.empty |> CheckResponseCode
            else Exec "msbuild" args wsDir Map.empty |> CheckResponseCode

            let appDir = GetFolder Env.Folder.AppOutput
            let artifactDir = appDir |> GetSubDirectory app.Name
            Bindings.UpdateArtifactBindingRedirects artifactDir
        else
            printfn "[WARNING] Can't publish application %A without repository" app.Name

let private publishZip (app : PublishApp) =
    let tmpApp = { app
                   with Name = ".tmp-" + app.Name }
    publishCopy tmpApp

    let appDir = GetFolder Env.Folder.AppOutput
    let sourceFolder = appDir |> GetSubDirectory (tmpApp.Name)
    let targetFile = appDir |> GetFile app.Name
    if targetFile.Exists then targetFile.Delete()

    System.IO.Compression.ZipFile.CreateFromDirectory(sourceFolder.FullName, targetFile.FullName, Compression.CompressionLevel.Optimal, false)

let private publishDocker (app : PublishApp) =
    let tmpApp = { app
                   with Name = ".tmp-docker" }
    publishCopy tmpApp

    let appDir = GetFolder Env.Folder.AppOutput
    let sourceFolder = appDir |> GetSubDirectory (tmpApp.Name)
    let targetFile = appDir |> GetFile app.Name
    if targetFile.Exists then targetFile.Delete()

    let dockerArgs = sprintf "build -t %s ." app.Name
    Exec "docker" dockerArgs sourceFolder Map.empty |> CheckResponseCode
    sourceFolder.Delete(true)

let private publishNuget (app : PublishApp) =
    let tmpApp = { app
                   with Name = ".tmp-nuget-" + app.Name }
    publishCopy tmpApp

    let appDir = GetFolder Env.Folder.AppOutput
    let sourceFolder = appDir |> GetSubDirectory tmpApp.Name
    let targetFolder = appDir |> GetSubDirectory app.Name
    if targetFolder.Exists then targetFolder.Delete(true)

    let nuspec = sourceFolder.EnumerateFiles("*.nuspec") 
                    |> Seq.tryHead 

    match nuspec with
    | Some nuspecFile ->
        Generators.Packagers.UpdateDependencies nuspecFile
        let version = app.Version
        let nugetArgs = sprintf "pack %s -version %s" nuspecFile.Name version
        Exec "nuget" nugetArgs sourceFolder Map.empty |> CheckResponseCode
        targetFolder.Create()
        for file in sourceFolder.EnumerateFiles("*.nupkg") do 
            file.MoveTo(Path.Combine(targetFolder.FullName, file.Name)) |> ignore
        sourceFolder.Delete(true)
    | None -> failwith (sprintf "No nuspec found for the application %s" app.Name)

let PublishWithPublisher (version : string) (app : Application) =
    let publisher = 
        match app.Publisher with
        | PublisherType.Copy -> publishCopy
        | PublisherType.Zip -> publishZip
        | PublisherType.Docker -> publishDocker
        | PublisherType.NuGet -> publishNuget
    { Name = app.Name; App = app; Version = version }
        |> publisher


