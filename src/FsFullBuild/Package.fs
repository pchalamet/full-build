﻿// Copyright (c) 2014-2015, Pierre Chalamet
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
//     * Redistributions of source code must retain the above copyright
//       notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright
//       notice, this list of conditions and the following disclaimer in the
//       documentation and/or other materials provided with the distribution.
//     * Neither the name of Pierre Chalamet nor the
//       names of its contributors may be used to endorse or promote products
//       derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL PIERRE CHALAMET BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
module Package 
open Anthology
open IoHelpers
open System.IO
open System.Xml.Linq
open System.Linq
open MsBuildHelpers
open Env
open Collections
open Simplify

let FxVersion2Folder =
    [ ("v4.6", ["net46"]) 
      ("v4.5.4", ["net454"])
      ("v4.5.3", ["net453"])
      ("v4.5.2", ["net452"])
      ("v4.5.1", ["net451"])
      ("v4.5", ["45"; "net45"; "net45-full"])
      ("v4.0", ["40"; "net4"; "net40"; "net40-full"; "net40-client"])
      ("v3.5", ["35"; "net35"; "net35-full"])
      ("v2.0", ["20"; "net20"; "net20-full"; "net"])
      ("v1.1", ["11"; "net11"])    
      ("v1.0", ["10"]) ] 


let GenerateItemGroupContent (pkgDir : DirectoryInfo) (files : FileInfo seq) =
    seq {
        for file in files do
            let assemblyName = Path.GetFileNameWithoutExtension (file.FullName)
            let relativePath = ComputeRelativePath pkgDir file
            let hintPath = sprintf "%s%s" MSBUILD_PACKAGE_FOLDER relativePath |> ToUnix
            yield XElement(NsMsBuild + "Reference",
                    XAttribute(NsNone + "Include", assemblyName),
                    XElement(NsMsBuild + "HintPath", hintPath),
                    XElement(NsMsBuild + "Private", "true"))
    }

let GenerateItemGroup (fxLibs : DirectoryInfo) =
    let pkgDir = WorkspacePackageFolder ()
    let dlls = fxLibs.EnumerateFiles("*.dll")
    let exes = fxLibs.EnumerateFiles("*.exes")
    let files = Seq.append dlls exes
    GenerateItemGroupContent pkgDir files

let rec GenerateWhenContent (fxFolders : DirectoryInfo seq) (fxVersion : string) (nugetFolderAliases : string list) =
    let matchLibfolder (fx : string) (dir : DirectoryInfo) =
        if fx = "" then true
        else
            let dirNames = dir.Name.Replace("portable-", "").Split('+')
            dirNames |> Seq.contains fx

    match nugetFolderAliases with
    | [] -> null
    | fxFolder::tail -> let libDir = fxFolders |> Seq.tryFind (matchLibfolder fxFolder)
                        match libDir with
                        | None -> GenerateWhenContent fxFolders fxVersion tail
                        | Some libFolder -> let itemGroup = GenerateItemGroup libFolder
                                            if itemGroup.Any() then
                                                let condition = sprintf "'$(TargetFrameworkVersion)' == '%s'" fxVersion
                                                XElement(NsMsBuild + "When",
                                                    XAttribute(NsNone + "Condition", condition),
                                                    XElement(NsMsBuild + "ItemGroup", 
                                                        itemGroup))
                                            else
                                                null

let GenerateChooseContent (libDir : DirectoryInfo) =
    let whens = seq {
            if libDir.Exists then
                let fxFolders = libDir.EnumerateDirectories()
                for (fxName, _) in FxVersion2Folder do
                    let fxWhens = FxVersion2Folder |> Seq.skipWhile (fun (fx, _) -> fx <> fxName)
                                                   |> Seq.map (fun (_, folders) -> GenerateWhenContent fxFolders fxName folders)
                    let fxDefaultWhen = [ [""] ] |> Seq.map (fun folders -> GenerateWhenContent [libDir] fxName folders)
                    let fxWhen = Seq.append fxWhens fxDefaultWhen |> Seq.tryFind (fun x -> x <> null)

                    match fxWhen with
                    | Some x -> yield x
                    | None -> ()
        }
    if whens.Any() then XElement (NsMsBuild + "Choose", whens)
    else null
    
