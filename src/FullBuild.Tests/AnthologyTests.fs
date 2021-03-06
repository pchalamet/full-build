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

module AnthologyTests

open NUnit.Framework
open FsUnit
open Anthology
open StringHelpers

[<Test>]
let CheckReferences () =
    AssemblyId.from "badaboum" |> should equal <| AssemblyId.from "BADABOUM"

    PackageId.from "badaboum" |> should equal <| PackageId.from "BADABOUM"
    PackageId.from "badaboum" |> should equal <| PackageId.from "badaboum"

    RepositoryId.from "badaboum" |> should equal <| RepositoryId.from "BADABOUM"

let CheckToRepository () =
    let repoGit = { Name = RepositoryId.from "cassandra-sharp"
                    Url = RepositoryUrl.from "https://github.com/pchalamet/cassandra-sharp"
                    Branch = None
                    Vcs = VcsType.Git }
    repoGit |> should equal { Name = RepositoryId.from "cassandra-sharp"
                              Branch = None
                              Url = RepositoryUrl.from "https://github.com/pchalamet/cassandra-sharp"
                              Vcs = VcsType.Git }

    let repoHg = { Name = RepositoryId.from "cassandra-sharp"
                   Url = RepositoryUrl.from "https://github.com/pchalamet/cassandra-sharp"
                   Branch = None
                   Vcs = VcsType.Git }
    repoHg |> should equal { Name = RepositoryId.from "cassandra-sharp"
                             Branch = None
                             Url = RepositoryUrl.from "https://github.com/pchalamet/cassandra-sharp"
                             Vcs = VcsType.Git }

[<Test>]
let CheckEqualityWithPermutation () =
    let antho1 = {
        Projects = [ { Output = AssemblyId.from "cqlplus"
                       ProjectId = ProjectId.from "cqlplus"
                       OutputType = OutputType.Exe
                       UniqueProjectId = ProjectUniqueId.from (ParseGuid "0a06398e-69be-487b-a011-4c0be6619b59")
                       RelativeProjectFile = ProjectRelativeFile "cqlplus/cqlplus-net45.csproj"
                       FxVersion = FxInfo.from "v4.5"
                       FxProfile = FxInfo.from null
                       FxIdentifier = FxInfo.from null
                       HasTests = false
                       ProjectReferences = [ ProjectId.from "cassandrasharp.interfaces"; ProjectId.from "cassandrasharp" ] |> set
                       AssemblyReferences = [ AssemblyId.from "System" ; AssemblyId.from "System.Data"; AssemblyId.from "System.Xml"] |> set
                       PackageReferences = Set.empty
                       Repository = RepositoryId.from "cassandra-sharp" } ] |> set
        Applications = Set.empty }

    let antho2 = {
        Projects = [ { Output = AssemblyId.from "cqlplus"
                       ProjectId = ProjectId.from "cqlplus"
                       OutputType = OutputType.Exe
                       UniqueProjectId = ProjectUniqueId.from (ParseGuid "0a06398e-69be-487b-a011-4c0be6619b59")
                       RelativeProjectFile = ProjectRelativeFile "cqlplus/cqlplus-net45.csproj"
                       FxVersion = FxInfo.from "v4.5"
                       FxProfile = FxInfo.from null
                       FxIdentifier = FxInfo.from null
                       HasTests = false
                       ProjectReferences = [ ProjectId.from "cassandrasharp.interfaces"; ProjectId.from "cassandrasharp" ] |> set
                       AssemblyReferences = [ AssemblyId.from "System" ; AssemblyId.from "System.Xml"; AssemblyId.from "System.Data" ] |> set
                       PackageReferences = Set.empty
                       Repository = RepositoryId.from "cassandra-sharp" } ] |> set
        Applications = Set.empty }

    antho1 |> should equal antho2
