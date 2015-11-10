﻿module MsBuildHelpersTests

open FsUnit
open NUnit.Framework
open MsBuildHelpers
open System.Xml.Linq
open Anthology
open StringHelpers

[<Test>]
let CheckCast () =
    let xel = XElement (NsNone + "Toto", 42)
    let i = !> xel : int
    i |> should equal 42

[<Test>]
let CheckProjectPropertyName () =
    let project = { Output = AssemblyId.from "cqlplus"
                    ProjectId = ProjectRef.from "CqlPlus"
                    OutputType = OutputType.Exe
                    UniqueProjectId = ProjectId.from (ParseGuid "0a06398e-69be-487b-a011-4c0be6619b59")
                    RelativeProjectFile = ProjectRelativeFile "cqlplus/cqlplus-net45.csproj"
                    FxTarget = FrameworkVersion "v4.5"
                    ProjectReferences = [ ProjectRef.from "cassandrasharp.interfaces"; ProjectRef.from "cassandrasharp" ] |> set
                    PackageReferences = Set.empty
                    AssemblyReferences = [ AssemblyId.from("System") ; AssemblyId.from("System.Xml") ] |> set
                    Repository = RepositoryId.from "cassandra-sharp" }

    let propName = ProjectPropertyName project
    propName |> should equal "Prj_cqlplus"

[<Test>]
let CheckPackagePropertyName () =
    let package = "Rx-Core"
    let propName = PackagePropertyName package
    propName |> should equal "FullBuild_Rx_Core_Pkg"
