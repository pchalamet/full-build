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

module Env

open System.IO
open FsHelpers
open System.Reflection

let private VIEW_FOLDER = "views"
let private PROJECT_FOLDER = "projects"
let private PACKAGE_FOLDER = "packages"
let BIN_FOLDER = @"bin"
let GLOBALS_FILENAME = "globals"
let ANTHOLOGY_FILENAME = ".anthology"
let BASELINE_FILENAME = ".baseline"
let BRANCH_FILENAME = "branch"
let FS_GLOBAL_ASSEMBLYINFO_FILENAME = "BuildVersionAssemblyInfo.fs"
let CS_GLOBAL_ASSEMBLYINFO_FILENAME = "BuildVersionAssemblyInfo.cs"
let FULLBUILD_TARGETS = "full-build.targets"
let MASTER_REPO = ".full-build"
let MSBUILD_SOLUTION_DIR2 = "$(FBWorkspaceDir)"
let MSBUILD_SOLUTION_DIR = "$(SolutionDir)"
let MSBUILD_TARGETFX_DIR = "$(TargetFrameworkVersion)"
let MSBUILD_APP_OUTPUT = "apps"
let MSBUILD_PROJECT_FOLDER = sprintf @"%s\%s\%s\" MSBUILD_SOLUTION_DIR MASTER_REPO PROJECT_FOLDER
let MSBUILD_PACKAGE_FOLDER = sprintf @"%s\%s\%s\" MSBUILD_SOLUTION_DIR MASTER_REPO PACKAGE_FOLDER
let MSBUILD_BIN_FOLDER = sprintf @"%s\%s\%s" MSBUILD_SOLUTION_DIR MASTER_REPO BIN_FOLDER
let MSBUILD_NUGET_FOLDER = sprintf @"..\%s\" PACKAGE_FOLDER
let MSBUILD_FULLBUILD_TARGETS = sprintf @"%s\%s\%s" MSBUILD_SOLUTION_DIR MASTER_REPO FULLBUILD_TARGETS
let PUBLISH_BIN_FOLDER = BIN_FOLDER
let PUBLISH_APPS_FOLDER = MSBUILD_APP_OUTPUT
let LOGS_FOLDER = ".logs"

let IsWorkspaceFolder(wsDir : DirectoryInfo) =
    let subDir = wsDir |> GetSubDirectory MASTER_REPO
    subDir.Exists

let rec private workspaceFolderSearch(dir : DirectoryInfo) =
    if dir = null || not dir.Exists then failwith "Can't find workspace root directory. Check you are in a workspace."
    if IsWorkspaceFolder dir then dir
    else workspaceFolderSearch dir.Parent

type DummyType () = class end

let getFullBuildAssembly () =
    let fbAssembly = typeof<DummyType>.GetTypeInfo().Assembly
    fbAssembly

let getInstallationFolder () =
    let fbAssembly = getFullBuildAssembly ()
    let fbAssFI = fbAssembly.Location |> FileInfo
    fbAssFI.Directory

[<RequireQualifiedAccess>]
type Folder =
       | Current
       | Workspace
       | AppOutput
       | Config
       | View
       | Project
       | Package
       | Bin
       | Installation
       | Logs


let rec GetFolder folder =
    match folder with
    | Folder.Current -> CurrentFolder()
    | Folder.Workspace -> CurrentFolder() |> workspaceFolderSearch
    | Folder.AppOutput -> GetFolder Folder.Workspace |> CreateSubDirectory MSBUILD_APP_OUTPUT
    | Folder.Config -> GetFolder Folder.Workspace |> CreateSubDirectory MASTER_REPO
    | Folder.View -> GetFolder Folder.Config |> CreateSubDirectory VIEW_FOLDER
    | Folder.Project -> GetFolder Folder.Config |> CreateSubDirectory PROJECT_FOLDER
    | Folder.Package -> GetFolder Folder.Config |> CreateSubDirectory PACKAGE_FOLDER
    | Folder.Bin -> GetFolder Folder.Config |> CreateSubDirectory BIN_FOLDER
    | Folder.Installation -> getInstallationFolder()
    | Folder.Logs -> GetFolder Folder.Workspace |> CreateSubDirectory LOGS_FOLDER

let GetFsGlobalAssemblyInfoFileName() =
    GetFolder Folder.Bin |> GetFile FS_GLOBAL_ASSEMBLYINFO_FILENAME

let GetCsGlobalAssemblyInfoFileName() =
    GetFolder Folder.Bin |> GetFile CS_GLOBAL_ASSEMBLYINFO_FILENAME

let GetBaselineFile () =
    GetFolder Folder.Bin |> GetFile BASELINE_FILENAME

let GetGlobalsFile() =
    GetFolder Folder.Config |> GetFile GLOBALS_FILENAME

let GetGlobalAnthologyFile() =
    GetFolder Folder.Bin |> GetFile ANTHOLOGY_FILENAME

let GetLocalAnthologyFile (repository : Anthology.RepositoryId) =
    Folder.Workspace |> GetFolder
                     |> GetSubDirectory repository.toString
                     |> GetFile ANTHOLOGY_FILENAME

let GetViewFile viewName =
    GetFolder Folder.View |> GetFile (AddExt Extension.View viewName)

let GetStaticViewFile viewName =
    GetFolder Folder.Current |> GetFile (AddExt Extension.View viewName)

let GetSolutionFile viewName =
    GetFolder Folder.Workspace |> GetFile (AddExt Extension.Solution viewName)

let GetSolutionDefinesFile viewName =
    GetFolder Folder.View |> GetFile (AddExt Extension.Targets viewName)

let GetBranchFile () =
    GetFolder Folder.View |> GetFile BRANCH_FILENAME

let IsMono () =
    let monoRuntime = System.Type.GetType ("Mono.Runtime")
    monoRuntime <> null

let CheckLicense () =
    let fbInstallDir = GetFolder Folder.Installation
    let licFile = fbInstallDir |> GetFile "LICENSE.txt"
    if not (licFile.Exists) then failwithf "Please ensure original LICENSE.txt is available."

    let licContent = File.ReadAllText (licFile.FullName)
    let guid = StringHelpers.GenerateGuidFromString licContent
    let licGuid = StringHelpers.ParseGuid "adb309ac-9a43-00de-cd67-6d479bc4752a"
    if guid <> licGuid then failwithf "Please ensure original LICENSE.txt is available."

let FullBuildVersion () =
    let fbAssembly = getFullBuildAssembly ()
    let version = fbAssembly.GetName().Version
    version
