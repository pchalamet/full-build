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

module Graph
open Collections



[<RequireQualifiedAccess>] 
type PackageVersion =
    | PackageVersion of string
    | Unspecified

[<RequireQualifiedAccess>]
type OutputType =
    | Exe
    | Dll

[<RequireQualifiedAccess>]
type PublisherType =
    | Copy
    | Zip
    | Docker

[<RequireQualifiedAccess>]
type BuilderType =
    | MSBuild
    | Skip

[<RequireQualifiedAccess>]
type VcsType =
    | Gerrit
    | Git
    | Hg

[<RequireQualifiedAccess>]
type TestRunnerType =
    | NUnit


// =====================================================================================================

[<CustomEquality; CustomComparison>]
type Package =
    { Graph : Graph
      Package :Anthology.PackageId }

    override this.Equals(other : System.Object) =
        System.Object.ReferenceEquals(this, other)

    override this.GetHashCode() : int =
        this.Package.GetHashCode()

    interface System.IComparable with
        member this.CompareTo(other) =
            match other with
            | :? Package as x -> System.Collections.Generic.Comparer<Anthology.PackageId>.Default.Compare(this.Package, x.Package)
            | _ -> failwith "Can't compare values with different types"

    member this.Name = this.Package.toString

// =====================================================================================================

and [<CustomEquality; CustomComparison>] Assembly = 
    { Graph : Graph
      Assembly : Anthology.AssemblyId }
    override this.Equals(other : System.Object) =
        System.Object.ReferenceEquals(this, other)

    override this.GetHashCode() : int =
        this.Assembly.GetHashCode()

    interface System.IComparable with
        member this.CompareTo(other) =
            match other with
            | :? Assembly as x -> System.Collections.Generic.Comparer<Anthology.AssemblyId>.Default.Compare(this.Assembly, x.Assembly)
            | _ -> failwith "Can't compare values with different types"

    member this.Name = this.Assembly.toString

// =====================================================================================================

and [<CustomEquality; CustomComparison>] Application =
    { Graph : Graph
      Application : Anthology.Application } 

    override this.Equals(other : System.Object) =
        System.Object.ReferenceEquals(this, other)

    override this.GetHashCode() : int =
        this.Application.GetHashCode()

    interface System.IComparable with
        member this.CompareTo(other) =
            match other with
            | :? Application as x -> System.Collections.Generic.Comparer<Anthology.ApplicationId>.Default.Compare(this.Application.Name, x.Application.Name)
            | _ -> failwith "Can't compare values with different types"

    member this.Name = this.Application.Name.toString

    member this.Publisher = match this.Application.Publisher with
                            | Anthology.PublisherType.Copy -> PublisherType.Copy
                            | Anthology.PublisherType.Zip -> PublisherType.Zip
                            | Anthology.PublisherType.Docker -> PublisherType.Docker

    member this.Projects =
        this.Application.Projects |> Set.map (fun x -> this.Graph.ProjectMap.[x])

// =====================================================================================================

and [<CustomEquality; CustomComparison>] Repository =
    { Graph : Graph
      Repository : Anthology.Repository }

    override this.Equals(other : System.Object) =
        System.Object.ReferenceEquals(this, other)

    override this.GetHashCode() : int =
        this.Repository.GetHashCode()

    interface System.IComparable with
        member this.CompareTo(other) =
            match other with
            | :? Repository as x -> System.Collections.Generic.Comparer<Anthology.RepositoryId>.Default.Compare(this.Repository.Name, x.Repository.Name)
            | _ -> failwith "Can't compare values with different types"

    member this.Name : string = this.Repository.Name.toString

    member this.Builder = 
        let buildableRepo = this.Graph.Anthology.Repositories |> Seq.tryFind (fun x -> x.Repository.Name = this.Repository.Name)
        match buildableRepo with
        | Some repo -> match repo.Builder with
                       | Anthology.BuilderType.MSBuild -> BuilderType.MSBuild
                       | Anthology.BuilderType.Skip -> BuilderType.Skip
        | _ -> BuilderType.Skip

    member this.Vcs = match this.Graph.Anthology.Vcs with
                      | Anthology.VcsType.Gerrit -> VcsType.Gerrit
                      | Anthology.VcsType.Git -> VcsType.Git
                      | Anthology.VcsType.Hg -> VcsType.Hg

    member this.Branch = match this.Repository.Branch with
                         | Some x -> x.toString
                         | None -> match this.Vcs with
                                   | VcsType.Gerrit | VcsType.Git -> "master"
                                   | VcsType.Hg -> "default"

    member this.Uri = this.Repository.Url.toString

    member this.Projects =
        let repositoryId = this.Repository.Name
        this.Graph.Anthology.Projects |> Set.filter (fun x -> x.Repository = repositoryId)
                                      |> Set.map (fun x -> this.Graph.ProjectMap.[x.ProjectId])

    member this.IsCloned =
        let wsDir = Env.GetFolder Env.Folder.Workspace
        let repoDir = wsDir |> IoHelpers.GetSubDirectory this.Name
        repoDir.Exists