let GenerateDependenciesContent (dependencies : PackageId seq) =
    seq {
        for dependency in dependencies do
            let depId = dependency.Value
            let dependencyTargets = sprintf "%s%s/package.targets" MSBUILD_PACKAGE_FOLDER depId
            let pkgProperty = PackagePropertyName depId
            let condition = sprintf "'$(%s)' == ''" pkgProperty
    
            yield XElement(NsMsBuild + "Import",
                      XAttribute(NsNone + "Project", dependencyTargets),
                      XAttribute(NsNone + "Condition", condition))
    }

let GenerateProjectContent (package : PackageId) (imports : XElement seq) (choose : XElement) =
    let defineName = PackagePropertyName (package.Value)
    let propCondition = sprintf "'$(%s)' == ''" defineName
    let project = XElement (NsMsBuild + "Project",
                    XAttribute (NsNone + "Condition", propCondition),
                    XElement (NsMsBuild + "PropertyGroup",
                        XElement (NsMsBuild + defineName, "Y")),
                    imports,
                    choose)
    project


let GenerateTargetForPackage (package : PackageId) =
    let pkgsDir = Env.WorkspacePackageFolder ()
    let pkgDir = pkgsDir |> GetSubDirectory (package.Value)
    let libDir = pkgDir |> GetSubDirectory "lib" 
    
    let nuspecFile = pkgDir |> GetFile (IoHelpers.AddExt (package.Value) NuSpec)
    let xnuspec = XDocument.Load (nuspecFile.FullName)
    let dependencies = NuGets.GetPackageDependencies xnuspec

    let imports = GenerateDependenciesContent dependencies
    let choose = GenerateChooseContent libDir
    let project = GenerateProjectContent package imports choose

    let targetFile = pkgDir |> GetFile "package.targets" 
    project.Save (targetFile.FullName)

let GatherAllAssemblies (package : PackageId) : AssemblyId set =
    let pkgsDir = Env.WorkspacePackageFolder ()
    let pkgDir = pkgsDir |> GetSubDirectory (package.Value)
    let dlls = pkgDir.EnumerateFiles("*.dll", SearchOption.AllDirectories)
    let exes = pkgDir.EnumerateFiles("*.exes", SearchOption.AllDirectories)
    let files = Seq.append dlls exes
    files |> Seq.map (fun x -> AssemblyId.Bind x) 
          |> set

let Install () =
    let pkgDir = Env.WorkspacePackageFolder ()
    pkgDir.Delete (true)

    let config = Configuration.GlobalConfig
    PaketParsing.UpdateSources config.NuGets

    let confDir = Env.WorkspaceConfigFolder ()
    Exec.Exec "paket.exe" "install" confDir.FullName

    let allPackages = NuGets.BuildPackageDependencies (PaketParsing.ParsePaketDependencies ())
                      |> Map.toList
                      |> Seq.map (fun (k, v) -> k)
    allPackages |> Seq.iter GenerateTargetForPackage

let Update () =
    let confDir = Env.WorkspaceConfigFolder ()
    Exec.Exec "paket.exe" "update" confDir.FullName
    
    let allPackages = NuGets.BuildPackageDependencies (PaketParsing.ParsePaketDependencies ())
                      |> Map.toList
                      |> Seq.map (fun (k, v) -> k)
    allPackages |> Seq.iter GenerateTargetForPackage

let Outdated () =
    let confDir = Env.WorkspaceConfigFolder ()
    Exec.Exec "paket.exe" "outdated" confDir.FullName

let List () =
    let confDir = Env.WorkspaceConfigFolder ()
    Exec.Exec "paket.exe" "show-installed-packages" confDir.FullName


let RemoveUnusedPackages (antho : Anthology) =
    let packages = PaketParsing.ParsePaketDependencies ()
    let usedPackages = antho.Projects |> Set.map (fun x -> x.PackageReferences)
                                      |> Set.unionMany
    let packagesToRemove = packages |> Set.filter (fun x -> (not << Set.contains x) usedPackages)
    PaketParsing.RemoveDependencies packagesToRemove

let SimplifyAnthology (antho) =
    let packages = antho.Projects |> Set.map (fun x -> x.PackageReferences)
                                  |> Set.unionMany
    let package2packages = NuGets.BuildPackageDependencies packages
    let allPackages = package2packages |> Seq.map (fun x -> x.Key) 
    let package2files = allPackages |> Seq.map (fun x -> (x, GatherAllAssemblies x)) 
                                    |> Map
    let newAntho = SimplifyAnthology antho package2files package2packages
    RemoveUnusedPackages newAntho
    newAntho

let Simplify () =
    Install ()
    
    let antho = Configuration.LoadAnthology ()
    let newAntho = SimplifyAnthology antho
    Configuration.SaveAnthology newAntho

