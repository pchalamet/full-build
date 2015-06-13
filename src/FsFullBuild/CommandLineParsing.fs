﻿module CommandLineParsing
open Types
open CommandLineToken

type Command =
    | Help
    | Usage
    | InitWorkspace of RelativePath
    | RefreshWorkspace
    | IndexWorkspace
    | ConvertWorkspace
    | OptimizeWorkspace
    | BookmarkWorkspace
    | CheckoutWorkspace of WorkspaceVersion
    | AddRepository of Vcs
    | CloneRepositories of NameFilter
    | ListRepositories
    | AddNuGet of Url
    | ListNuGets
    | ListPackages
    | InstallPackages
    | UpgradePackages
    | UsePackage of Name * PackageVersion
    | CheckPackages
    | InitView of Name * NameFilter
    | DropView of Name
    | ListViews
    | DescribeView of Name
    | GraphView of Name
    | GenerateView of Name
    | BuildView of Name    
    | RefreshSources
    | ListBinaries

let ParseWorkspace (args : string list) =
    match args with
    | [Token(Create); wsPath] -> let (ToRelativePath wsPath) = wsPath
                                 Command.InitWorkspace wsPath
    | [Token(Index)] -> Command.IndexWorkspace
    | [Token(Update)] -> RefreshWorkspace
    | _ -> Command.Help

let ParseView (args : string list) =
    match args with
    | [Token (Token.Create); vwName; vwFilter] -> Command.InitView (vwName, vwFilter)
    | [Token (Token.Drop); vwName] -> Command.DropView (vwName)
    | [Token (Token.Build); vwName] -> Command.BuildView (vwName)
    | [Token (Token.Graph); vwName] -> Command.GraphView (vwName)
    | _ -> Command.Help

let ParsePackage (args : string list) =
    match args with
    | [Token(Token.List)] -> ListPackages
    | [Token(Token.Update)] -> InstallPackages
    | [Token(Token.Check)] -> CheckPackages
    | [Token(Token.Upgrade)] -> UpgradePackages
    | [Token(Token.Add); name; version] -> let (ToName pkgName) = name
                                           let (ToWorkspaceVersion wsVersion) = version
                                           UsePackage (pkgName, wsVersion)
    | _ -> Command.Help

let ParseRepo (args : string list) =
    match args with
    | [Token(Token.Clone); filter] -> let (ToNameFilter repoFilter) = filter
                                      CloneRepositories (repoFilter)
    | [Token(Token.Add); vcs; name; url] -> let (ToVcs repoVcs) = (vcs, name, url)
                                            AddRepository (repoVcs)
    | [Token(Token.List)] -> ListRepositories
    | _ -> Command.Help




let ParseCommandLine (args : string list) : Command =
    match args with
    | head::tail -> match head with
                    | Token (Token.Help) -> Command.Help
                    | Token (Token.Workspace) -> ParseWorkspace tail
                    | Token (Token.View) -> ParseView tail
                    | Token (Token.Package) -> ParsePackage tail
                    | Token (Token.Repo) -> ParseRepo tail
                    | _ -> Command.Usage
    | _ -> Command.Usage  

//
//let ParseCommandLine (args : string list) : Command =
//    match args with
//    | [Token(Token.Help)] -> Help
//    | [Token(Token.Workspace); Token(Token.Create); path] -> let (ToRelativePath wsPath) = path
//                                                             InitWorkspace (wsPath)
//    | [Token(Token.Workspace); Token(Token.Update)] -> RefreshWorkspace // RefreshSources
//    | [Token(Token.Workspace); Token(Token.Index)] -> IndexWorkspace
//    | [Token(Token.Workspace); Token(Token.Convert)] -> ConvertWorkspace
//    | [Token(Token.Repo); Token(Token.Clone); filter] -> let (ToNameFilter repoFilter) = filter
//                                                         CloneRepositories (repoFilter)
//    | [Token(Token.Repo); Token(Token.Add); vcs; name; url] -> let (ToVcs repoVcs) = (vcs, name, url)
//                                                               AddRepository (repoVcs)
//    | [Token(Token.Repo); Token(Token.List)] -> ListRepositories
//    //| ["optimize"; "workspace"] -> OptimizeWorkspace
//    | [Token(Token.Workspace); Token(Token.Bookmark)] -> BookmarkWorkspace
//    | [Token(Token.Workspace); Token(Token.Checkout); version] -> let (ToWorkspaceVersion wsVersion) = version
//                                                                  CheckoutWorkspace (wsVersion)
//    | [Token(Token.NuGet); Token(Token.Add); url] -> let (ToUrl ngUrl) = url
//                                                     AddNuGet (ngUrl)
//    | [Token(Token.NuGet); Token(Token.List)] -> ListNuGets
//    | [Token(Token.Package); Token(Token.List)] -> ListPackages
//    | [Token(Token.Package); Token(Token.Update)] -> InstallPackages
//    | ["check"; "packages"] -> CheckPackages
//    | ["upgrade"; "packages"] -> UpgradePackages
//    | ["use"; "packages"; name; version] -> let (ToName pkgName) = name
//                                            let (ToWorkspaceVersion wsVersion) = version
//                                            UsePackage (pkgName, wsVersion)
//    | ["init"; "view"; name; filter] -> let (ToName vwName) = name
//                                        let (ToNameFilter vwFilter) = filter
//                                        InitView (vwName, vwFilter)
//    | ["drop"; "view"; name] -> let (ToName vwName) = name
//                                DropView (vwName)
//    | ["list"; "views"] -> ListViews
//    | ["describe"; "view"; name] -> let (ToName vwName) = name
//                                    DescribeView (vwName)
//    | ["graph"; "view"; name] -> let (ToName vwName) = name
//                                 GraphView (vwName)
//    | ["generate"; "view"; name] -> let (ToName vwName) = name
//                                    GenerateView (vwName)
//    | ["build"; "view"; name] -> let (ToName vwName) = name
//                                 BuildView (vwName)
//    | ["list"; "binaries"] -> ListBinaries
//    | _ -> Usage

let DisplayUsage () = 
    printfn "Usage: TBD"

let DisplayHelp () =
    printfn "Help : TBD"