// =====================================================================================================

and [<CustomEquality; CustomComparison>] Project =
    { Graph : Graph
      Project : Anthology.Project }

    override this.Equals(other : System.Object) =
        System.Object.ReferenceEquals(this, other)

    override this.GetHashCode() : int =
        this.Project.GetHashCode()

    interface System.IComparable with
        member this.CompareTo(other) =
            match other with
            | :? Project as x -> System.Collections.Generic.Comparer<Anthology.ProjectId>.Default.Compare(this.Project.ProjectId, x.Project.ProjectId)
            | _ -> failwith "Can't compare values with different types"

    member this.Repository =
        this.Graph.RepositoryMap.[this.Project.Repository]

    member this.Applications =
        let projectId = this.Project.ProjectId
        this.Graph.Anthology.Applications |> Set.filter (fun x -> x.Projects |> Set.contains projectId)
                                          |> Set.map (fun x -> this.Graph.ApplicationMap.[x.Name])

    member this.References =
        let referenceIds = this.Project.ProjectReferences
        referenceIds |> Set.map (fun x -> this.Graph.ProjectMap.[x])

    member this.ReferencedBy =
        let projectId = this.Project.ProjectId
        this.Graph.Anthology.Projects |> Set.filter (fun x -> x.ProjectReferences |> Set.contains projectId)
                                      |> Set.map (fun x -> this.Graph.ProjectMap.[x.ProjectId])

    member this.ProjectFile = 
        this.Project.RelativeProjectFile.toString

    member this.Output = this.Graph.AssemblyMap.[this.Project.Output]

    member this.BinFile = 
        let repo = this.Repository.Name
        let path = System.IO.Path.GetDirectoryName(this.ProjectFile)
        let ass = this.Output.Name
        let ext = match this.OutputType with
                  | OutputType.Dll -> "dll"
                  | OutputType.Exe -> "exe"
        sprintf "%s/%s/bin/%s.%s" repo path ass ext

    member this.UniqueProjectId = this.Project.UniqueProjectId.toString

    member this.ProjectId = this.Project.ProjectId.toString

    member this.OutputType = match this.Project.OutputType with
                             | Anthology.OutputType.Dll -> OutputType.Dll
                             | Anthology.OutputType.Exe -> OutputType.Exe

    member this.FxVersion = match this.Project.FxVersion.toString with
                            | null -> None
                            | x -> Some x

    member this.FxProfile = match this.Project.FxProfile.toString with
                            | null -> None
                            | x -> Some x

    member this.FxIdentifier = match this.Project.FxIdentifier.toString with
                               | null -> None
                               | x -> Some x

    member this.HasTests = this.Project.HasTests

    member this.AssemblyReferences = 
        this.Project.AssemblyReferences |> Set.map (fun x -> this.Graph.AssemblyMap.[x])

    member this.PackageReferences = 
        this.Project.PackageReferences |> Set.map (fun x -> this.Graph.PackageMap.[x])

// =====================================================================================================

and [<CustomEquality; CustomComparison>] Bookmark =
    { Graph : Graph
      Bookmark : Anthology.Bookmark }
with
    override this.Equals(other : System.Object) =
        System.Object.ReferenceEquals(this, other)

    override this.GetHashCode() : int =
        this.Bookmark.GetHashCode()

    interface System.IComparable with
        member this.CompareTo(other) =
            match other with
            | :? Bookmark as x -> System.Collections.Generic.Comparer<Anthology.RepositoryId>.Default.Compare(this.Bookmark.Repository, x.Bookmark.Repository)
            | _ -> failwith "Can't compare values with different types"

    member this.Repository =
        this.Graph.RepositoryMap.[this.Bookmark.Repository]
    member this.Version = this.Bookmark.Version.toString

