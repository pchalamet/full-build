﻿//   Copyright 2014-2016 Pierre Chalamet
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

module Core.BuildArtifacts
open System.IO
open IoHelpers


let Publish (branch : string option) buildnum hash =
    let graph = Configuration.LoadAnthology () |> Graph.from
    let mainRepo = graph.MasterRepository
    let wsDir = Env.GetFolder Env.Folder.Workspace
    let appDir = Env.GetFolder Env.Folder.AppOutput
    let versionDir = DirectoryInfo(graph.ArtifactsDir) |> GetSubDirectory hash
    let tmpVersionDir = DirectoryInfo(versionDir.FullName + ".tmp")
    let buildTag = match branch with
                      | None -> sprintf "%s:%s:default" buildnum hash
                      | Some br -> sprintf "%s:%s:%s" buildnum hash br

    try
        let doPublish = not versionDir.Exists
        if doPublish then
            if tmpVersionDir.Exists then
                tmpVersionDir.Delete(true)

            let sourceBinDir = Env.GetFolder Env.Folder.Bin
            let targetBinDir = tmpVersionDir |> GetSubDirectory Env.PUBLISH_BIN_FOLDER
            IoHelpers.CopyFolder sourceBinDir targetBinDir true

            let appTargetDir = tmpVersionDir |> GetSubDirectory Env.PUBLISH_APPS_FOLDER
            IoHelpers.CopyFolder appDir appTargetDir true

            // publish
            Try (fun () -> Plumbing.Vcs.Push wsDir mainRepo)

            tmpVersionDir.MoveTo(versionDir.FullName)
        else
            printfn "[WARNING] Build output already exists - skipping"

        let latestVersionFile = DirectoryInfo(graph.ArtifactsDir) |> GetFile "versions"
        
        File.AppendAllLines(latestVersionFile.FullName, [buildTag])
        printfn "[version] %s" hash
        for app in appDir |> EnumarateFiles do
            printfn "[appversion] %s" app.Name
            let versionFile = DirectoryInfo(graph.ArtifactsDir) |> GetFile (sprintf "%s.versions" app.Name)
            File.AppendAllLines(versionFile.FullName, [buildTag])
    with
        _ -> versionDir.Refresh ()
             if versionDir.Exists then versionDir.MoveTo(versionDir.FullName + ".failed")

             tmpVersionDir.Refresh()
             if tmpVersionDir.Exists then tmpVersionDir.Delete(true)

             reraise ()

let PullReferenceBinaries version =
    let antho = Configuration.LoadAnthology ()
    let artifactDir = antho.Artifacts |> DirectoryInfo

    let versionDir = artifactDir |> GetSubDirectory version
    if versionDir.Exists then
        DisplayHighlight (sprintf "Getting binaries %s" version)
        let sourceBinDir = versionDir |> GetSubDirectory Env.PUBLISH_BIN_FOLDER
        let targetBinDir = Env.GetFolder Env.Folder.Bin
        IoHelpers.CopyFolder sourceBinDir targetBinDir false
    else
        DisplayHighlight "[WARNING] No reference binaries found"

let PullLatestReferenceBinaries () =
    let antho = Configuration.LoadAnthology ()
    let versionsFile = DirectoryInfo(antho.Artifacts) |> GetFile "versions"
    let version = File.ReadAllLines(versionsFile.FullName) |> Seq.last
    let hash = version.Split(':') |> Seq.toArray
    PullReferenceBinaries hash.[1]