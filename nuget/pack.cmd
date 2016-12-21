dotnet restore ..\dotnetcore\Ractor.Persistence
dotnet pack ..\dotnetcore\Ractor.Persistence -c RELEASE -o ..\artifacts

dotnet restore ..\dotnetcore\Ractor.Persistence.AWS
dotnet pack ..\dotnetcore\Ractor.Persistence.AWS -c RELEASE -o ..\artifacts

dotnet restore ..\dotnetcore\Ractor
dotnet pack ..\dotnetcore\Ractor -c RELEASE -o ..\artifacts

pause