// =====================================================================================================

and [<CustomEquality; CustomComparison>] Baseline =
    { Graph : Graph 
      Baseline : Anthology.Baseline }
with
    override this.Equals(other : System.Object) =
        System.Object.ReferenceEquals(this, other)

    override this.GetHashCode() : int =
        this.Baseline.GetHashCode()

    interface System.IComparable with
        member this.CompareTo(other) =
            match other with
            | :? Baseline as x -> System.Collections.Generic.Comparer<Anthology.Baseline>.Default.Compare(this.Baseline, x.Baseline)
            | _ -> failwith "Can't compare values with different types"

    member this.IsIncremental = false
    
    member this.Bookmarks = 
        this.Baseline.Bookmarks |> Set.map (fun x -> { Graph = this.Graph; Bookmark = x})
    
    member this.ModifiedRepositories : Repository set = 
        Set.empty

    member this.Save () = 
        Configuration.SaveBaseline this.Baseline

// =====================================================================================================

and [<CustomEquality; CustomComparison>] View =
    { Graph : Graph
      View : Anthology.View }
with
    override this.Equals(other : System.Object) =
        System.Object.ReferenceEquals(this, other)

    override this.GetHashCode() : int =
        this.View.GetHashCode()

    interface System.IComparable with
        member this.CompareTo(other) =
            match other with
            | :? View as x -> System.Collections.Generic.Comparer<Anthology.View>.Default.Compare(this.View, x.View)
            | _ -> failwith "Can't compare values with different types"

    member this.Name = this.View.Name
    member this.Filters = this.View.Filters
    member this.Parameters = this.View.Parameters
    member this.Dependencies = this.View.SourceOnly
    member this.ReferencedBy = this.View.Parents
    member this.Modified = this.View.Modified
    member this.Builder = match this.View.Builder with
                          | Anthology.BuilderType.MSBuild -> BuilderType.MSBuild
                          | Anthology.BuilderType.Skip -> BuilderType.Skip

    member this.Projects : Project set =
        let projects = PatternMatching.FilterMatch<Project> this.Graph.Projects 
                                                            (fun x -> sprintf "%s/%s" x.Repository.Name x.Output.Name) 
                                                            this.View.Filters
        projects

    member this.Save (isDefault : bool option) = 
        let viewId = Anthology.ViewId this.View.Name
        Configuration.SaveView viewId this.View isDefault

    member this.Delete () =
        Configuration.DeleteView (Anthology.ViewId this.View.Name)

// =====================================================================================================

