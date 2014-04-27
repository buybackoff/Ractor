// --------------------------------------------------------------------------------------
// FAKE build script 
// --------------------------------------------------------------------------------------

#r @"packages/FAKE/tools/FakeLib.dll"
open Fake 
open Fake.Git
open Fake.AssemblyInfoFile
open Fake.ReleaseNotesHelper
open System

// --------------------------------------------------------------------------------------
// START TODO: Provide project-specific details below
// --------------------------------------------------------------------------------------

// Information about the project are used
//  - for version and project name in generated AssemblyInfo file
//  - by the generated NuGet package 
//  - to run tests and to publish documentation on GitHub gh-pages
//  - for documentation, you also need to edit info in "docs/tools/generate.fsx"

// The name of the project 
// (used by attributes in AssemblyInfo, name of a NuGet package and directory in 'src')
let project = "Fredis"


// Short summary of the project
// (used as description in AssemblyInfo and as a short summary for NuGet package)
let summary = "F# Redis client"

// Longer description of the project
// (used as a description for NuGet package; line breaks are automatically cleaned up)
let description = """
 Fredis is a BookSleeve wrapper for convenient usage from F# (plus planned some extentions, 
additional usefull commands, patterns and a DSL implemented as custom query expression commands). """
// List of author names (for NuGet package)
let authors = [ "Victor Baybekov" ]
// Tags for your project (for NuGet package)
let tags = "F# fsharp redis"

// File system information 
// (<solutionFile>.sln is built during the building process)
let solutionFile  = "Fredis"
// Pattern specifying assemblies to be tested using NUnit
let testAssemblies = "tests/**/bin/Release/*Tests*.dll"

// Git configuration (used for publishing documentation in gh-pages branch)
// The profile where the project is posted 
let gitHome = "https://github.com/buybackoff"
// The name of the project on GitHub
let gitName = "Fredis"
let cloneUrl = "git@github.com:buybackoff/Fredis.git"


// --------------------------------------------------------------------------------------
// Fredis.Persistence
// --------------------------------------------------------------------------------------

let projectPersistence = "Fredis.Persistence"
let summaryPersistence = "Persistence abstractions for DB, files, queues."
let descriptionPersistence = """
 Persistence abstractions for DB, files, queues with some basic implementations. Very simple sharding
 strategy for RDBMSs similar to Pinterest's but with incremental growth and based on Guids """
let tagsPersistence = "persistence DB database shards"


// --------------------------------------------------------------------------------------
// Beehive
// --------------------------------------------------------------------------------------

let projectBeehive = "Beehive"
let summaryBeehive = "Redis based distributed Actor System extending Fredis and FSharp.Actor/Pigeon"
let descriptionBeehive = """
 Redis based distributed Actor System extending Fredis and FSharp.Actor/Pigeon. """
let tagsBeehive = "F# fsharp redis actor agents"

// --------------------------------------------------------------------------------------
// END TODO: The rest of the file includes standard build steps 
// --------------------------------------------------------------------------------------

// Read additional information from the release notes document
Environment.CurrentDirectory <- __SOURCE_DIRECTORY__
let release = parseReleaseNotes (IO.File.ReadAllLines "RELEASE_NOTES.md")

// Generate assembly info files with the right version & up-to-date information
Target "AssemblyInfo" (fun _ ->
  let fileName = "src/" + project + "/AssemblyInfo.fs"
  CreateFSharpAssemblyInfo fileName
      [ Attribute.Title project
        Attribute.Product project
        Attribute.Description summary
        Attribute.Version release.AssemblyVersion
        Attribute.FileVersion release.AssemblyVersion ] 
)

// --------------------------------------------------------------------------------------
// Clean build results & restore NuGet packages

Target "RestorePackages" RestorePackages

Target "Clean" (fun _ ->
    CleanDirs ["bin"; "temp"]
)

Target "CleanDocs" (fun _ ->
    CleanDirs ["docs/output"]
)

// --------------------------------------------------------------------------------------
// Build library & test project

Target "Build" (fun _ ->
    !! (solutionFile + "*.sln")
    |> MSBuildRelease "" "Rebuild"
    |> ignore
)

// --------------------------------------------------------------------------------------
// Run the unit tests using test runner

Target "RunTests" (fun _ ->
    !! testAssemblies 
    |> NUnit (fun p ->
        { p with
            DisableShadowCopy = true
            TimeOut = TimeSpan.FromMinutes 20.
            OutputFile = "TestResults.xml" })
)

// --------------------------------------------------------------------------------------
// Build a NuGet package

Target "NuGet" (fun _ ->
    NuGet (fun p -> 
        { p with   
            Authors = authors
            Project = project
            Summary = summary
            Description = description
            Version = release.NugetVersion
            ReleaseNotes = String.Join(Environment.NewLine, release.Notes)
            Tags = tags
            OutputPath = "bin"
            AccessKey = getBuildParamOrDefault "nugetkey" ""
            Publish = hasBuildParam "nugetkey"
            Dependencies = [] })
        ("nuget/" + project + ".nuspec")

    NuGet (fun p -> 
        { p with   
            Authors = authors
            Project = projectBeehive
            Summary = summaryBeehive
            Description = description + "\n\n" + descriptionBeehive
            Version = release.NugetVersion
            ReleaseNotes = String.concat " " release.Notes
            Tags = tagsBeehive
            OutputPath = "bin"
            AccessKey = getBuildParamOrDefault "nugetkey" ""
            Publish = hasBuildParam "nugetkey" 
            Dependencies = [] })
        ("nuget/Beehive.nuspec")

    NuGet (fun p -> 
        { p with   
            Authors = authors
            Project = projectPersistence
            Summary = summaryPersistence
            Description = descriptionPersistence
            Version = release.NugetVersion
            ReleaseNotes = String.concat " " release.Notes
            Tags = tagsPersistence
            OutputPath = "bin"
            AccessKey = getBuildParamOrDefault "nugetkey" ""
            Publish = hasBuildParam "nugetkey" 
            Dependencies = [] })
        ("nuget/Fredis.Persistence.nuspec")
)

// --------------------------------------------------------------------------------------
// Generate the documentation

Target "GenerateDocs" (fun _ ->
    executeFSIWithArgs "docs/tools" "generate.fsx" ["--define:RELEASE"] [] |> ignore
)

// --------------------------------------------------------------------------------------
// Release Scripts

Target "ReleaseDocs" (fun _ ->
    let tempDocsDir = "temp/gh-pages"
    CleanDir tempDocsDir
    Repository.cloneSingleBranch "" cloneUrl "gh-pages" tempDocsDir

    fullclean tempDocsDir
    CopyRecursive "docs/output" tempDocsDir true |> tracefn "%A"
    StageAll tempDocsDir
    Commit tempDocsDir (sprintf "Update generated documentation for version %s" release.NugetVersion)
    Branches.push tempDocsDir
)

Target "Release" DoNothing

// --------------------------------------------------------------------------------------
// Run all targets by default. Invoke 'build <Target>' to override

Target "All" DoNothing

"Clean"
  ==> "RestorePackages"
  ==> "AssemblyInfo"
  ==> "Build"
 // ==> "RunTests"
  ==> "All"

"All" 
  ==> "NuGet"
  ==> "CleanDocs"
  ==> "GenerateDocs"
  ==> "ReleaseDocs"
  ==> "Release"

RunTargetOrDefault "NuGet"
