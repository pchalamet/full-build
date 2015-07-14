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

module Repo

open Types
open Configuration
open System.Text.RegularExpressions
open WellknownFolders

let rec List2 (repos : Repository list) =
    match repos with
    | repo::tail -> printfn "%s : %s [%A]" repo.Name repo.Url repo.Vcs
                    List2 tail
    | [] -> ()

let List () =
    let wsConfig = WorkspaceConfig ()
    List2 wsConfig.Repositories

let MatchRepo (repo : Repository seq) (filter : string) =
    let matchRegex = "^" + filter + "$"
    let regex = new Regex(matchRegex, RegexOptions.IgnoreCase)
    repo |> Seq.filter ( fun x -> regex.IsMatch(x.Name)) |> Seq.distinct

let Clone (filters : string list) =
    let wsDir = WorkspaceFolder ()
    let wsConfig = WorkspaceConfig ()
    let res = filters |> Seq.map (MatchRepo wsConfig.Repositories) |> Seq.collect (fun x -> x) |> Seq.distinct
    res |> Seq.iter (Vcs.VcsCloneRepo wsDir)