and [<Sealed>] Graph(anthology : Anthology.Anthology) =
    let mutable assemblyMap : System.Collections.Generic.IDictionary<Anthology.AssemblyId, Assembly> = null
    let mutable packageMap : System.Collections.Generic.IDictionary<Anthology.PackageId, Package> = null
    let mutable repositoryMap : System.Collections.Generic.IDictionary<Anthology.RepositoryId, Repository> = null
    let mutable applicationMap : System.Collections.Generic.IDictionary<Anthology.ApplicationId, Application> = null
    let mutable projectMap : System.Collections.Generic.IDictionary<Anthology.ProjectId, Project> = null
    let mutable viewMap : System.Collections.Generic.IDictionary<Anthology.ViewId, View> = null

    member this.Anthology : Anthology.Anthology = anthology

    member this.PackageMap : System.Collections.Generic.IDictionary<Anthology.PackageId, Package> =
        if packageMap |> isNull then
            packageMap <- anthology.Projects |> Set.map (fun x -> x.PackageReferences)
                                             |> Set.unionMany
                                             |> Seq.map (fun x -> x, { Graph = this; Package = x})
                                             |> dict
        packageMap

    member this.AssemblyMap : System.Collections.Generic.IDictionary<Anthology.AssemblyId, Assembly> =
        if assemblyMap |> isNull then 
            let outputAss = anthology.Projects |> Seq.map (fun x -> x.Output)
                                               |> Set
            assemblyMap <- anthology.Projects |> Set.map (fun x -> x.AssemblyReferences)
                                              |> Set.unionMany
                                              |> Set.union outputAss
                                              |> Seq.map (fun x -> x, { Graph = this; Assembly = x})
                                              |> dict
        assemblyMap

    member this.RepositoryMap : System.Collections.Generic.IDictionary<Anthology.RepositoryId, Repository> =
        if repositoryMap |> isNull then 
            repositoryMap <- anthology.Repositories |> Seq.map (fun x -> x.Repository.Name, { Graph = this; Repository = x.Repository})                                 
                                                    |> dict
        repositoryMap

    member this.ApplicationMap : System.Collections.Generic.IDictionary<Anthology.ApplicationId, Application> =
        if applicationMap |> isNull then
            applicationMap <- anthology.Applications |> Seq.map (fun x -> x.Name, { Graph = this; Application = x } )
                                                     |> dict
        applicationMap

    member this.ProjectMap : System.Collections.Generic.IDictionary<Anthology.ProjectId, Project> = 
        if projectMap |> isNull then
            projectMap <- anthology.Projects |> Seq.map (fun x -> x.ProjectId, { Graph = this; Project = x } )
                                             |> dict
        projectMap

    member this.ViewMap : System.Collections.Generic.IDictionary<Anthology.ViewId, View> = 
        if viewMap |> isNull then
            let vwDir = Env.GetFolder Env.Folder.View
            viewMap <- vwDir.EnumerateFiles("*.view") |> Seq.map (fun x -> System.IO.Path.GetFileNameWithoutExtension(x.Name) |> Anthology.ViewId)
                                                      |> Seq.map Configuration.LoadView
                                                      |> Seq.map (fun x -> x.Name |> Anthology.ViewId, { Graph = this; View = x })
                                                      |> dict
        viewMap

    member this.MasterRepository = { Graph = this; Repository = this.Anthology.MasterRepository }

    member this.Repositories = this.RepositoryMap.Values |> set

    member this.Assemblies = this.AssemblyMap.Values |> set

    member this.Packages = this.PackageMap.Values |> set

    member this.Applications = this.ApplicationMap.Values |> set

    member this.Projects = this.ProjectMap.Values |> set

    member this.Views = this.ViewMap.Values |> set

    member this.DefaultView =
        let viewId = Configuration.DefaultView ()
        match viewId with
        | None -> None
        | Some x -> Some this.ViewMap.[x]

    member this.Baseline = 
        let baseline = Configuration.LoadBaseline()
        { Graph = this; Baseline = baseline }

    member this.CreateBaseline (incremental : bool) =
        this.Baseline

    member this.TestRunner =
        match this.Anthology.Tester with
        | Anthology.TestRunnerType.NUnit -> TestRunnerType.NUnit

    member this.ArtifactsDir = this.Anthology.Artifacts

    member this.CreateView name filters parameters dependencies referencedBy modified builder =
        let view = { Anthology.View.Name = name 
                     Anthology.View.Filters = filters
                     Anthology.View.Parameters = parameters
                     Anthology.View.SourceOnly = dependencies
                     Anthology.View.Parents = referencedBy
                     Anthology.View.Modified = modified
                     Anthology.View.Builder = match builder with
                                              | BuilderType.MSBuild -> Anthology.BuilderType.MSBuild
                                              | BuilderType.Skip -> Anthology.BuilderType.Skip }

        { Graph = this
          View = view }

    member this.Save () =
        Configuration.SaveAnthology this.Anthology


// =====================================================================================================


let from (antho : Anthology.Anthology) : Graph =
    Graph(antho)

let create (uri : string) (artifacts : string) vcs runner =
    let repo = { Anthology.Name = Anthology.RepositoryId.from Env.MASTER_REPO
                 Anthology.Url = Anthology.RepositoryUrl.from uri
                 Anthology.Branch = None }

    let anthoVcs = match vcs with
                   | VcsType.Gerrit -> Anthology.VcsType.Gerrit
                   | VcsType.Git -> Anthology.VcsType.Git
                   | VcsType.Hg -> Anthology.VcsType.Hg

    let anthoRunner = match runner with
                      | TestRunnerType.NUnit -> Anthology.TestRunnerType.NUnit

    let antho = { Anthology.Anthology.MinVersion = Env.FullBuildVersion().ToString()
                  Anthology.Anthology.Artifacts = artifacts
                  Anthology.Anthology.NuGets = []
                  Anthology.Anthology.MasterRepository = repo
                  Anthology.Anthology.Repositories = Set.empty
                  Anthology.Anthology.Projects = Set.empty
                  Anthology.Anthology.Applications = Set.empty
                  Anthology.Anthology.Tester = anthoRunner
                  Anthology.Anthology.Vcs = anthoVcs }
    from antho


let init uri vcs =
    create uri "dummy" vcs TestRunnerType.NUnit
