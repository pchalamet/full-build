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

module SimplifyTests

open FsUnit
open NUnit.Framework
open Anthology
open StringHelpers
open System.IO
open TestHelpers


let loadAnthology anthologyFile =
    let anthologyFile = FileInfo(testFile anthologyFile)
    let anthology = AnthologySerializer.Load anthologyFile
    anthology

[<Test>]
let CheckSimplifyAssemblies () =
    let anthology = loadAnthology "indexed-anthology.yaml"

    let package2Files = Map.empty

    let lognetunittestsRef = ProjectUniqueId.from (ParseGuid "9e8648a4-d25a-4cfa-aaee-20d9d63ff571")
    let cassandraSharpAssName = AssemblyId.from "cassandrasharp"
    let cassandraSharpItfAssName = AssemblyId.from "cassandrasharp.interfaces"
    let cassandraSharpPrjRef = ProjectId.from "cassandrasharp"
    let cassandraSharpItfPrjRef = ProjectId.from "cassandrasharp.interfaces"

    let lognetunittests = anthology.Projects |> Seq.find (fun x -> x.UniqueProjectId = lognetunittestsRef)
    lognetunittests.AssemblyReferences |> should contain cassandraSharpAssName
    lognetunittests.AssemblyReferences |> should contain cassandraSharpItfAssName
    lognetunittests.ProjectReferences |> should not' (contain cassandraSharpPrjRef)
    lognetunittests.ProjectReferences |> should not' (contain cassandraSharpItfPrjRef)

    let simplifedProjects = Core.Simplify.TransformSingleAssemblyToProjectOrPackage package2Files anthology.Projects
    let simplifiedlognetunittests = simplifedProjects |> Seq.find (fun x -> x.UniqueProjectId = lognetunittestsRef)

    simplifiedlognetunittests.AssemblyReferences |> should not' (contain cassandraSharpAssName)
    simplifiedlognetunittests.AssemblyReferences |> should not' (contain cassandraSharpItfAssName)
    simplifiedlognetunittests.ProjectReferences |> should contain cassandraSharpPrjRef
    simplifiedlognetunittests.ProjectReferences |> should contain cassandraSharpItfPrjRef

[<Test>]
let CheckSimplifyAnthology () =
    let anthology = loadAnthology "indexed-anthology.yaml"
    let expectedAnthology = loadAnthology "simplified-anthology.yaml"

    let package2files = Map [ (PackageId.from "log4net", Set [AssemblyId.from "log4net"])
                              (PackageId.from "Moq", Set [AssemblyId.from "moq"; AssemblyId.from "Moq.Silverlight" ])
                              (PackageId.from "Nunit", Set [AssemblyId.from "nunit.framework"])
                              (PackageId.from "Rx-Core", Set [AssemblyId.from "System.Reactive.Core"])
                              (PackageId.from "Rx-Interfaces", Set [AssemblyId.from "System.Reactive.Interfaces"])
                              (PackageId.from "Rx-Linq", Set [AssemblyId.from "System.Reactive.Linq"])
                              (PackageId.from "Rx-Main", Set.empty)
                              (PackageId.from "Rx-PlatformServices", Set [AssemblyId.from "System.Reactive.PlatformServices"])
                              (PackageId.from "cassandra-sharp", Set.empty)
                              (PackageId.from "cassandra-sharp-core", Set [AssemblyId.from "CassandraSharp"])
                              (PackageId.from "cassandra-sharp-interfaces", Set [AssemblyId.from "CassandraSharp.Interfaces"]) ]

    let package2packages = Map [ (PackageId.from "log4net", Set.empty)
                                 (PackageId.from "Moq", Set.empty)
                                 (PackageId.from "Nunit", Set.empty)
                                 (PackageId.from "Rx-Core", Set [PackageId.from "Rx-Interfaces"])
                                 (PackageId.from "Rx-Interfaces", Set.empty)
                                 (PackageId.from "Rx-Linq", Set [PackageId.from "Rx-Core"; PackageId.from "Rx-Interfaces"])
                                 (PackageId.from "Rx-Main", Set [PackageId.from "Rx-Core"; PackageId.from "Rx-Interfaces"; 
                                                                 PackageId.from "Rx-Linq"; PackageId.from "Rx-PlatformServices"])
                                 (PackageId.from "Rx-PlatformServices", Set [PackageId.from "Rx-Core"; PackageId.from "Rx-Interfaces"])
                                 (PackageId.from "cassandra-sharp", Set [PackageId.from "cassandra-sharp-core"; PackageId.from "cassandra-sharp-interfaces"])
                                 (PackageId.from "cassandra-sharp-core", Set [PackageId.from "Rx-Main"])
                                 (PackageId.from "cassandra-sharp-interfaces", Set.empty) ]

    let newAnthology = Core.Simplify.SimplifyAnthologyWithPackages anthology package2files package2packages
    newAnthology |> should equal expectedAnthology

