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

module Baselines

open Collections
open Graph

[<RequireQualifiedAccess; Sealed>]
type TagInfo =
    member Branch: string

    member Version: string

    member Format: unit
                -> string

    static member Parse: string
                      -> TagInfo

type [<Sealed>] Bookmark = interface System.IComparable
with
    member Repository : Repository
    member Version : string

and [<Sealed>] Baseline = interface System.IComparable
with
    member Info: TagInfo

    member Bookmarks: Bookmark set

    member IsHead : bool

    static member (-): Baseline*Baseline
                    -> Bookmark set
    member Save: comment : string
              -> unit


and [<Sealed>] Factory =
    member FindBaseline: unit 
                      -> Baseline

    member CreateBaseline: buildNumber : string
                        -> Baseline

val from: graph : Graph
       -> Factory
