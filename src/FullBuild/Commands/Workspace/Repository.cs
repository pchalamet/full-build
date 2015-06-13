// Copyright (c) 2014, Pierre Chalamet
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

using System.Collections.Generic;
using FullBuild.Config;
using FullBuild.NatLangParser;

namespace FullBuild.Commands.Workspace
{
    public class Repository
    {
        public static IEnumerable<Matcher> Commands()
        {
            var path = Parameter<string>.Create("path");
            var repos = Parameter<string[]>.Create("regex");
            var vcs = Parameter<VersionControlType>.Create("vcs");
            var repo = Parameter<string>.Create("repoName");
            var url = Parameter<string>.Create("url");
            var version = Parameter<string>.Create("version");

            // init workspace
            yield return MatcherBuilder.Describe("initialize workspace in folder <path>")
                                       .Command("init")
                                       .Command("workspace")
                                       .Param(path)
                                       .Do(ctx => Workspace.InitWorkspace(ctx.Get(path)));

            // refresh workspace
            yield return MatcherBuilder.Describe("refresh workspace from remote")
                                       .Command("refresh")
                                       .Command("workspace")
                                       .Do(ctx => Workspace.RefreshWorkspace());

            yield return MatcherBuilder.Describe("index workspace with local changes")
                                       .Command("index")
                                       .Command("workspace")
                                       .Do(ctx => Workspace.IndexWorkspace());

            // convert projects
            yield return MatcherBuilder.Describe("convert projects to ensure compatibility with full-build")
                                       .Command("convert")
                                       .Command("projects")
                                       .Do(ctx => Workspace.ConvertProjects());

            // clone repo
            yield return MatcherBuilder.Describe("clone repositories which names matching {0}", repos)
                                       .Command("clone")
                                       .Command("repo")
                                       .Param(repos)
                                       .Do(ctx => Workspace.CloneRepo(ctx.Get(repos)));

            // refresh source
            yield return MatcherBuilder.Describe("refresh sources from source control")
                                       .Command("refresh")
                                       .Command("sources")
                                       .Do(ctx => Workspace.RefreshSources());

            // add repo
            yield return MatcherBuilder.Describe("add a new repository to the workspace")
                                       .Command("add")
                                       .Param(vcs)
                                       .Command("repo")
                                       .Param(repo)
                                       .Command("from")
                                       .Param(url)
                                       .Do(ctx => Workspace.AddRepo(ctx.Get(repo), ctx.Get(vcs), ctx.Get(url)));

            // list repos
            yield return MatcherBuilder.Describe("list repositories")
                                       .Command("list")
                                       .Command("repos")
                                       .Do(ctx => Workspace.ListRepos());

            // optimize anthology
            yield return MatcherBuilder.Describe("optimize workspace")
                                       .Command("optimize")
                                       .Command("workspace")
                                       .Do(ctx => Workspace.Optimize());

            // bookmark workspace
            yield return MatcherBuilder.Describe("bookmark workspace")
                                       .Command("bookmark")
                                       .Command("workspace")
                                       .Do(ctx => Workspace.Bookmark());

            // checkout workspace
            yield return MatcherBuilder.Describe("checkout workspace {0}", version)
                                       .Command("checkout")
                                       .Command("workspace")
                                       .Param(version)
                                       .Do(ctx => Workspace.CheckoutBookmark(ctx.Get(version)));
        }
    }
}