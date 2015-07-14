// --------------------------------------------------------------------------------------
// FAKE build script 
// --------------------------------------------------------------------------------------

#r @"C:/tools/BUILD/FAKE/FakeLib.dll"
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
let project = "Ractor"


// Short summary of the project
// (used as description in AssemblyInfo and as a short summary for NuGet package)
let summary = "Redis based distributed actors + Dead simple API for distributed POCOs persistence"

// Longer description of the project
// (used as a description for NuGet package; line breaks are automatically cleaned up)
let description = """Ractor (Redis Actor) is a distributed actor system with CLR/JVM interop (WIP) and dead simple API for POCOs cache/persistence. 
Its API is inspired by F#'s MailboxProcessor, Fsharp.Actor and Akka(.NET) libraries. 
The main difference is that in Ractor actors exist is Redis per se as lists of messages, 
while a number of ephemeral workers (actors' "incarnations") take messages from Redis, 
process them and post results back."""

// List of author names (for NuGet package)
let authors = [ "Victor Baybekov" ]
// Tags for your project (for NuGet package)
let tags = "F# .NET fsharp redis akka distributed JVM interop"

// File system information 
// (<solutionFile>.sln is built during the building process)
let solutionFile  = "Ractor"
// Pattern specifying assemblies to be tested using NUnit
let testAssemblies = "tests/**/bin/Release/*Tests*.dll"

// Git configuration (used for publishing documentation in gh-pages branch)
// The profile where the project is posted 
let gitHome = "https://github.com/buybackoff"
// The name of the project on GitHub
let gitName = "Ractor"
let cloneUrl = "git@github.com:buybackoff/Ractor.CLR.git"


// --------------------------------------------------------------------------------------
// Ractor.Persistence
// --------------------------------------------------------------------------------------

let projectPersistence = "Ractor.Persistence"
let summaryPersistence = "Ractor.Persistence is a collection of APIs for POCOs and blobs persistence and a strongly typed Redis
client based on excellent Stackexchange.Redis library."
let descriptionPersistence = summaryPersistence + """
The typed Redis client has strong opinion about keys schema inside Redis and uses a 
concept of root/owner objects to store dependent objects/collections. POCO/database persistor is implemented on top 
of Entity Framework 6 (with automatic migrations enabled for non-destructive schema changes). Blob persistor saves large data 
objects to files or S3."""
let tagsPersistence = "F# .NET fsharp redis akka distributed JVM interop persistence DB database shards"

// --------------------------------------------------------------------------------------
// Ractor.Persistence
// --------------------------------------------------------------------------------------

let projectPersistenceAWS = "Ractor.Persistence.AWS"
let summaryPersistenceAWS = "Ractor.Persistence is a collection of APIs for POCOs and blobs persistence and a strongly typed Redis
client based on excellent Stackexchange.Redis library."
let descriptionPersistenceAWS = summaryPersistence + """
Ractor.Persistence.AWS has some interface implementations for AWS platform."""
let tagsPersistenceAWS = "F# .NET fsharp redis akka distributed JVM interop AWS S3 persistence queue cloud"

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
        Attribute.FileVersion release.AssemblyVersion 
        Attribute.InternalsVisibleTo "Ractor.CS.Tests"
        Attribute.InternalsVisibleTo "Ractor.Tests"
        Attribute.InternalsVisibleTo "Ractor.Profiler"] 
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
            ReleaseNotes = String.Join(Environment.NewLine, release.Notes |> Seq.take 1)
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
            ReleaseNotes = String.concat " " (release.Notes  |> Seq.take 1)
            Tags = tagsPersistence
            OutputPath = "bin"
            AccessKey = getBuildParamOrDefault "nugetkey" ""
            Publish = hasBuildParam "nugetkey" 
            Dependencies = [] })
        ("nuget/Ractor.Persistence.nuspec")

    NuGet (fun p -> 
        { p with   
            Authors = authors
            Project = projectPersistenceAWS
            Summary = summaryPersistenceAWS
            Description = descriptionPersistenceAWS
            Version = release.NugetVersion
            ReleaseNotes = String.concat " " (release.Notes  |> Seq.take 1)
            Tags = tagsPersistenceAWS
            OutputPath = "bin"
            AccessKey = getBuildParamOrDefault "nugetkey" ""
            Publish = hasBuildParam "nugetkey" 
            Dependencies = [] })
        ("nuget/Ractor.Persistence.AWS.nuspec")
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