[<Test>]
let CheckConflictsWithSameGuid () =
    let p1 = { Output = AssemblyId.from "cqlplus"
               ProjectId = ProjectId.from "cqlplus"
               OutputType = OutputType.Exe
               UniqueProjectId = ProjectUniqueId.from (ParseGuid "0a06398e-69be-487b-a011-4c0be6619b59")
               RelativeProjectFile = ProjectRelativeFile "cqlplus/cqlplus-net45.csproj"
               FxVersion = FxInfo.from "v4.5"
               FxProfile = FxInfo.from null
               FxIdentifier = FxInfo.from null
               HasTests = false
               ProjectReferences = [ ProjectId.from "cassandrasharp"; ProjectId.from "cassandrasharp.interfaces" ] |> set
               AssemblyReferences = [ AssemblyId.from "System" ; AssemblyId.from "System.Xml"; AssemblyId.from "System.Data" ] |> set
               PackageReferences = Set.empty
               Repository = RepositoryId.from "cassandra-sharp" } 

    let p2 = { Output = AssemblyId.from "cqlplus2"
               ProjectId = ProjectId.from "cqlplus"
               OutputType = OutputType.Exe
               UniqueProjectId = ProjectUniqueId.from (ParseGuid "0a06398e-69be-487b-a011-4c0be6619b59")
               RelativeProjectFile = ProjectRelativeFile "cqlplus/cqlplus-net45.csproj"
               FxVersion = FxInfo.from "v4.5"
               FxProfile = FxInfo.from null
               FxIdentifier = FxInfo.from null
               HasTests = false
               ProjectReferences = [ ProjectId.from "cassandrasharp"; ProjectId.from "cassandrasharp.interfaces" ] |> set
               AssemblyReferences = [ AssemblyId.from "System" ; AssemblyId.from "System.Xml"; AssemblyId.from "System.Data" ] |> set
               PackageReferences = Set.empty
               Repository = RepositoryId.from "cassandra-sharp2" }

    let conflictsSameGuid = Core.Indexation.findConflicts [p1; p2] |> List.ofSeq
    conflictsSameGuid |> should equal [Core.Indexation.SameGuid (p1, p2)]

[<Test>]
let CheckConflictsWithSameOutput () =
    let p1 = { Output = AssemblyId.from "cqlplus"
               ProjectId = ProjectId.from "cqlplus"
               OutputType = OutputType.Exe
               UniqueProjectId = ProjectUniqueId.from (ParseGuid "0a06398e-69be-487b-a011-4c0be6619b59")
               RelativeProjectFile = ProjectRelativeFile "cqlplus/cqlplus-net45.csproj"
               FxVersion = FxInfo.from "v4.5"
               FxProfile = FxInfo.from null
               FxIdentifier = FxInfo.from null
               HasTests = false
               ProjectReferences = [ ProjectId.from "cassandrasharp"; ProjectId.from "cassandrasharp.interfaces" ] |> set
               AssemblyReferences = [ AssemblyId.from "System" ; AssemblyId.from "System.Xml"; AssemblyId.from "System.Data" ] |> set
               PackageReferences = Set.empty
               Repository = RepositoryId.from "cassandra-sharp" } 

    let p2 = { Output = AssemblyId.from "cqlplus"
               ProjectId = ProjectId.from "cqlplus"
               OutputType = OutputType.Exe
               UniqueProjectId = ProjectUniqueId.from (ParseGuid "39787692-f8f8-408d-9557-0c40547c1563")
               RelativeProjectFile = ProjectRelativeFile "cqlplus/cqlplus-net45.csproj"
               FxVersion = FxInfo.from "v4.5"
               FxProfile = FxInfo.from null
               FxIdentifier = FxInfo.from null
               HasTests = false
               ProjectReferences = [ ProjectId.from "cassandrasharp"; ProjectId.from "cassandrasharp.interfaces" ] |> set
               AssemblyReferences = [ AssemblyId.from "System" ; AssemblyId.from "System.Xml"; AssemblyId.from "System.Data" ] |> set
               PackageReferences = Set.empty
               Repository = RepositoryId.from "cassandra-sharp2" }

    let conflictsSameGuid = Core.Indexation.findConflicts [p1; p2] |> List.ofSeq
    conflictsSameGuid |> should equal [Core.Indexation.SameOutput (p1, p2)]

[<Test>]
let CheckNoConflictsSameProjectName () =
    let p1 = { Output = AssemblyId.from "cqlplus"
               ProjectId = ProjectId.from "cqlplus"
               OutputType = OutputType.Exe
               UniqueProjectId = ProjectUniqueId.from (ParseGuid "0a06398e-69be-487b-a011-4c0be6619b59")
               RelativeProjectFile = ProjectRelativeFile "cqlplus/cqlplus-net45.csproj"
               FxVersion = FxInfo.from "v4.5"
               FxProfile = FxInfo.from null
               FxIdentifier = FxInfo.from null
               HasTests = false
               ProjectReferences = [ ProjectId.from "cassandrasharp"; ProjectId.from "cassandrasharp.interfaces" ] |> set
               AssemblyReferences = [ AssemblyId.from "System" ; AssemblyId.from "System.Xml"; AssemblyId.from "System.Data" ] |> set
               PackageReferences = Set.empty
               Repository = RepositoryId.from "cassandra-sharp" } 

    let p2 = { Output = AssemblyId.from "cqlplus2"
               ProjectId = ProjectId.from "cqlplus"
               OutputType = OutputType.Exe
               UniqueProjectId = ProjectUniqueId.from (ParseGuid "39787692-f8f8-408d-9557-0c40547c1563")
               RelativeProjectFile = ProjectRelativeFile "cqlplus/cqlplus-net45.csproj"
               FxVersion = FxInfo.from "v4.5"
               FxProfile = FxInfo.from null
               FxIdentifier = FxInfo.from null
               HasTests = false
               ProjectReferences = [ ProjectId.from "cassandrasharp"; ProjectId.from "cassandrasharp.interfaces" ] |> set
               AssemblyReferences = [ AssemblyId.from "System" ; AssemblyId.from "System.Xml"; AssemblyId.from "System.Data" ] |> set
               PackageReferences = Set.empty
               Repository = RepositoryId.from "cassandra-sharp2" }

    let conflictsSameGuid = Core.Indexation.findConflicts [p1; p2] |> List.ofSeq
    conflictsSameGuid |> should equal []
