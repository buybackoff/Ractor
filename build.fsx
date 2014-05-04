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
let summary = "Fredis (F#+Redis) is dead simple collection of APIs for POCOs cache/persistence + redis based distributed actors"

// Longer description of the project
// (used as a description for NuGet package; line breaks are automatically cleaned up)
let description = """
 **Fredis** (F# + Redis) is a light distributed actors framework built on top of Redis.  Its API is similar to 
F#'s MailboxProcessor and Fsharp.Actor library. The main difference is that in Fredis actors exist 
is Redis per se as lists of messages, while a number of ephemeral workers (actors' "incarnations") take messages
from Redis, process them and post results back."""

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
let summaryPersistence = "Fredis.Persistence is a collection of APIs for POCOs and blobs persistence and a strongly typed Redis
client based on excellent Stackexchange.Redis library."
let descriptionPersistence = """
The typed Redis client has strong opinion about keys schema inside Redis and uses a concept 
root/owner objects to store dependent objects/collections. POCO/database persistor base implementation
wraps around ServiceStack.ORMLite.v3, however there is no binary dependency and any ORM could be plugged 
in. Blob persistor saves large data objects to files or S3. """
let tagsPersistence = "persistence DB database shards"


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
