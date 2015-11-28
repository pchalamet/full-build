﻿//   Copyright 2014-2015 Pierre Chalamet
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

module MsBuildHelpers
open Anthology
open System.Xml.Linq

let NsMsBuild = XNamespace.Get("http://schemas.microsoft.com/developer/msbuild/2003")

let NsDgml = XNamespace.Get("http://schemas.microsoft.com/vs/2009/dgml")

let NsNone = XNamespace.None

let inline (!>) (x : ^a) : ^b = (((^a or ^b) : (static member op_Explicit : ^a -> ^b) x))


let ReplaceInvalidChars (s : string) =
    s.Replace('-', '_').Replace('.', '_').Replace("{", "").Replace("}", "")

let ProjectPropertyName (project : Project) =
    let prjGuid = project.Output.toString |> ReplaceInvalidChars
    let prjProp = sprintf "Prj_%s" prjGuid
    prjProp

let PackagePropertyName (id : string) =
    let pkgProp = sprintf "FullBuild_%s_Pkg" id |> ReplaceInvalidChars
    pkgProp
